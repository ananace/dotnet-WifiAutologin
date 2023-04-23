using System.Diagnostics; // .FileVersionInfo;
using System.Reflection; // .Assembly;

namespace WifiAutologin;

public class AutoUpdate
{
    public static bool UpdateAvailable { get {
        return CurrentVersion != LatestVersion;
    } }

    public static Task<bool> Update()
    {
        return Task.FromResult(false);
    }

    public static string CurrentVersion { get; } = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "???";
    public static string LatestVersion { get {
        return CurrentVersion;
    } }
}
