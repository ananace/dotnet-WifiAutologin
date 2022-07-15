using System.Diagnostics.Tracing;

namespace WifiAutologin.Logging;

public class Listener : EventListener
{
    static readonly Dictionary<EventLevel, string> EventLevelMapping = new Dictionary<EventLevel, string>{
        { EventLevel.Verbose,       "DEBUG" },
        { EventLevel.Informational, " INFO" },
        { EventLevel.Warning,       " WARN" },
        { EventLevel.Error,         "ERROR" },
        { EventLevel.Critical,      " CRIT" },
    };

    protected override void OnEventSourceCreated(EventSource source)
    {
        base.OnEventSourceCreated(source);

        if (source.Name == "WifiAutologin")
            EnableEvents(source, EventLevel.Warning, EventKeywords.All);
    }

    protected override void OnEventWritten(EventWrittenEventArgs ev)
    {
        var module = ev.Payload?[0] ?? ev.EventSource.Name;
        var message = ev.Payload?[1] ?? "";

        var result = $"{EventLevelMapping[ev.Level]}  {module} : {message}";

        if (ev.Level <= EventLevel.Warning)
            Console.Out.WriteLine(result);
        else
            Console.Error.WriteLine(result);

        base.OnEventWritten(ev);
    }
}
