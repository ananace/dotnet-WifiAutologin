using WifiAutologin.Util;

namespace WifiAutologin;

public class WebDriver : IDisposable
{
    static string FallbackUrl = "http://example.com";

    public Config.NetworkConfig Network { get; private set; }
    public OpenQA.Selenium.WebDriver Driver { get; private set; }

    public TimeSpan LoadTimeout { get; set; } = TimeSpan.FromSeconds(30);

    static ILogger Logger { get; } = WifiAutologin.Logger.Global[typeof(WebDriver)];
    int ActiveRequests = 0;

    public WebDriver(Config.NetworkConfig network)
    {
        Network = network;
        Driver = CreateWebDriver(network.Driver ?? Config.NetworkDriver.Automatic);

        SetupDriver(Driver);
    }

    void SetupDriver(OpenQA.Selenium.IWebDriver driver)
    {
        var opts = driver.Manage();
        opts.Timeouts().PageLoad = LoadTimeout;
        opts.Network.NetworkRequestSent += (ev, s) => {
            var active = ++ActiveRequests;
            Logger.Debug($"New request; {ev} for {s}. Currently {active} active.");
        };
        opts.Network.NetworkResponseReceived += (ev, s) => {
            var active = --ActiveRequests;
            Logger.Debug($"Request {ev} for {s} finished. Currently {active} active.");
        };
    }

    public void Login()
    {
        var url = Network.URL ?? Program.RedirectURL?.ToString() ?? Config.Instance.Fallback.URL ?? FallbackUrl;
        Logger.Debug($"Navigating to {url}");
        try
        {
            Driver.Navigate().GoToUrl(url);
        }
        catch (OpenQA.Selenium.WebDriverException ex)
        {
            Logger.Debug($"Failed to use driver, recreating - {ex}");
            Driver = CreateWebDriver(Network.Driver ?? Config.NetworkDriver.Automatic);
            SetupDriver(Driver);
            Driver.Navigate().GoToUrl(url);
        }

        // Apparently the network is actually logged in
        if (Driver.Url == FallbackUrl)
        {
            Logger.Info("Navigation to fallback URL succeeded, assuming working network.");
            return;
        }

        foreach (var action in Network.LoginActions)
        {
            var begin = DateTime.Now;
            Logger.Debug($"- {action.Action} {action.Element}");

            OpenQA.Selenium.IWebElement? element = null;
            if (action.Element != null)
                element = FindElement(action, begin);

            switch (action.Action)
            {
            case Config.NetworkActionType.Click:
            case Config.NetworkActionType.Input:
            case Config.NetworkActionType.Submit:
            case Config.NetworkActionType.Acquire:
                if (element == null && !(action.Dialog ?? false))
                    throw new Exception("Missing a web element for action");
                else if (action.Dialog ?? false)
                    ActOnPage(action, begin);
                else if (element != null)
                    ActOnElement(ref element, action, begin);
                else
                    throw new Exception("Element is null and dialog not triggered - this should never happen");
                break;

            default:
                ActOnPage(action, begin);
                break;
            }
        }

        if (Network.LoginActions.Last().Action != Config.NetworkActionType.Settle)
        {
            Logger.Debug("Allowing page a few seconds to settle after login...");
            ActOnPage(new Config.NetworkAction {
              Action = Config.NetworkActionType.Settle,
              Timeout = (float)2.0
            });
        }
    }

