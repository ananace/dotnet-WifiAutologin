using System.Diagnostics;
using System.Text.RegularExpressions;
using WifiAutologin.Util;

namespace WifiAutologin.DiscoveryBackends;

[DiscoveryBackend("iwconfig", OSes = OSFamily.Linux)]
public class IwConfig : IDiscoveryBackend
{
    static ILogger Logger { get; } = WifiAutologin.Logger.Global[typeof(IwConfig)];

    public bool IsAvailable => OperatingSystem.IsLinux() && PathUtils.ExistsOnPath("iwconfig");
    public bool IsConnected => ConnectedNetworks.Any();
    public bool IsConnectedToVPN => false;
    public bool SupportsDaemonize => false;

    public IDisposable WatchChanges(Action<IDiscoveryBackend> handler)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<string> ConnectedNetworks { get
        {
            var command = "iwconfig";
            Logger.Debug($"> {command}");

            using (var process = new Process())
            {
                var start = process.StartInfo;
                start.FileName = "iwconfig";
                start.RedirectStandardOutput = true;

                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    yield break;

                var stream = process.StandardOutput;
                var rex = new Regex("SSID:\"(.+)\"");
                string? line;
                while ((line = stream.ReadLine()) != null)
                {
                    Logger.Debug($"< {line}");
                    var match = rex.Match(line);
                    if (!match.Success)
                        continue;

                    yield return match.Groups[1].Value;
                }
            }
        }
    }
}
