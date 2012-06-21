namespace RunnersPal.Calculators
{
    public class PaceData
    {
        public long? Route { get; set; }
        public Distance Distance { get; set; }
        public string Time { get; set; }
        public string Pace { get; set; }
        public double? PaceInSeconds { get; set; }
        public string Calc { get; set; }

        public bool HasRoute { get { return Route.HasValue && Route.Value > 0; } }
        public bool HasDistance { get { return Distance != null && Distance.BaseDistance > 0; } }
        public bool HasPace { get { return !string.IsNullOrWhiteSpace(Pace); } }
        public bool HasTime { get { return !string.IsNullOrWhiteSpace(Time); } }
    }
}