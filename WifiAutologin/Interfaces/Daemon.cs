using WifiAutologin.Util;

namespace WifiAutologin.Interfaces;

public class Daemon : IInterface
{
    bool SkipConnectionCheck = false;
    static ILogger Logger { get; } = WifiAutologin.Logger.Global[typeof(Daemon)];

    public void Run(Program.Options Args)
    {
        SkipConnectionCheck = Args.SkipConnectionCheck;

        var backend = BackendFactory.CreateDaemonBackend() ?? BackendFactory.CreateInteractiveBackend();
        if (backend == null)
        {
            Console.Error.WriteLine("Failed to find a valid backend");
            Program.ExitCode = 1;
            return;
        }

        Logger.Debug($"Using backend {backend.GetType()}");

        if (backend is IStreamingDiscoveryBackend streamingBackend)
        {
            var quitEvent = new ManualResetEvent(false);

            Console.CancelKeyPress += delegate(object? sender, ConsoleCancelEventArgs e) {
                e.Cancel = true;
                quitEvent.Set();
            };

            streamingBackend.OnConnectionChanged += (_, __) => OnChange(backend);
            streamingBackend.WatchChanges();

            Logger.Info("Launched background change watcher...");
            quitEvent.WaitOne();
        }
        else
        {
            bool run = true;
            Console.CancelKeyPress += delegate(object? sender, ConsoleCancelEventArgs e) {
                e.Cancel = true;
                run = false;
            };

            Logger.Info("Launching backend polling...");
            var wasConnected = backend.ConnectedNetworks.ToHashSet();
            while (run)
            {
                var nowConnected = backend.ConnectedNetworks.ToHashSet();
                if (nowConnected != wasConnected)
                    OnChange(backend);

                wasConnected = nowConnected;

                Thread.Sleep(1000);
            }
        }
        Logger.Info("Quitting...");
    }

    List<CancellationTokenSource> ActiveLogins = new List<CancellationTokenSource>();
    void OnChange(IDiscoveryBackend backend)
    {
        foreach (var net in backend.ConnectedNetworks)
        {
            Logger.Info($"New connection to {net}");

            var network = Config.Instance.Networks.FirstOrDefault(n => n?.SSID?.Equals(net, StringComparison.CurrentCultureIgnoreCase) == true);
            if (network == null)
            {
                Logger.Info($"No network configuration found for {net}, ignoring");
                continue;
            }

            // Run pre-login hooks before testing for connection, to allow for network-specific config
            HookRunner.RunHooks(network, HookType.PreLogin, Config.NetworkHook.OnlyWhen.Always);

            if (!SkipConnectionCheck && !Program.NeedsLogin(network))
            {
                Logger.Info("No login required, ignoring");

                if (network.AlwaysHooks)
                    HookRunner.RunHooks(network, HookType.Login, Config.NetworkHook.OnlyWhen.Success);

                HookRunner.RunHooks(network, HookType.PostLogin, Config.NetworkHook.OnlyWhen.Success);
                continue;
            }

            {
                foreach (var active in ActiveLogins)
                    active.Cancel();

                if (ActiveLogins.Any())
                {
                    Logger.Info("Skipping login due to exisitng login attempt");
                    continue;
                }

                Logger.Info("Logging in...");

                Program.Login(network, attempts: 3, timeout: TimeSpan.FromSeconds(10));
            }
        }
    }
}
