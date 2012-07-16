using System;

namespace RunnersPal.Calculators
{
    public class WeightCalculator
    {
        private const double PoundsToKg = 0.45359237;
        private const double KgToPounds = 1/PoundsToKg;

        public WeightData Calculate(WeightData weightData)
        {
            switch (weightData.Units)
            {
                case "kg":
                    FromKilograms(weightData);
                    break;
                case "lbs":
                    FromPounds(weightData);
                    break;
                case "st":
                    FromStoneAndPounds(weightData);
                    break;
            }

            return weightData;
        }

        public void FromKilograms(WeightData weightData)
        {
            if (!weightData.HasKg) return;
            weightData.Lbs = weightData.Kg.Value * KgToPounds;
            weightData.St = Math.Floor(weightData.Lbs.Value / 14);
            weightData.StLbs = weightData.Lbs.Value - (weightData.St.Value * 14);
        }

        public void FromPounds(WeightData weightData)
        {
            if (!weightData.HasLbs) return;
            weightData.Kg = weightData.Lbs.Value * PoundsToKg;
            weightData.St = Math.Floor(weightData.Lbs.Value / 14);
            weightData.StLbs = weightData.Lbs.Value - (weightData.St.Value * 14);
        }

        public void FromStoneAndPounds(WeightData weightData)
        {
            if (!weightData.HasSt && !weightData.HasStLbs) return;
            weightData.Lbs = (weightData.HasSt ? weightData.St.Value * 14 : 0) +
                (weightData.HasStLbs ? weightData.StLbs.Value : 0);
            weightData.Kg = weightData.Lbs.Value * PoundsToKg;
        }
    }
}
