namespace Universes.Presentation
{
    public static class O_NumberFormatHelper
    {
        public static string Format(double value)
        {
            if (value >= 1_000_000_000)
                return (value / 1_000_000_000).ToString("0.##") + "B";
            if (value >= 1_000_000)
                return (value / 1_000_000).ToString("0.##") + "M";
            if (value >= 1_000)
                return (value / 1_000).ToString("0.##") + "K";
            if (value >= 100)
                return value.ToString("0");
            return value.ToString("0.#");
        }
    }
}
