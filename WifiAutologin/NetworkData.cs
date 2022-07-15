namespace WifiAutologin;

public class NetworkData
{
    public ulong? AvailableMB { get; set; }
    public ulong? UsedMB { get; set; }
    public ulong? TotalMB { get; set; }

    public bool IsInfinite => !TotalMB.HasValue && !AvailableMB.HasValue;

    public ulong GetAvailableMB()
    {
        if (AvailableMB.HasValue)
            return AvailableMB.Value;

        if (UsedMB.HasValue && TotalMB.HasValue)
            return TotalMB.Value - UsedMB.Value;

        return ulong.MaxValue;
    }
}
