namespace WifiAutologin.Util;

class DisposableWrapper : IDisposable
{
    static ILogger Logger { get; } = WifiAutologin.Logger.Global[typeof(DisposableWrapper)];

    public Dictionary<string, IDisposable> Disposables { get; private set; }

    public DisposableWrapper(Dictionary<string, IDisposable> disposables)
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
