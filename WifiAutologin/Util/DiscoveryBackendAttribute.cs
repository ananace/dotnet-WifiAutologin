namespace WifiAutologin.Util;

[AttributeUsage(AttributeTargets.Class)]
internal class DiscoveryBackendAttribute : Attribute
{
    public string Name { get; private set; }
    public OSFamily OSes { get; set; } = OSFamily.All;

    public DiscoveryBackendAttribute(string name)
    {
        Name = name;
    }
}
