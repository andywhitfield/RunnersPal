namespace RunnersPal.Calculators
{
    public class DistanceData
    {
        public double? DistanceM { get; set; }
        public double? DistanceKm { get; set; }
        public string Calc { get; set; }

        public bool HasDistanceM { get { return DistanceM.HasValue; } }
        public bool HasDistanceKm { get { return DistanceKm.HasValue; } }
    }
}
