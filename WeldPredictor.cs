using System.Diagnostics;
using System.IO;

public static class WeldPredictor
{
    public static int GetWeldsRemaining(float temperature)
    {
        const int MaxTemperature = 40;
        int remaining = (int)(MaxTemperature - temperature);
        return Math.Max(0, remaining);
    }
}
