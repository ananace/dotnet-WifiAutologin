using CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace WifiAutologin;

public class Program
{
    static ILogger Logger { get; } = WifiAutologin.Logger.Global[typeof(Program)];

    public class Options
    {
        [CommandLine.Option('v', "verbose", Required = false)]
        public bool Verbose { get; set; } = false;
        [CommandLine.Option('q', "quiet", Required = false)]
        public bool Quiet { get; set; } = false;
        [CommandLine.Option('d', "daemonize", Required = false, HelpText = "Run as a long-lived service, acting on network changes")]
        public bool Daemon { get; set; } = false;

        [CommandLine.Option('a', "auto", Required = false, HelpText = "Automatically discover connected network (interactive only)")]
        public bool AutodetectNetwork { get; set; } = false;
        [CommandLine.Option('S', "skip", Required = false, HelpText = "Skip connection check")]
        public bool SkipConnectionCheck { get; set; } = false;
        [CommandLine.Option('L', "login", Required = false, HelpText = "Log in to network (interactive only)")]
        public bool Login { get; set; } = true;
        [CommandLine.Option('D', "data", Required = false, HelpText = "Read data limits from network (interactive only)")]
        public bool ReadData { get; set; } = false;

        [CommandLine.Option('n', "network", MetaValue = "NETWORK", HelpText = "The name of the network (interactive only)")]
        public string? Network { get; set; } = null;
    }

    public static int ExitCode { get; set; } = 0;

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Options))]
    public static int Main(string[] Args)
    {
        var parser = new CommandLine.Parser(s => {
            s.IgnoreUnknownArguments = true;
        });
        var parserResult = parser.ParseArguments<Options>(Args);

        parserResult
            .WithParsed<Options>(p => Run(Args, p))
            .WithNotParsed(p => DisplayHelp(parserResult, p));

        return ExitCode;
    }

    static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
    {
        CommandLine.Text.HelpText helpText;

        if (errs.IsVersion())
            helpText = CommandLine.Text.HelpText.AutoBuild(result);
        else
        {
            helpText = CommandLine.Text.HelpText.AutoBuild(
                result,
                h => h,
                e => e);
        }

        Console.Out.WriteLine(helpText);

        if (!errs.IsHelp())
            Program.ExitCode = 1;
    }

    static void Run(string[] Args, Options result)
    {
        if (result.Quiet)
            WifiAutologin.Logger.Level = System.Diagnostics.Tracing.EventLevel.Error;
        else if (result.Verbose)
            WifiAutologin.Logger.Level = System.Diagnostics.Tracing.EventLevel.Verbose;
        else if (result.Daemon)
            WifiAutologin.Logger.Level = System.Diagnostics.Tracing.EventLevel.Informational;

        Logger.Info("Starting up");

        if (result.Daemon)
            new Interfaces.Daemon().Run(result);
        else
            new Interfaces.Interactive().Run(result);
    }

    static readonly HttpClientHandler httpClientHandler = new HttpClientHandler() {
        AllowAutoRedirect = false
    };
    static readonly HttpClient httpClient = new HttpClient(httpClientHandler);
    public static bool NeedsLogin(Config.NetworkConfig network)
    {
        if (!network.LoginActions.Any())
            return false;

        return !ConnectionCheck(network);
    }


    static string ConnectionCheckUrl = "http://detectportal.firefox.com/canonical.html";
    public static bool ConnectionCheck(Config.NetworkConfig? config = null)
    {
        try
        {
            // TODO: Make this configurable?
            httpClient.Timeout = TimeSpan.FromMilliseconds(10000);
        }
        // This instance has already started one or more requests. Properties can only be modified before sending the first request.
        catch (System.InvalidOperationException)
        {
            Logger.Debug("Failed to set HTTP client timeout, continuing with default.");
            // httpClient = new HttpClient(httpClientHandler);
            // httpClient.Timeout = TimeSpan.FromMilliseconds(10000);
        }

        try
        {
            var url = config?.TestURL ?? ConnectionCheckUrl;
            Logger.Debug("Checking connection...");
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            Logger.Debug($"< {req.Method} {req.RequestUri}");
            var res = httpClient.Send(req);
            Logger.Debug($"> {(int)res.StatusCode} {res.ReasonPhrase}");

            var code = (int)res.StatusCode;
            if (code < 200 || code >= 300)
                return false;

            if (res.Headers.Contains("Location"))
                return false;

            return true;
        }
        catch (TaskCanceledException)
        {
            Logger.Warn("Connectivity check timed out.");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to check connection: {ex}");
            return false;
        }
    }

    public static void Login(Config.NetworkConfig network)
    {
        try
        {
            Logger.Info($"Logging in to {network.SSID}...");

            HookRunner.RunHooks(network, HookType.Login, Config.NetworkHook.OnlyWhen.Always);

            using(var driver = new WebDriver(network))
            {
                driver.Login();

                // Allow driver to live during the connection check, for any delayed action by the login
                if (Program.ConnectionCheck())
                    HookRunner.RunHooks(network, HookType.PostLogin, Config.NetworkHook.OnlyWhen.Success);
                else
                {
                    var environment = new Dictionary<string, string>{
                        { "ERROR", "Unable to verify connection after login" }
                    };
                    HookRunner.RunHooks(network, HookType.PostLogin, Config.NetworkHook.OnlyWhen.Failure, environment);
                    HookRunner.RunHooks(network, HookType.Error, Config.NetworkHook.OnlyWhen.Always, environment);
                    ExitCode = 1;
                    return;
                }
            }

        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());

            var environment = new Dictionary<string, string>{
                { "ERROR", ex.ToString() }
            };
            HookRunner.RunHooks(network, HookType.PostLogin, Config.NetworkHook.OnlyWhen.Failure, environment);
            HookRunner.RunHooks(network, HookType.Error, Config.NetworkHook.OnlyWhen.Always, environment);
            ExitCode = 1;
        }
    }

    public static void ReadData(Config.NetworkConfig network)
    {
        try
        {
            using(var driver = new WebDriver(network))
            {
                var data = driver.ReadData();
                if (data == null)
                {
                    Logger.Info("No data information for network, skipping.");
                    return;
                }

                var environment = new Dictionary<string, string>();
                if (data.IsInfinite)
                    environment["DATA_INFINITE"] = "1";
                else
                {
                    environment["DATA_AVAILABLE"] = data.GetAvailableMB().ToString();
                    if (data.UsedMB.HasValue)
                        environment["DATA_USED"] = data.UsedMB.Value.ToString();
                    if (data.TotalMB.HasValue)
                        environment["DATA_TOTAL"] = data.TotalMB.Value.ToString();
                }
                HookRunner.RunHooks(network, HookType.Data, Config.NetworkHook.OnlyWhen.Success);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());

            var environment = new Dictionary<string, string>{
                { "ERROR", ex.ToString() }
            };
            HookRunner.RunHooks(network, HookType.Data, Config.NetworkHook.OnlyWhen.Failure);
            HookRunner.RunHooks(network, HookType.Error, Config.NetworkHook.OnlyWhen.Always, environment);
            ExitCode = 1;
        }
    }
}
