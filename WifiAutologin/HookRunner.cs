using System.Diagnostics;

namespace WifiAutologin;

public enum HookType
{
    PreLogin,
    Login,
    PostLogin,
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
            hooks = network.Hooks.PreLogin ?? Config.Instance.Fallback.Hooks.PreLogin ?? new List<Config.NetworkHook>();
            break;
        case HookType.Login:
            hooks = network.Hooks.Login ?? Config.Instance.Fallback.Hooks.Login ?? new List<Config.NetworkHook>();
            break;
        case HookType.PostLogin:
            hooks = network.Hooks.PostLogin ?? Config.Instance.Fallback.Hooks.PostLogin ?? new List<Config.NetworkHook>();
            break;
        case HookType.Data:
            hooks = network.Hooks.Data ?? Config.Instance.Fallback.Hooks.Data ?? new List<Config.NetworkHook>();
            break;
        case HookType.Error:
            hooks = network.Hooks.Error ?? Config.Instance.Fallback.Hooks.Error ?? new List<Config.NetworkHook>();
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