    public NetworkData? ReadData()
    {
        var data = new NetworkData();

        var url = Network.URL ?? Program.RedirectURL?.AbsoluteUri ?? Config.Instance.Fallback.URL;
        if (url == null)
            return null;

        Logger.Debug($"Navigating to {url}");
        try
        {
            Driver.Navigate().GoToUrl(url);
        }
        catch (OpenQA.Selenium.WebDriverException ex)
        {
            Logger.Debug($"Failed to use driver, recreating - {ex}");
            try
            {
                Driver.Close();
            }
            catch (Exception ex2)
            {
                Logger.Debug($"Failed to close driver, just disposing - {ex2}");
            }
            Driver.Dispose();
            Driver = CreateWebDriver(Network.Driver ?? Config.NetworkDriver.Automatic);
            Driver.Navigate().GoToUrl(url);
        }

        foreach (var action in Network.DataActions)
        {
            var begin = DateTime.Now;
            Logger.Debug($"- {action.Action} {action.Element}");

            OpenQA.Selenium.IWebElement? element = null;
            if (action.Element != null)
                element = FindElement(action, begin);

            switch (action.Action)
            {
            case Config.NetworkActionType.Click:
            case Config.NetworkActionType.Input:
            case Config.NetworkActionType.Submit:
            case Config.NetworkActionType.Acquire:
                if (element == null && !(action.Dialog ?? false))
                    throw new Exception("No element for action");

                // TODO: Support reading data information from dialogs
                if (action.Dialog ?? false)
                    ActOnPage(action, begin);
                else if (element != null)
                {
                    ActOnElement(ref element, action, begin);

                    if (action.Regex != null)
                    {
                        var rex = new System.Text.RegularExpressions.Regex(action.Regex);
                        var match = rex.Match(element.Text);
                        var dict = match.Groups.AsDictionary();

                        if (dict.ContainsKey("total_kb") && double.TryParse(dict["total_kb"], out var total_kb))
                            data.TotalMB = total_kb / 1024;
                        else if (dict.ContainsKey("total_mb") && double.TryParse(dict["total_mb"], out var total_mb))
                            data.TotalMB = total_mb;
                        else if (dict.ContainsKey("total_gb") && double.TryParse(dict["total_gb"], out var total_gb))
                            data.TotalMB = total_gb * 1024;

                        if (dict.ContainsKey("avail_kb") && double.TryParse(dict["avail_kb"], out var avail_kb))
                            data.AvailableMB = avail_kb / 1024;
                        else if (dict.ContainsKey("avail_mb") && double.TryParse(dict["avail_mb"], out var avail_mb))
                            data.AvailableMB = avail_mb;
                        else if (dict.ContainsKey("avail_gb") && double.TryParse(dict["avail_gb"], out var avail_gb))
                            data.AvailableMB = avail_gb * 1024;

                        if (dict.ContainsKey("used_kb") && double.TryParse(dict["used_kb"], out var used_kb))
                            data.UsedMB =  used_kb / 1024;
                        else if (dict.ContainsKey("used_mb") && double.TryParse(dict["used_mb"], out var used_mb))
                            data.UsedMB = used_mb;
                        else if (dict.ContainsKey("used_gb") && double.TryParse(dict["used_gb"], out var used_gb))
                            data.UsedMB = used_gb * 1024;
                    }
                }
                else
                    throw new Exception("Element is null and dialog not triggered - this should never happen");
                break;

            default:
                ActOnPage(action, begin);
                break;
            }
        }

        return data;
    }

    public void Dispose()
    {
        Logger.Debug("Disposing");
        Driver.Close();
        Driver.Dispose();
    }

    OpenQA.Selenium.IWebElement FindElement(Config.NetworkAction action, DateTime? start = null)
    {
        var startTime = start ?? DateTime.Now;
        var endTime = startTime + TimeSpan.FromSeconds(action.Timeout ?? 5);

        do
        {
            try
            {
                return Driver.FindElement(OpenQA.Selenium.By.CssSelector(action.Element));
            }
            catch (OpenQA.Selenium.NoSuchElementException)
            {
                if (DateTime.Now > endTime)
                    throw;
                else
                    System.Threading.Thread.Sleep(100);
            }
        } while (true);
    }

