using WifiAutologin.Util;

namespace WifiAutologin.Interfaces;

public class Daemon : IInterface
{
    bool SkipConnectionCheck = false;
    static ILogger Logger { get; } = WifiAutologin.Logger.Global[typeof(Daemon)];

    public void Run(Program.Options Args)
    {
        SkipConnectionCheck = Args.SkipConnectionCheck;

        var backend = BackendFactory.CreateDaemonBackend();
        if (backend == null)
        {
            Console.Error.WriteLine("Unable to find a daemonizable backend.");
            Program.ExitCode = 1;
            return;
        }

        var quitEvent = new ManualResetEvent(false);

        Console.CancelKeyPress += delegate(object? sender, ConsoleCancelEventArgs e) {
            e.Cancel = true;
            quitEvent.Set();
        };

        using (backend.WatchChanges(OnChange))
        {
            Logger.Info("Launched background change watcher...");
            quitEvent.WaitOne();
            Logger.Info("Quitting...");
        }
    }

    object LoginLock = new object();
    bool AttemptingLogin = false;
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

            if (!SkipConnectionCheck && !Program.NeedsLogin(network))
            {
                Logger.Info("No login required, ignoring");
                continue;
            }

            lock(LoginLock)
            {
                if (AttemptingLogin)
                {
                    Logger.Info("Skipping login due to exisitng login attempt");
                    continue;
                }

                AttemptingLogin = true;

                Logger.Info("Logging in...");

                Program.Login(network);

                AttemptingLogin = false;
            }
        }
    }
}
