using System.Diagnostics;
using System.Text.RegularExpressions;
using WifiAutologin.Util;

namespace WifiAutologin.DiscoveryBackends;

[DiscoveryBackend("Netsh", OSes = OSFamily.Windows)]
public class NetshExe : IDiscoveryBackend
{
    static ILogger Logger { get; } = WifiAutologin.Logger.Global[typeof(NetshExe)];

    public bool IsAvailable => OperatingSystem.IsWindows() && PathUtils.ExistsOnPath("netsh.exe");
    public bool IsConnected => ConnectedNetworks.Any();
    public bool IsConnectedToVPN => false;
    public bool SupportsDaemonize => false;

    public IDisposable WatchChanges(Action<IDiscoveryBackend> handler)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<string> ConnectedNetworks { get
        {
            var command = "netsh.exe wlan show interfaces";
            Logger.Debug($"> {command}");

            using (var process = new Process())
            {
                var start = process.StartInfo;
                start.FileName = "netsh.exe";
                start.Arguments = "wlan show interfaces";
                start.RedirectStandardOutput = true;
                start.CreateNoWindow = true;

                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    yield break;

                var stream = process.StandardOutput;
                var rex = new Regex(@"\s+SSID\s+:\s*(.+)");
                string? line;
                while ((line = stream.ReadLine()) != null)
                {
                    Logger.Debug($"< {line}");
                    var match = rex.Match(line);
                    if (!match.Success)
                        continue;

                    // TODO: Check if IP address is resolved

                    yield return match.Groups[1].Value;
                }
            }
        }
    }
}
