using System.Diagnostics;
using System.Text.RegularExpressions;
using WifiAutologin.Util;

namespace WifiAutologin.DiscoveryBackends;

[DiscoveryBackend("IW", OSes = OSFamily.Linux)]
public class Iw : IDiscoveryBackend
{
    static ILogger Logger { get; } = WifiAutologin.Logger.Global[typeof(Iw)];

    public bool IsAvailable => OperatingSystem.IsLinux() && PathUtils.ExistsOnPath("iw");
    public bool IsConnected => ConnectedNetworks.Any();
    public bool IsConnectedToVPN => false;
    public bool SupportsDaemonize => false;

    public IDisposable WatchChanges(Action<IDiscoveryBackend> handler)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<string> ConnectedNetworks { get
        {
            var command = "iw dev";
            Logger.Debug($"> {command}");

            using (var process = new Process())
            {
                var start = process.StartInfo;
                start.FileName = "iw";
                start.Arguments = "dev";
                start.RedirectStandardOutput = true;

                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    yield break;

                var stream = process.StandardOutput;
                var rex = new Regex(@"\s+ssid (.+)");
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
