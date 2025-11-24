using System.Reflection;

namespace WifiAutologin.Util;

public static class BackendFactory
{
    static ILogger Logger { get; } = WifiAutologin.Logger.Global[typeof(BackendFactory)];

    public static IDiscoveryBackend CreateInteractiveBackend()
    {
        return AvailableBackends.First(b => b.IsAvailable);
    }
    public static IStreamingDiscoveryBackend? CreateDaemonBackend()
    {
        return AvailableBackends.OfType<IStreamingDiscoveryBackend>().FirstOrDefault(b => b.IsAvailable);
    }

    static List<IDiscoveryBackend> _AvailableBackends = new List<IDiscoveryBackend>();

    static IEnumerable<IDiscoveryBackend> AvailableBackends
    {
        get
        {
            if (!_AvailableBackends.Any())
                EnumerateBackends();

            return _AvailableBackends;
        }
    }

    static void EnumerateBackends()
    {
        Logger.Debug("Checking for available backends");

        /*
        var backends = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetInterface(nameof(IDiscoveryBackend)) != null);
        */
        var backends = new[] {
            // Linux
            typeof(DiscoveryBackends.NMDBus),
            typeof(DiscoveryBackends.WickedDBus),

            typeof(DiscoveryBackends.Iw),
            typeof(DiscoveryBackends.IwConfig),

            // Windows
            typeof(DiscoveryBackends.NetshExe),
        };

        OSFamily fam = OSFamily.None;
        if (OperatingSystem.IsLinux())
            fam = OSFamily.Linux;
        else if (OperatingSystem.IsWindows())
            fam = OSFamily.Windows;

        foreach (var backend in backends)
        {
            var attr = backend.GetCustomAttribute<DiscoveryBackendAttribute>();
            if (attr != null)
            {
                if (!attr.OSes.HasFlag(fam))
                    continue;
            }

            IDiscoveryBackend? instance = null;

            try
            {
                instance = Activator.CreateInstance(backend) as IDiscoveryBackend;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create instance of {attr?.Name ?? backend.FullName}, {ex}");
            }

            if (instance == null)
                continue;

            Logger.Debug($"Found available backend: {backend}");
            _AvailableBackends.Add(instance);
        }
    }
}
