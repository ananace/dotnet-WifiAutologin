namespace WifiAutologin;

public interface IDiscoveryBackend
{
    bool IsAvailable { get; }

    bool IsConnected { get; }
    bool IsConnectedToVPN { get; }
    bool SupportsDaemonize { get; }

    IDisposable WatchChanges(Action<IDiscoveryBackend> handler);

    IEnumerable<string> ConnectedNetworks { get; }
}