    void ActOnElement(ref OpenQA.Selenium.IWebElement element, Config.NetworkAction action, DateTime? startTime = null)
    {
        var start = startTime ?? DateTime.Now;
        var endTime = start + TimeSpan.FromSeconds(action.Timeout ?? 5);

        do
        {
            try
            {
                switch (action.Action)
                {
                    case Config.NetworkActionType.Click: element?.Click(); break;
                    case Config.NetworkActionType.Input: element?.SendKeys(action.Input); break;
                    case Config.NetworkActionType.Submit: element?.Submit(); break;
                    case Config.NetworkActionType.Acquire: Driver.ExecuteScript("arguments[0].scrollIntoView();", element); break;
                }
                break;
            }
            catch (OpenQA.Selenium.ElementNotInteractableException)
            {
                if (DateTime.Now > endTime)
                    throw;
                else
                    System.Threading.Thread.Sleep(100);
            }
            catch (OpenQA.Selenium.StaleElementReferenceException)
            {
                if (action.Element == null)
                    throw;

                var newElement = FindElement(action, start);
                if (DateTime.Now > endTime)
                    throw;

                element = newElement;
            }
        } while(DateTime.Now <= endTime);
    }

    void ActOnPage(Config.NetworkAction action, DateTime? startTime = null)
    {
        var start = startTime ?? DateTime.Now;
        var endTime = start + TimeSpan.FromSeconds(action.Timeout ?? 5);

        switch (action.Action)
        {
            case Config.NetworkActionType.Click:
            case Config.NetworkActionType.Input:
            case Config.NetworkActionType.Submit:
                if (!(action.Dialog ?? false))
                    throw new Exception("Only valid on dialog");

                {
                var alert = Driver.SwitchTo().Alert();
                if (!string.IsNullOrEmpty(action.Input))
                    alert?.SendKeys(action.Input);
                alert?.Accept();
                }
                break;

            case Config.NetworkActionType.Dismiss:
                if (!(action.Dialog ?? false))
                    throw new Exception("Only valid on dialog");

                {
                var alert = Driver.SwitchTo().Alert();
                if (!string.IsNullOrEmpty(action.Input))
                    alert?.SendKeys(action.Input);
                alert?.Dismiss();
                }
                break;

            case Config.NetworkActionType.Script: Driver.ExecuteScript(action.Script); break;
            case Config.NetworkActionType.Sleep: System.Threading.Thread.Sleep((int)((action.Sleep ?? 0.25) * 1000)); break;
            case Config.NetworkActionType.Settle:
                string source;

                do
                {
                    source = Driver.PageSource;
                    Thread.Sleep(500);

                    // Check that the DOM is ready
                    var result = Driver.ExecuteScript("return document.readyState");
                    if (result != null && (string)result != "complete")
                    {
                        Logger.Debug("  Document readyState != complete, waiting");
                        continue;
                    }

                    // Check if any active requests are still outstanding
                    if (ActiveRequests > 0)
                    {
                        Logger.Debug("  Active WebDriver requests, waiting");
                        continue;
                    }

                    // Check if DOM has modified
                    if (Driver.PageSource != source)
                    {
                        Logger.Debug("  DOM modified since last check, waiting");
                        continue;
                    }

                    // TODO: Check other things?

                    break;
                } while(DateTime.Now < endTime);

                if (DateTime.Now >= endTime)
                    Logger.Debug("Settle attempt timed out, continuing.");
                else
                    Logger.Debug("Page has settled, continuing.");

                break;
        }
    }

