using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace WifiAutologin;

public class Config
{
    static ILogger Logger { get; } = WifiAutologin.Logger.Global[typeof(Config)];

    public enum NetworkDriver {
        Automatic,

        PhantomJS,
        Chrome,
        Firefox,
        Edge,

        Auto = Automatic,
        Selenium = Automatic,
        Poltergeist = PhantomJS,
        Chromium = Chrome,
    }

    public class NetworkHook
    {
        public enum OnlyWhen {
            Success,
            Failure,
            Always
        }

        public string Hook { get; set; } = "";
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? Unless { get; set; }
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public OnlyWhen When { get; set; } = OnlyWhen.Success;
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? If { get; set; }
        [YamlMember(Alias = "final", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public bool Break { get; set; } = false;

        public void LoadFromNode(YamlNode node)
        {
            if (node is YamlMappingNode mapping)
            {
                Hook = mapping.Children[new YamlScalarNode("hook")].ToString();
                if (mapping.Children.ContainsKey(new YamlScalarNode("when")))
                    When = Enum.Parse<OnlyWhen>(mapping.Children[new YamlScalarNode("when")].ToString(), true);
                else
                    When = OnlyWhen.Success;
                if (mapping.Children.ContainsKey(new YamlScalarNode("unless")))
                    Unless = mapping.Children[new YamlScalarNode("unless")].ToString();
                if (mapping.Children.ContainsKey(new YamlScalarNode("if")))
                    If = mapping.Children[new YamlScalarNode("if")].ToString();
                if (mapping.Children.ContainsKey(new YamlScalarNode("final")))
                    Break = bool.Parse(mapping.Children[new YamlScalarNode("final")].ToString());
            }
            else
            {
                Hook = node.ToString();
                When = OnlyWhen.Success;
            }
        }

        public static NetworkHook ParseFromNode(YamlNode node)
        {
            var hook = new NetworkHook();

            hook.LoadFromNode(node);
            return hook;
        }
    }

    public class NetworkHooks
    {
        [YamlMember(Alias = "pre-login", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)]
        public List<NetworkHook>? PreLogin { get; set; } = null;
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)]
        public List<NetworkHook>? Login { get; set; } = null;
        [YamlMember(Alias = "post-login", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)]
        public List<NetworkHook>? PostLogin { get; set; } = null;
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)]
        public List<NetworkHook>? Data { get; set; } = null;
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)]
        public List<NetworkHook>? Error { get; set; } = null;

        public void Merge(NetworkHooks source)
        {
            if (PreLogin == null)
                PreLogin = new List<NetworkHook>(source.PreLogin ?? new List<NetworkHook>());
            if (Login == null)
                Login = new List<NetworkHook>(source.Login ?? new List<NetworkHook>());
            if (PostLogin == null)
                PostLogin = new List<NetworkHook>(source.PostLogin ?? new List<NetworkHook>());
            if (Data == null)
                Data = new List<NetworkHook>(source.Data ?? new List<NetworkHook>());
            if (Error == null)
                Error = new List<NetworkHook>(source.Error ?? new List<NetworkHook>());
        }

        public void LoadFromNode(YamlNode node)
        {
            var mapping = node as YamlMappingNode;
            if (mapping == null)
                throw new ArgumentException("Not a mapping node");

            if (mapping.Children.ContainsKey(new YamlScalarNode("pre-login")))
                PreLogin = ((YamlSequenceNode)mapping.Children[new YamlScalarNode("pre-login")]).Select(n => NetworkHook.ParseFromNode(n)).ToList();
            if (mapping.Children.ContainsKey(new YamlScalarNode("login")))
                Login = ((YamlSequenceNode)mapping.Children[new YamlScalarNode("login")]).Select(n => NetworkHook.ParseFromNode(n)).ToList();
            if (mapping.Children.ContainsKey(new YamlScalarNode("post-login")))
                PostLogin = ((YamlSequenceNode)mapping.Children[new YamlScalarNode("post-login")]).Select(n => NetworkHook.ParseFromNode(n)).ToList();
            if (mapping.Children.ContainsKey(new YamlScalarNode("data")))
                Data = ((YamlSequenceNode)mapping.Children[new YamlScalarNode("data")]).Select(n => NetworkHook.ParseFromNode(n)).ToList();
            if (mapping.Children.ContainsKey(new YamlScalarNode("error")))
                Error = ((YamlSequenceNode)mapping.Children[new YamlScalarNode("error")]).Select(n => NetworkHook.ParseFromNode(n)).ToList();
        }
    }

    public enum NetworkActionType
    {
        Click,
        Input,
        Submit,
        Script,
        Sleep,
        Settle,
        Acquire,

        // Dialog-unique action
        Dismiss
    }

    public class NetworkAction
    {
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public NetworkActionType? Action { get; set; }
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool? Dialog { get; set; }
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? Element { get; set; }
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? Script { get; set; }
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? Input { get; set; }
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? Regex { get; set; }
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public float? Sleep { get; set; }
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public float? Timeout { get; set; }

        public void LoadFromNode(YamlNode node)
        {
            if (node is YamlMappingNode mapping)
            {
                string[] accessedKeys = new string[] {
                    "action",
                    "dialog",
                    "element",
                    "input",
                    "regex",
                    "script",
                    "sleep"
                };

                if (mapping.Children.Any(c => accessedKeys.Contains(c.Key.ToString())))
                {
                    if (mapping.Children.ContainsKey(new YamlScalarNode("action")))
                        Action = Enum.Parse<NetworkActionType>(mapping.Children[new YamlScalarNode("action")].ToString(), true);
                    else
                    {
                        if (mapping.Children.ContainsKey(new YamlScalarNode("script")))
                            Action = NetworkActionType.Script;
                        else if (mapping.Children.ContainsKey(new YamlScalarNode("input")))
                            Action = NetworkActionType.Input;
                        else if (mapping.Children.ContainsKey(new YamlScalarNode("sleep")))
                            Action = NetworkActionType.Sleep;
                        else if (mapping.Children.ContainsKey(new YamlScalarNode("regex"))
                                 || mapping.Children.ContainsKey(new YamlScalarNode("acquire")))
                            Action = NetworkActionType.Acquire;
                        else
                            Action = NetworkActionType.Click;
                    }

                    if (mapping.Children.ContainsKey(new YamlScalarNode("dialog")))
                        Dialog = true; //mapping.Children[new YamlScalarNode("dialog")];

                    if (mapping.Children.ContainsKey(new YamlScalarNode("script")))
                        Script = mapping.Children[new YamlScalarNode("script")].ToString();
                    else if (mapping.Children.ContainsKey(new YamlScalarNode("input")))
                        Input = mapping.Children[new YamlScalarNode("input")].ToString();
                    else if (mapping.Children.ContainsKey(new YamlScalarNode("sleep")))
                        Sleep = float.Parse(mapping.Children[new YamlScalarNode("sleep")].ToString());
                    else if (mapping.Children.ContainsKey(new YamlScalarNode("regex")))
                        Sleep = float.Parse(mapping.Children[new YamlScalarNode("regex")].ToString());

                    if (mapping.Children.ContainsKey(new YamlScalarNode("element")))
                        Element = mapping.Children[new YamlScalarNode("element")].ToString();
                    else if (mapping.Children.ContainsKey(new YamlScalarNode("acquire")))
                        Element = mapping.Children[new YamlScalarNode("acquire")].ToString();

                    if (mapping.Children.ContainsKey(new YamlScalarNode("timeout")))
                        Timeout = float.Parse(mapping.Children[new YamlScalarNode("timeout")].ToString());
                }
                else
                {
                    Action = NetworkActionType.Acquire;
                    Element = mapping.Children.First().Key.ToString();
                    Regex = mapping.Children.First().Value.ToString();
                }
            }
            else
            {
                Action = NetworkActionType.Click;
                Element = node.ToString();
            }
        }

        public static NetworkAction ParseFromNode(YamlNode node)
        {
            var action = new NetworkAction();
            action.LoadFromNode(node);
            return action;
        }
    }

    public class NetworkConfig
    {
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public NetworkDriver? Driver { get; set; }
        [YamlMember(Alias = "ssid", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? SSID { get; set; }
        [YamlMember(Alias = "url", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? URL { get; set; }
        [YamlMember(Alias = "test-url", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? TestURL { get; set; }
        [YamlMember(Alias = "always-run-hooks", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public bool AlwaysHooks { get; set; } = false;
        [YamlMember(Alias = "hooks", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public NetworkHooks Hooks { get; set; } = new NetworkHooks();
        [YamlMember(Alias = "login", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public List<NetworkAction> LoginActions { get; set; } = new List<NetworkAction>();
        [YamlMember(Alias = "data", DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
        public List<NetworkAction> DataActions { get; set; } = new List<NetworkAction>();

        [YamlIgnore]
        public bool HasData => DataActions.Any();

        public void Merge(NetworkConfig global)
        {
            if (Driver == null)
                Driver = global.Driver;

            Hooks.Merge(global.Hooks);
        }

        public void LoadFromNode(YamlNode node)
        {
            var mapping = node as YamlMappingNode;
            if (mapping == null)
                throw new ArgumentException("Not a mapping node");

            if (mapping.Children.ContainsKey(new YamlScalarNode("ssid")))
                SSID = mapping.Children[new YamlScalarNode("ssid")].ToString();
            if (mapping.Children.ContainsKey(new YamlScalarNode("url")))
                URL = mapping.Children[new YamlScalarNode("url")].ToString();
            if (mapping.Children.ContainsKey(new YamlScalarNode("test-url")))
                TestURL = mapping.Children[new YamlScalarNode("test-url")].ToString();
            if (mapping.Children.ContainsKey(new YamlScalarNode("always-run-hooks")))
                AlwaysHooks = bool.Parse(mapping.Children[new YamlScalarNode("always-run-hooks")].ToString());
            if (mapping.Children.ContainsKey(new YamlScalarNode("driver")))
                Driver = Enum.Parse<NetworkDriver>(mapping.Children[new YamlScalarNode("driver")].ToString(), true);
            if (mapping.Children.ContainsKey(new YamlScalarNode("hooks")))
                Hooks.LoadFromNode(mapping.Children[new YamlScalarNode("hooks")]);
            if (mapping.Children.ContainsKey(new YamlScalarNode("login")))
                LoginActions.AddRange(((YamlSequenceNode)mapping.Children[new YamlScalarNode("login")]).Select(n => NetworkAction.ParseFromNode(n)));
            if (mapping.Children.ContainsKey(new YamlScalarNode("data")))
                DataActions.AddRange(((YamlSequenceNode)mapping.Children[new YamlScalarNode("data")]).Select(n => NetworkAction.ParseFromNode(n)));
        }

        public static NetworkConfig ParseFromNode(YamlNode node, string ssid)
        {
            if (!(node is YamlMappingNode))
                throw new ArgumentException("Not a mapping node");

            var config = new NetworkConfig();
            config.SSID = ssid;
            config.LoadFromNode(node);
            return config;
        }
    }


    public class EnumTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type) { return type == typeof(NetworkActionType) || type == typeof(NetworkDriver); }

        public object? ReadYaml(IParser parser, Type type) { return null; }

        public void WriteYaml(IEmitter emitter, object? val, Type type)
        {
            emitter.Emit(new YamlDotNet.Core.Events.Scalar(val?.ToString()?.ToLower() ?? ""));
        }
    }


    static DateTime LastWrite;
    static bool ReloadNecessary()
    {
        return File.GetLastWriteTimeUtc(ConfigPath) > LastWrite;
    }

    public static string ExampleConfig()
    {
        Config example = new Config();

        var hooks = example.Fallback.Hooks;
        hooks.PreLogin = new List<NetworkHook>() {
            new NetworkHook {
                Hook = "nmcli c down id VPN-Connection",
                If = "nmcli c show --active id VPN-Connection | grep connection.id",
            }
        };
        hooks.Login = new List<NetworkHook>() {
            new NetworkHook {
                Hook = "notify-send -i network-wireless-hotspot -u low -a wifi-autologin \"Wifi Autologin\" \"Logging into ${NETWORK}\""
            }
        };
        hooks.PostLogin = new List<NetworkHook>() {
            new NetworkHook {
                Hook = "notify-send -i network-wireless-hotspot -u low -a wifi-autologin \"Wifi Autologin\" \"Automatically logged into ${NETWORK}\""
            },
            new NetworkHook {
                Hook = "nmcli c up id VPN-Connection",
                Unless = "nmcli c show --active id VPN-Connection | grep connection.id",
            }
        };
        hooks.Error = new List<NetworkHook>() {
            new NetworkHook {
                Hook = "notify-send -i network-wireless-hotspot -u low -a wifi-autologin \"Wifi Autologin\" \"Failed to log into ${NETWORK}, ${ERROR}\""
            }
        };

        var network = new NetworkConfig() {
            SSID = "example",
            LoginActions = new List<NetworkAction>() {
                new NetworkAction {
                    Action = NetworkActionType.Input,
                    Element = "#email",
                    Input = "my-email@example.com"
                },
                new NetworkAction {
                    Action = NetworkActionType.Click,
                    Element = "#accept-checkbox"
                },
                new NetworkAction {
                    Action = NetworkActionType.Sleep,
                    Sleep = 0.5f
                },
                new NetworkAction {
                    Action = NetworkActionType.Script,
                    Script = "$(\"form#submit-form\").submit()"
                }
            }
        };

        example.Networks = new List<NetworkConfig>() {
            network
        };

        var yamlWriter = new StringWriter();
        example.Serialize(yamlWriter);

        return yamlWriter.ToString();
    }

    public static string ConfigPath => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "wifi.yml");

    static Config? _instance = null;
    public static Config Instance { get {
        if (ReloadNecessary())
        {
            Logger.Info("Configuration change detected, reloading.");
            _instance = null;
        }

        if (_instance == null)
        {
            _instance = new Config();

            Logger.Info($"Loading configuration from {ConfigPath}...");

            LastWrite = File.GetLastWriteTimeUtc(ConfigPath);
            using (var input = File.OpenRead(ConfigPath))
            using (var reader = new StreamReader(input))
            {
                var yaml = new YamlStream();
                yaml.Load(reader);

                bool legacy = true;

                var root = (YamlMappingNode)yaml.Documents[0].RootNode;
                foreach (var network in root.Children)
                {
                    if (network.Key.ToString() == "_global" || network.Key.ToString() == "defaults")
                    {
                        Logger.Debug("Loading globals from config...");
                        _instance.Fallback.LoadFromNode(network.Value);

                        if (network.Key.ToString() == "defaults")
                            legacy = false;
                    }
                    else if (network.Key.ToString() == "networks")
                    {
                        legacy = false;

                        foreach (var netConfig in ((YamlMappingNode)network.Value).Children)
                        {
                            Logger.Debug($"Loading network {network.Key} from config...");
                            var parsed = NetworkConfig.ParseFromNode(network.Value, network.Key.ToString());
                            _instance.Networks.Add(parsed);
                        }
                    }
                    else if (legacy)
                    {
                        Logger.Debug($"Loading legacy network {network.Key} from config...");
                        var parsed = NetworkConfig.ParseFromNode(network.Value, network.Key.ToString());
                        _instance.Networks.Add(parsed);
                    }
                    else
                        Logger.Warn($"Found invalid key {network.Key} in config, ignoring");
                }
            }
        }

        return _instance;
    } }

    public void Serialize(System.IO.TextWriter output)
    {
        var serializer = new YamlDotNet.Serialization.SerializerBuilder()
            .WithIndentedSequences()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .WithTypeConverter(new Config.EnumTypeConverter())
            .Build();

        serializer.Serialize(output, this);
    }

    [YamlMember(Alias = "defaults")]
    public NetworkConfig Fallback { get; set; } = new NetworkConfig();
    public List<NetworkConfig> Networks { get; set; } = new List<NetworkConfig>();
}
