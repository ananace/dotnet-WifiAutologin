namespace WifiAutologin.Util;

class DisposableWrapper : IDisposable
{
    static ILogger Logger { get; } = WifiAutologin.Logger.Global[typeof(DisposableWrapper)];

    public event Action? OnDispose;

    public void Dispose()
    {
        Logger.Debug("Running OnDispose actions...");
        OnDispose?.Invoke();
    }
}
