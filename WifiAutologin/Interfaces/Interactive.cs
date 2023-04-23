using WifiAutologin.Util;

namespace WifiAutologin.Interfaces;

public class Interactive : IInterface
{
    static ILogger Logger { get; } = WifiAutologin.Logger.Global[typeof(Interactive)];

    public void Run(Program.Options result)
    {
        string? networkName = null;
        if (result.Network != null)
            networkName = result.Network;

        if (networkName == null && result.AutodetectNetwork)
        {
            var backend = BackendFactory.CreateBackend();
            Logger.Info($"Using backend {backend.GetName()}");

            if (backend.IsConnectedToVPN)
                Logger.Info("Connected to VPN");

            networkName = backend.ConnectedNetworks.First();
        }

        if (networkName == null)
        {
            Console.Error.WriteLine("Need to specify a network to connect to");
            Program.ExitCode = 1;
            return;
        }

        Logger.Debug($"Using network: {networkName}");

        var network = Config.Instance.Networks.FirstOrDefault(n => n?.SSID?.Equals(networkName, StringComparison.CurrentCultureIgnoreCase) == true);
        if (network == null)
        {
            Console.Error.WriteLine($"No network configuration found for {networkName}");
            Program.ExitCode = 1;
            return;
        }

        // Run pre-login hooks before testing for connection, to allow for network-specific config
        if (result.Login)
            HookRunner.RunHooks(network, HookType.PreLogin, Config.NetworkHook.OnlyWhen.Always);

        if (result.Login && (result.SkipConnectionCheck || Program.NeedsLogin(network)))
            Program.Login(network);
        else if (result.Login)
        {
            Logger.Info("No login necessary, skipping.");
            if (network.AlwaysHooks)
                HookRunner.RunHooks(network, HookType.PostLogin, Config.NetworkHook.OnlyWhen.Success);
        }

        if (result.ReadData)
            Program.ReadData(network);
    }
}
