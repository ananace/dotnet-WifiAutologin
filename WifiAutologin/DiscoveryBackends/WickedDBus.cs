#if OS == UNIX
// using Wicked.DBus;
using Tmds.DBus;
#endif

using WifiAutologin.Util;

namespace WifiAutologin.DiscoveryBackends;

[DiscoveryBackend("Wicked", OSes = OSFamily.Linux)]
public class WickedDBus : IDiscoveryBackend, IDisposable
{
    static ILogger Logger { get; } = WifiAutologin.Logger.Global[typeof(WickedDBus)];

    public void Dispose()
    {
    }

    public WickedDBus()
    {
    }

    public bool IsAvailable =>
#if OS == UNIX
        false &&
        OperatingSystem.IsLinux();
#else
        false;
#endif
    public bool IsConnected => ConnectedNetworks.Any();
    public bool IsConnectedToVPN => false;

    public IEnumerable<string> ConnectedNetworks { get { return new string[0]; } }
}
