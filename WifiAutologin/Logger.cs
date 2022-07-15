namespace WifiAutologin;

internal class Logger : ILogger
{
    public static Logging.Listener Listener { get; } = new Logging.Listener();
    public static Logging.Source Source { get; } = Logging.Source.Instance;

    static System.Diagnostics.Tracing.EventLevel _level = System.Diagnostics.Tracing.EventLevel.Warning;
    public static System.Diagnostics.Tracing.EventLevel Level {
        get { return _level; }

        set {
            _level = value;

            Listener.DisableEvents(Source);
            Listener.EnableEvents(Source, value, System.Diagnostics.Tracing.EventKeywords.All);
        }
    }

    string? Name { get; set; }

    static Logger _Instance = new Logger(null);
    public static Logger Global => _Instance;

    Logger(string? name)
    {
        Name = name;
    }

    public ILogger this[object obj]
    {
        get {
            if (obj is Type type)
                return (ILogger)new Logger(type.FullName);
            else
                return (ILogger)new Logger(obj.GetType().FullName);
        }
    }

    public void Debug(string message) => Source.Debug(message, Name);
    public void Info(string message) => Source.Info(message, Name);
    public void Warn(string message) => Source.Warn(message, Name);
    public void Error(string message) => Source.Error(message, Name);
    public void Critical(string message) => Source.Critical(message, Name);
    public void Debug(string message, params object[] args) => Source.Debug(String.Format(message, args), Name);
    public void Info(string message, params object[] args) => Source.Info(String.Format(message, args), Name);
    public void Warn(string message, params object[] args) => Source.Warn(String.Format(message, args), Name);
    public void Error(string message, params object[] args) => Source.Error(String.Format(message, args), Name);
    public void Critical(string message, params object[] args) => Source.Critical(String.Format(message, args), Name);
}
