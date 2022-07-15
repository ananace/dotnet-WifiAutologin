using System.Reflection;

namespace WifiAutologin.Util;

public static class IDiscoveryBackendExtensions
{
    public static string GetName(this IDiscoveryBackend backend)
    {
        var attr = backend.GetType().GetCustomAttribute<DiscoveryBackendAttribute>();
        if (attr != null)
            return attr.Name;

        return backend.GetType().Name;
    }
}
