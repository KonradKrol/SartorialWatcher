namespace SartorialWatcher.Core.Utils;

public static class DoubleUtils
{
    extension(double value)
    {
        public double RoundTo(int places)
        {
            return Math.Round(
                value,
                places);
        }
    }
}