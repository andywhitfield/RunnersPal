namespace RunnersPal.Calculators
{
    public class DistanceCalculator
    {
        public static readonly double KilometersToMiles;
        public static readonly double MilesToKilometers;

        static DistanceCalculator()
        {
            var calculator = new DistanceCalculator();
            var distanceData = new DistanceData { DistanceM = 1, Calc = "kilometers" };
            calculator.Calculate(distanceData);
            MilesToKilometers = distanceData.DistanceKm.Value;

            distanceData.DistanceKm = 1;
            distanceData.Calc = "miles";
            calculator.Calculate(distanceData);
            KilometersToMiles = distanceData.DistanceM.Value;
        }

        public void Calculate(DistanceData distanceData)
        {
            if ((distanceData.Calc ?? "").ToLower() == "miles")
            {
                // convert KM to Miles
                var kilometers = distanceData.HasDistanceKm ? distanceData.DistanceKm.Value : 0;
                if (kilometers == 0)
                {
                    if (!distanceData.HasDistanceM) distanceData.DistanceM = null;
                    return;
                }

                var meters = kilometers * 1000;
                var inches = meters / 0.0254;
                distanceData.DistanceM = inches / 63360;
            }
            else
            {
                // convert Miles to KM
                double miles = distanceData.HasDistanceM ? distanceData.DistanceM.Value : 0;
                if (miles == 0)
                {
                    if (!distanceData.HasDistanceKm) distanceData.DistanceKm = null;
                    return;
                }

                var inches = miles * 63360; // 1 mile = 63360 inches
                var meters = inches * 0.0254; // 1 inch = 0.0254 meters
                distanceData.DistanceKm = meters / 1000;
            }
        }
    }
}
