namespace WifiAutologin.Util;

class DisposableCollector : IDisposable
{
    static ILogger Logger { get; } = WifiAutologin.Logger.Global[typeof(DisposableCollector)];

    public Dictionary<string, IDisposable> Disposables { get; private set; }

    public DisposableCollector(Dictionary<string, IDisposable> disposables)
    {
        Disposables = disposables;
    }

    public void Dispose()
    {
        Logger.Debug($"Disposing {Disposables.Count()} element(s)...");
        foreach (var disposable in Disposables)
            disposable.Value.Dispose();
    }
}
