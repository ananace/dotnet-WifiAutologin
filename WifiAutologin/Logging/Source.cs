using System.Diagnostics.Tracing;

namespace WifiAutologin.Logging;

[EventSource(Name = NAME)]
public class Source : EventSource
{
    const string NAME = "WifiAutologin";

    private Source() : base(EventSourceSettings.EtwSelfDescribingEventFormat) { }

    public static Source Instance { get; } = new();

    [Event(1, Level = EventLevel.Verbose)]
    public void Debug(string message, string? module = null)
    {
        if (!IsEnabled())
            return;

        WriteEvent(1, module ?? NAME, message);
    }

    [Event(2, Level = EventLevel.Informational)]
    public void Info(string message, string? module = null)
    {
        if (!IsEnabled())
            return;

        WriteEvent(2, module ?? NAME, message);
    }

    [Event(3, Level = EventLevel.Warning)]
    public void Warn(string message, string? module = null)
    {
        if (!IsEnabled())
            return;

        WriteEvent(3, module ?? NAME, message);
    }

    [Event(4, Level = EventLevel.Error)]
    public void Error(string message, string? module = null)
    {
        if (!IsEnabled())
            return;

        WriteEvent(4, module ?? NAME, message);
    }

    [Event(5, Level = EventLevel.Critical)]
    public void Critical(string message, string? module = null)
    {
        if (!IsEnabled())
            return;

        WriteEvent(5, module ?? NAME, message);
    }
}
