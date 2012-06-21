using System;

namespace RunnersPal.Calculators
{
    public class CaloriesCalculator
    {
        private WeightCalculator weightCalculator = new WeightCalculator();

        public int Calculate(Distance distance, WeightData weightData)
        {
            if (distance.BaseDistance == 0) return 0;
            if (!(weightData.HasKg || weightData.HasLbs || weightData.HasSt || weightData.HasStLbs)) return 0;
            if (string.IsNullOrWhiteSpace(weightData.Units)) return 0;

            // ensure the weight data has all the up-to-date values
            weightCalculator.Calculate(weightData);
            if (!weightData.HasKg || weightData.Kg.Value <= 0) return 0;

            distance = distance.ConvertTo(DistanceUnits.Kilometers);
            return (int)Math.Floor(distance.BaseDistance * weightData.Kg.Value * 1.036);
        }
    }
}
