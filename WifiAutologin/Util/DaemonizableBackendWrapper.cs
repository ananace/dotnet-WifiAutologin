using System.Linq;

namespace WifiAutologin.Util;

public class DaemonizableBackendWrapper : IDiscoveryBackend
{
    IDiscoveryBackend Backend;

    static ILogger Logger { get; } = WifiAutologin.Logger.Global[typeof(DaemonizableBackendWrapper)];

    public bool IsAvailable => Backend != null;
    public bool IsConnected => Backend.IsConnected;
    public bool IsConnectedToVPN => false;
    public bool SupportsDaemonize => false;

    public double PollDelaySeconds = 10;

    public DaemonizableBackendWrapper(IDiscoveryBackend Backend)
    {
        this.Backend = Backend;
    }

    List<string> LastConnected = new List<string>();
    public IEnumerable<string> ConnectedNetworks { get { return LastConnected; } }

    public IDisposable WatchChanges(Action<IDiscoveryBackend> handler)
    {
        if (Backend == null)
            throw new Exception("No underlying backend.");

        if (!LastConnected.Any())
            LastConnected = ConnectedNetworks.ToList();

        var cts = new CancellationTokenSource();
        var token = cts.Token;

        var backgroundWorker = Task.Run(async () => {
            Logger.Debug($"Starting polling wrapper around {Backend} for connection tracking");
            while (true) {
                token.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(PollDelaySeconds), token);

                var currentlyConnected = Backend.ConnectedNetworks.ToList();
                var newConnections = currentlyConnected.Except(LastConnected);
                LastConnected = currentlyConnected;

                if (newConnections.Any())
                    handler(this);
            }
        }, token);

        var wrapper = new DisposableWrapper();
        wrapper.OnDispose += () => {
            cts.Cancel();
            try {
                backgroundWorker.Wait();
            } catch(AggregateException ex) {
                if (!(ex.InnerException is TaskCanceledException))
                    throw;
            }
        };

        var collection = new DisposableCollector(new Dictionary<string, IDisposable> {
            { "wrapper", wrapper },
            { "task", backgroundWorker },
            { "tokenSource", cts }
        });

        return collection;
    }
}
