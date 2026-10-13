using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace HxCompanion;

static class Program
{
    [STAThread]
    static void Main()
    {
        // NGENUITY's assemblies are not shipped with this tool; they are loaded from the user's own installation.
        string? ngenuityDir = FindNgenuityDir();
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            string path = Path.Combine(ngenuityDir ?? "", name.Name + ".dll");
            return ngenuityDir != null && File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
        };
        ApplicationConfiguration.Initialize();
        if (ngenuityDir == null)
        {
            MessageBox.Show("HyperX NGENUITY est introuvable. Installez-le et lancez-le, puis relancez HxCompanion.",
                "HxCompanion", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        Run();
    }

    // Kept out of Main so no NGENUITY type is needed before the resolver above is registered.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run() => Application.Run(new MainForm());

    static string? FindNgenuityDir()
    {
        var candidates = new List<string?>();
        foreach (var process in Process.GetProcessesByName("NGENUITY"))
        {
            try
            {
                candidates.Add(Path.GetDirectoryName(process.MainModule?.FileName));
            }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                // Process exited or is not readable; fall back to the default location.
            }
            process.Dispose();
        }
        candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NGENUITY", "current"));
        return candidates.FirstOrDefault(dir => dir != null && File.Exists(Path.Combine(dir, "NGENUITY.Common.dll")));
    }
}
