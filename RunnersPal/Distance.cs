using System;
using RunnersPal.Calculators;

namespace RunnersPal
{
    public class Distance
    {
        public Distance(double distance, DistanceUnits units)
        {
            var calculator = new DistanceCalculator();
            var distanceData = new DistanceData { DistanceM = distance, DistanceKm = distance };

            switch (units)
            {
                case DistanceUnits.Miles:
                    distanceData.Calc = "kilometers";
                    break;
                case DistanceUnits.Kilometers:
                    distanceData.Calc = "miles";
                    break;
                default:
                    throw new ArgumentException("Unknown units: " + units, "units");
            }

            calculator.Calculate(distanceData);
            DistanceInM = distanceData.DistanceM.Value;
            DistanceInKm = distanceData.DistanceKm.Value;
            BaseDistance = distance;
            BaseUnits = units;
        }

        public double DistanceInM { get; private set; }
        public double DistanceInKm { get; private set; }

        public double BaseDistance { get; private set; }
        public DistanceUnits BaseUnits { get; private set; }

        public Distance ConvertTo(DistanceUnits toUnits)
        {
            switch (toUnits)
            {
                case DistanceUnits.Miles:
                    if (this.BaseUnits == DistanceUnits.Miles) return this;
                    return new Distance(this.DistanceInM, DistanceUnits.Miles);
                case DistanceUnits.Kilometers:
                    if (this.BaseUnits == DistanceUnits.Kilometers) return this;
                    return new Distance(this.DistanceInKm, DistanceUnits.Kilometers);
                default:
                    return this;
            }
        }

        public override string ToString()
        {
            return BaseDistance.ToString("0.##") + " " + BaseUnits;
        }
    }
}
