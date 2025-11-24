namespace WifiAutologin;

public interface IDiscoveryBackend
{
    bool IsAvailable { get; }

    bool IsConnected { get; }
    bool IsConnectedToVPN { get; }

    IEnumerable<string> ConnectedNetworks { get; }
}

public interface IStreamingDiscoveryBackend : IDiscoveryBackend
{
    event EventHandler OnConnectionChanged;

    void WatchChanges();

}
