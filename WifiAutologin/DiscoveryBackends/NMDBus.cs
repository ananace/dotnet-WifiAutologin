#if OS == UNIX
using NetworkManager.DBus;
using Tmds.DBus;
#endif

using WifiAutologin.Util;

namespace WifiAutologin.DiscoveryBackends;

[DiscoveryBackend("NetworkManager", OSes = OSFamily.Linux)]
public class NMDBus : IStreamingDiscoveryBackend, IDisposable
{
    static ILogger Logger { get; } = WifiAutologin.Logger.Global[typeof(NMDBus)];

    public class NMNetwork
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public bool VPN { get; set; } = false;
    }

    public void Dispose()
    {
#if OS == UNIX
        NMConnection = null;
        watchThread = null;
#endif
    }

#if OS == UNIX
    INetworkManager? NMConnection;

    public NMDBus()
    {
        try
        {
            NMConnection = Connection.System.CreateProxy<INetworkManager>("org.freedesktop.NetworkManager",
                                                                          "/org/freedesktop/NetworkManager");
        }
        catch (DBusException ex)
        {
            Logger.Warn($"Failed to connect to NetworkManager: {ex}");
        }
    }
#endif

    public bool IsAvailable =>
#if OS == UNIX
        OperatingSystem.IsLinux() &&
        NMConnection != null;
#else
        false;
#endif
    public bool IsConnected => ConnectedNetworks.Any();
    public bool IsConnectedToVPN { get {
        var task = FindConnectedNetworks();
        task.Wait();

        return task.Result.Any(n => n.VPN);
    } }

    public IEnumerable<string> ConnectedNetworks => FindConnectedNetworks().Result.Where(net => net.Type.Contains("wireless")).Select(net => net.Id);

    public event EventHandler? OnConnectionChanged;

    Thread? watchThread;
    public void WatchChanges()
    {
        if (watchThread != null)
            return;

        watchThread = new Thread(WatchThread);
        watchThread.Start();
    }

    void WatchThread()
    {
#if OS == UNIX
        if (NMConnection == null)
            throw new Exception("No DBus connection");

        IEnumerable<IDevice> wifiDevices = new IDevice[0];
        Task.WaitAll(Task.Run(async () => {
            wifiDevices = await (await NMConnection.GetDevicesAsync())
                .Where(async d => await d.GetDeviceTypeAsync() == DeviceType.WiFi);
        }));

        Logger.Debug("Adding watchers...");
        Dictionary<string, IDisposable> watchers = new Dictionary<string, IDisposable>();
        using var wrapper = new DisposableCollector(watchers);

        void OnStateChange(IDevice device, DeviceState newState, DeviceState oldState, uint reason)
        {
            Logger.Debug($"State change for {device.ObjectPath}: {oldState} => {newState} ({reason})");

            if (newState == DeviceState.Activated)
            {
                Task.Run(() => {
                    OnConnectionChanged?.Invoke(this, EventArgs.Empty);
                });
            }
        };

        async void OnDeviceAdded(IDevice device)
        {
            DeviceType type = DeviceType.Unknown;
            try{
                type = await device.GetDeviceTypeAsync();
            } catch (DBusException ex) {
                Logger.Warn($"Failed to check device type for {device.ObjectPath}: {ex}");
                return;
            }

            if (type != DeviceType.WiFi)
            {
                Logger.Debug($"Received add request for {device.ObjectPath} of type {type}, ignoring.");
                return;
            }

            Logger.Debug($"Adding watcher for {device.ObjectPath}");
            IDisposable watcher = await device.WatchStateChangedAsync(c => OnStateChange(device, c.newState, c.oldState, c.reason));

            watchers[device.ObjectPath.ToString()] = watcher;
        }

        void OnDeviceRemoved(IDevice device)
        {
            Logger.Debug($"Removing watcher for {device.ObjectPath}");

            var key = device.ObjectPath.ToString();

            if (watchers.ContainsKey(key))
            {
                watchers[key].Dispose();
                watchers.Remove(key);
            }
        }

        Task.Run(async () => {
            Logger.Debug($"Adding watcher for OnDeviceAdded");
            var watcher = await NMConnection.WatchDeviceAddedAsync(c => {
                var dev = Connection.System.CreateProxy<IDevice>("org.freedesktop.NetworkManager", c);
                OnDeviceAdded(dev);
            });
            watchers["OnDeviceAdded"] = watcher;
        });
        Task.Run(async () => {
            Logger.Debug($"Adding watcher for OnDeviceRemoved");
            var watcher = await NMConnection.WatchDeviceRemovedAsync(c => {
                var dev = Connection.System.CreateProxy<IDevice>("org.freedesktop.NetworkManager", c);
                OnDeviceRemoved(dev);
            });
            watchers["OnDeviceRemoved"] = watcher;
        });

        foreach (var device in wifiDevices) {
            OnDeviceAdded(device);
        }

        while (true) {
            Thread.Sleep(250);
        }
#endif
    }

    public async Task<IEnumerable<NMNetwork>> FindConnectedNetworks()
    {

        List<NMNetwork> ret = new List<NMNetwork>();
#if OS == UNIX
        if (NMConnection == null)
            throw new Exception("No DBus connection");

        Logger.Debug("Checking for active networks");

        var connections = (await NMConnection.GetActiveConnectionsAsync()).ToList();
        foreach (var active in connections)
        {
            Logger.Debug($"- Working on {active}...");
            var nmConn = Connection.System.CreateProxy<IActive>("org.freedesktop.NetworkManager", active);

            var type = await nmConn.GetTypeAsync();
            Logger.Debug($"  is {type}");

            var id = await nmConn.GetIdAsync();
            Logger.Debug($"  has id {id}");

            var vpn = await nmConn.GetVpnAsync();
            vpn = vpn || type == "wireguard";
            Logger.Debug($"  is vpn {vpn}");

            ret.Add(new NMNetwork { Id = id, Type = type, VPN = vpn });
        }

#endif
        return ret;
    }
}
