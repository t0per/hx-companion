using CommunicationHub;
using Google.FlatBuffers;
using Hub = CommunicationHub;

namespace HxCompanion;

/// <summary>Sidetone control of a Cloud III S Wireless through NGENUITY's communication hub service.</summary>
sealed class Headset : IDisposable
{
    readonly Ipc _ipc = new(6890, 6899);
    ulong _baseId;

    public string Name { get; private set; } = "";

    public void Find()
    {
        var builder = new FlatBufferBuilder(1024);
        DeviceList.StartDeviceList(builder);
        var list = DeviceList.EndDeviceList(builder);
        AddRequest.StartAddRequest(builder);
        AddRequest.AddRequestData(builder, list.Value);
        AddRequest.AddRequestDataType(builder, RequestData.DeviceList);
        var request = AddRequest.EndAddRequest(builder);
        Hub.Message.StartMessage(builder);
        Hub.Message.AddAction(builder, request.Value);
        Hub.Message.AddActionType(builder, Hub.Action.AddRequest);
        var ids = Exchange(builder).ActionAsAddRequest().RequestDataAsDeviceList().GetBaseIdsArray() ?? [];

        var seen = new List<string>();
        foreach (var id in ids)
        {
            var b = new FlatBufferBuilder(1024);
            DeviceInformation.StartDeviceInformation(b);
            var info = Command(id, b, CommandData.DeviceInformation, DeviceInformation.EndDeviceInformation(b).Value);
            if (info.ActionType == Hub.Action.NONE || info.ErrMessage != null)
                continue;
            var device = info.ActionAsAddCommand().CommandDataAsDeviceInformation();
            seen.Add($"{device.Product}{(device.IsDongle ? " (dongle)" : "")}");
            // NGENUITY addresses a wireless headset through its dongle entry, so the dongle flag is not filtered.
            if (device.Product is Products.CloudIIISWireless or Products.CloudIIISWirelessDTS)
            {
                _baseId = id;
                Name = device.Product.ToString();
                return;
            }
        }
        throw new InvalidOperationException("Cloud III S Wireless introuvable. Appareils vus : "
            + (seen.Count == 0 ? "aucun" : string.Join(", ", seen)));
    }

    public bool GetSidetoneEnabled()
    {
        var b = new FlatBufferBuilder(1024);
        AudioInputMonitoringStatusResponse.StartAudioInputMonitoringStatusResponse(b);
        var reply = Checked(Command(_baseId, b, CommandData.AudioInputMonitoringStatusResponse,
            AudioInputMonitoringStatusResponse.EndAudioInputMonitoringStatusResponse(b).Value));
        return reply.ActionAsAddCommand().CommandDataAsAudioInputMonitoringStatusResponse().IsEnabled;
    }

    public void SetSidetoneEnabled(bool enabled)
    {
        var b = new FlatBufferBuilder(1024);
        AudioInputMonitoringStatus.StartAudioInputMonitoringStatus(b);
        AudioInputMonitoringStatus.AddIsEnabled(b, enabled);
        Checked(Command(_baseId, b, CommandData.AudioInputMonitoringStatus,
            AudioInputMonitoringStatus.EndAudioInputMonitoringStatus(b).Value));
    }

    public int GetSidetoneDb()
    {
        var b = new FlatBufferBuilder(1024);
        AudioInputMonitoringLevelResponse.StartAudioInputMonitoringLevelResponse(b);
        var reply = Checked(Command(_baseId, b, CommandData.AudioInputMonitoringLevelResponse,
            AudioInputMonitoringLevelResponse.EndAudioInputMonitoringLevelResponse(b).Value));
        return (sbyte)reply.ActionAsAddCommand().CommandDataAsAudioInputMonitoringLevelResponse().Level;
    }

    public void SetSidetoneDb(int db)
    {
        var b = new FlatBufferBuilder(1024);
        AudioInputMonitoringLevel.StartAudioInputMonitoringLevel(b);
        AudioInputMonitoringLevel.AddLevel(b, unchecked((byte)(sbyte)db));
        Checked(Command(_baseId, b, CommandData.AudioInputMonitoringLevel,
            AudioInputMonitoringLevel.EndAudioInputMonitoringLevel(b).Value));
    }

    Hub.Message Command(ulong baseId, FlatBufferBuilder builder, CommandData type, int dataOffset)
    {
        AddCommand.StartAddCommand(builder);
        AddCommand.AddCommandDataType(builder, type);
        AddCommand.AddCommandData(builder, dataOffset);
        var command = AddCommand.EndAddCommand(builder);
        Hub.Message.StartMessage(builder);
        Hub.Message.AddBaseId(builder, baseId);
        Hub.Message.AddAction(builder, command.Value);
        Hub.Message.AddActionType(builder, Hub.Action.AddCommand);
        return Exchange(builder);
    }

    Hub.Message Exchange(FlatBufferBuilder builder)
    {
        Hub.Message.FinishMessageBuffer(builder, Hub.Message.EndMessage(builder));
        return Hub.Message.GetRootAsMessage(new ByteBuffer(_ipc.Send(builder.SizedByteArray())));
    }

    static Hub.Message Checked(Hub.Message reply)
    {
        if (reply.ErrMessage != null)
            throw new InvalidOperationException("Refusé par le casque : " + reply.ErrMessage);
        return reply;
    }

    public void Dispose() => _ipc.Dispose();
}
