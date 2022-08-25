using System.Diagnostics;

namespace WifiAutologin;

public enum HookType
{
    PreLogin,
    Login,
    Data,
    Error
}

public static class HookRunner
{
    static ILogger Logger { get; } = WifiAutologin.Logger.Global[typeof(HookRunner)];

    public static void RunHooks(Config.NetworkConfig network, HookType type, Config.NetworkHook.OnlyWhen only, IReadOnlyDictionary<string, string>? env = null)
    {
        IReadOnlyList<Config.NetworkHook> hooks;
        switch (type)
        {
        case HookType.PreLogin:
            if (network.Hooks.HasPreLogin)
                hooks = network.Hooks.PreLogin;
            else
                hooks = Config.Instance.Fallback.Hooks.PreLogin;
            break;
        case HookType.Login:
            if (network.Hooks.HasLogin)
                hooks = network.Hooks.Login;
            else
                hooks = Config.Instance.Fallback.Hooks.Login;
            break;
        case HookType.Data:
            if (network.Hooks.HasData)
                hooks = network.Hooks.Data;
            else
                hooks = Config.Instance.Fallback.Hooks.Data;
            break;
        case HookType.Error:
            if (network.Hooks.HasError)
                hooks = network.Hooks.Error;
            else
                hooks = Config.Instance.Fallback.Hooks.Error;
            break;
        default: return;
        }

        var environment = new Dictionary<string, string>{
            { "WIFI_AUTOLOGIN", "1" },
            { "NETWORK", network.SSID ?? "<SSID>" }
        };
        env?.ToList()?.ForEach(v => environment.Add(v.Key, v.Value));

        foreach (var hook in hooks.Where(h => h.When == only || only == Config.NetworkHook.OnlyWhen.Always || h.When == Config.NetworkHook.OnlyWhen.Always))
        {
            if (hook.If != null)
            {
                if (!RunCommand(hook.If, environment))
                    continue;
            }

            if (hook.Unless != null)
            {
                if (RunCommand(hook.Unless, environment))
                    continue;
            }

            if (RunCommand(hook.Hook, environment) && hook.Break)
                break;
        }
    }

    private static bool RunCommand(string command, IReadOnlyDictionary<string, string>? env = null)
    {
        Logger.Debug($"> {command}");

        using (var process = new Process())
        {
            var start = process.StartInfo;

            start.FileName = "sh";
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add(command);
            env?.ToList()?.ForEach(v => start.Environment[v.Key] = v.Value);

            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
    }
}
