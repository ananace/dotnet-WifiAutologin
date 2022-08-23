namespace WifiAutologin;

public class NetworkData
{
    public double? AvailableMB { get; set; }
    public double? UsedMB { get; set; }
    public double? TotalMB { get; set; }

    public bool IsInfinite => !TotalMB.HasValue && !AvailableMB.HasValue;

    public double GetAvailableMB()
    {
        if (AvailableMB.HasValue)
            return AvailableMB.Value;

        if (UsedMB.HasValue && TotalMB.HasValue)
            return TotalMB.Value - UsedMB.Value;

        // Probably needs a way for a user to notice if this is hit
        if (TotalMB.HasValue)
            return TotalMB.Value;

        return double.MaxValue;
    }
}
