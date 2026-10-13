using NetMQ;
using NetMQ.Sockets;

namespace HxCompanion;

/// <summary>Request/reply client for an NGENUITY background service (same handshake and port scan as the app).</summary>
sealed class Ipc(int startPort, int endPort) : IDisposable
{
    static readonly byte[] Handshake = [119, 1, 36];
    static readonly TimeSpan HandshakeTimeout = TimeSpan.FromMilliseconds(300);
    static readonly TimeSpan ReplyTimeout = TimeSpan.FromMilliseconds(2500);

    RequestSocket? _socket;

    public byte[] Send(byte[] request)
    {
        var socket = _socket ??= Connect();
        socket.SendFrame(request);
        if (socket.TryReceiveFrameBytes(ReplyTimeout, out var reply) && reply != null)
            return reply;
        // A REQ socket cannot send again until it has received; drop it so the next call reconnects.
        Dispose();
        throw new TimeoutException("Le service NGENUITY n'a pas répondu.");
    }

    RequestSocket Connect()
    {
        for (int port = startPort; port < endPort; port++)
        {
            var socket = new RequestSocket();
            socket.Options.Linger = TimeSpan.Zero;
            socket.Connect($"tcp://localhost:{port}");
            socket.SendFrame(Handshake);
            if (socket.TryReceiveFrameBytes(HandshakeTimeout, out var reply) && reply != null && reply.AsSpan().SequenceEqual(Handshake))
                return socket;
            socket.Dispose();
        }
        throw new InvalidOperationException($"Service NGENUITY introuvable (ports {startPort}-{endPort - 1}). NGENUITY est-il lancé ?");
    }

    public void Dispose()
    {
        _socket?.Dispose();
        _socket = null;
    }
}
