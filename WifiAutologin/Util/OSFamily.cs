namespace WifiAutologin.Util;

[Flags]
public enum OSFamily
{
    None    = 0,

    Linux   = 1 << 0,
    Windows = 1 << 1,

    All = 0xFF
}