    static Dictionary<Config.NetworkDriver, Func<OpenQA.Selenium.WebDriver>> WebDriverFactories = new Dictionary<Config.NetworkDriver, Func<OpenQA.Selenium.WebDriver>>{
        //{ Config.NetworkDriver.PhantomJS, () => new OpenQA.Selenium.PhantomJS.PhantomJSDriver() },
        {
            Config.NetworkDriver.Firefox, () =>
            {
                var opts = new OpenQA.Selenium.Firefox.FirefoxOptions();
                opts.AddArguments("--headless");
                opts.SetEnvironmentVariable("MOZ_HEADLESS", "1");
                opts.SetEnvironmentVariable("MOZ_REMOTE_SETTINGS_DEVTOOLS", "1");
                opts.AcceptInsecureCertificates = true;
                opts.LogLevel = OpenQA.Selenium.Firefox.FirefoxDriverLogLevel.Error;

                opts.SetLoggingPreference(OpenQA.Selenium.LogType.Browser, OpenQA.Selenium.LogLevel.Warning);
                opts.SetLoggingPreference(OpenQA.Selenium.LogType.Client, OpenQA.Selenium.LogLevel.Severe);
                opts.SetLoggingPreference(OpenQA.Selenium.LogType.Driver, OpenQA.Selenium.LogLevel.Severe);
                opts.SetLoggingPreference(OpenQA.Selenium.LogType.Server, OpenQA.Selenium.LogLevel.Severe);
                opts.SetLoggingPreference(OpenQA.Selenium.LogType.Profiler, OpenQA.Selenium.LogLevel.Off);

                return new OpenQA.Selenium.Firefox.FirefoxDriver(opts);
            }
        },
        {
            Config.NetworkDriver.Edge, () =>
            {
                var opts = new OpenQA.Selenium.Edge.EdgeOptions();
                opts.AcceptInsecureCertificates = true;
                opts.AddArguments("--headless", "--disable-gpu");

                opts.SetLoggingPreference(OpenQA.Selenium.LogType.Browser, OpenQA.Selenium.LogLevel.Warning);
                opts.SetLoggingPreference(OpenQA.Selenium.LogType.Client, OpenQA.Selenium.LogLevel.Severe);
                opts.SetLoggingPreference(OpenQA.Selenium.LogType.Driver, OpenQA.Selenium.LogLevel.Severe);
                opts.SetLoggingPreference(OpenQA.Selenium.LogType.Server, OpenQA.Selenium.LogLevel.Severe);
                opts.SetLoggingPreference(OpenQA.Selenium.LogType.Profiler, OpenQA.Selenium.LogLevel.Off);

                return new OpenQA.Selenium.Edge.EdgeDriver(opts);
            }
        },
        {
            Config.NetworkDriver.Chrome, () =>
            {
                var opts = new OpenQA.Selenium.Chrome.ChromeOptions();
                opts.AcceptInsecureCertificates = true;
                opts.AddArguments("--headless", "--disable-gpu");

                opts.SetLoggingPreference(OpenQA.Selenium.LogType.Browser, OpenQA.Selenium.LogLevel.Warning);
                opts.SetLoggingPreference(OpenQA.Selenium.LogType.Client, OpenQA.Selenium.LogLevel.Severe);
                opts.SetLoggingPreference(OpenQA.Selenium.LogType.Driver, OpenQA.Selenium.LogLevel.Severe);
                opts.SetLoggingPreference(OpenQA.Selenium.LogType.Server, OpenQA.Selenium.LogLevel.Severe);
                opts.SetLoggingPreference(OpenQA.Selenium.LogType.Profiler, OpenQA.Selenium.LogLevel.Off);

                return new OpenQA.Selenium.Chrome.ChromeDriver(opts);
            }
        },
    };
    private static OpenQA.Selenium.WebDriver CreateWebDriver(Config.NetworkDriver preferred)
    {
        OpenQA.Selenium.WebDriver? driver = null;

        if (preferred == Config.NetworkDriver.Automatic)
        {
            Logger.Debug("Finding first available webdriver...");
            Config.NetworkDriver type = Config.NetworkDriver.Automatic;
            foreach (var factory in WebDriverFactories)
            {
                Logger.Debug($"Trying {factory.Key}");
                try
                {
                    driver = factory.Value();
                    type = factory.Key;
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to initialize {factory.Key} - {ex}");
                }
            }

            if (type == Config.NetworkDriver.Automatic || driver == null)
            {
                throw new Exception("Failed to initialize any webdriver");
            }

            Logger.Debug($"Found default driver as {type}");
        }
        else
        {
            if (!WebDriverFactories.ContainsKey(preferred))
                return CreateWebDriver(Config.NetworkDriver.Automatic);

            var factory = WebDriverFactories[preferred];
            driver = factory();
        }

        if (driver == null)
            throw new Exception("Failed to create a web driver");

        return driver;
    }
}
