#if OS == UNIX
using NetworkManager.DBus;
using Tmds.DBus;
#endif

using WifiAutologin.Util;

namespace WifiAutologin.DiscoveryBackends;

[DiscoveryBackend("NetworkManager", OSes = OSFamily.Linux)]
public class NMDBus : IDiscoveryBackend, IDisposable
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
        LastConnected?.Clear();
#endif
    }

#if OS == UNIX
    INetworkManager? NMConnection;
    List<ObjectPath>? LastConnected;

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
        false &&
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
    public bool SupportsDaemonize => true;


    public IDisposable WatchChanges(Action<IDiscoveryBackend> handler)
    {
#if OS == UNIX
        if (NMConnection == null)
            throw new Exception("No DBus connection");

        if (LastConnected == null)
            LastConnected = new List<ObjectPath>();
        IEnumerable<IDevice> wifiDevices = new IDevice[0];

        Task.WaitAll(Task.Run(async () => {
            wifiDevices = await (await NMConnection.GetDevicesAsync())
                .Where(async d => await d.GetDeviceTypeAsync() == DeviceType.WiFi);
        }));

        Logger.Debug("Adding watchers...");
        Dictionary<string, IDisposable> watchers = new Dictionary<string, IDisposable>();
        var wrapper = new DisposableCollector(watchers);

        async void OnStateChange(IDevice device, DeviceState newState, DeviceState oldState, uint reason)
        {
            Logger.Debug($"State change for {device.ObjectPath}: {oldState} => {newState} ({reason})");

            if (newState == DeviceState.Activated)
                await Task.Run(() => handler(this));
        }

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

        List<Task> tasks = new List<Task>();
        tasks.Add(Task.Run(async () => {
            Logger.Debug($"Adding watcher for OnDeviceAdded");
            var watcher = await NMConnection.WatchDeviceAddedAsync(c => {
                var dev = Connection.System.CreateProxy<IDevice>("org.freedesktop.NetworkManager", c);
                OnDeviceAdded(dev);
            });
            watchers["OnDeviceAdded"] = watcher;
        }));
        tasks.Add(Task.Run(async () => {
            Logger.Debug($"Adding watcher for OnDeviceRemoved");
            var watcher = await NMConnection.WatchDeviceRemovedAsync(c => {
                var dev = Connection.System.CreateProxy<IDevice>("org.freedesktop.NetworkManager", c);
                OnDeviceRemoved(dev);
            });
            watchers["OnDeviceRemoved"] = watcher;
        }));

        tasks.AddRange(wifiDevices.Select(device => {
            return Task.Run(() => OnDeviceAdded(device));
        }));

        Task.WaitAll(tasks.ToArray());

        Logger.Debug($"Finished adding {watchers.Count} watchers");

        return wrapper;
#else
        return null;
#endif
    }

    public IEnumerable<string> ConnectedNetworks { get {
        var task = FindConnectedNetworks();
        task.Wait();

        return task.Result.Where(n => n.Type.Contains("wireless")).Select(n => n.Id);
    } }

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
            if (LastConnected?.Contains(active) ?? false)
            {
                Logger.Debug($"- Was already connected to {active}, skipping");
                continue;
            }

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

        LastConnected?.Clear();
        LastConnected?.AddRange(connections);

#else
        await Task.FromResult<object?>(null);
#endif
        return ret;
    }
}
