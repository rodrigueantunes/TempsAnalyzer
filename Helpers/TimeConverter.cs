namespace TempsAnalyzer.Helpers
{
    public static class TimeConverter
    {
        public static TimeSpan ConvertToHours(double journee)
        {
            return TimeSpan.FromHours(journee * 8);
        }

        public static string FormatTime(double journee)
        {
            var heures = ConvertToHours(journee);
            return $"{(int)heures.TotalHours}h {heures.Minutes}min";
        }
    }
}
