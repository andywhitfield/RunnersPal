using System.ComponentModel.DataAnnotations;

namespace RunnersPal.Calculators
{
    public class WeightData
    {
        private readonly WeightCalculator weightCalculator = new WeightCalculator();

        public double? UnitWeight
        {
            get
            {
                switch (Units)
                {
                    case "kg": return Kg;
                    case "lbs": return Lbs;
                    case "st": return HasSt && HasStLbs ? (St * 14) + StLbs : null;
                }
                return null;
            }
        }
        public double? Kg { get; set; }
        public double? Lbs { get; set; }
        public double? St { get; set; }
        public double? StLbs { get; set; }
        [Required]
        public string Units { get; set; }

        public bool HasKg { get { return Kg.HasValue; } }
        public bool HasLbs { get { return Lbs.HasValue; } }
        public bool HasSt { get { return St.HasValue; } }
        public bool HasStLbs { get { return StLbs.HasValue; } }

        public void UpdateFromUnits()
        {
            weightCalculator.Calculate(this);
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj)) return true;
            if (obj == null) return false;
            var w1 = obj as WeightData;
            if (w1 == null) return false;
            return Units == w1.Units && UnitWeight == w1.UnitWeight;
        }

        public override int GetHashCode()
        {
            return Units.GetHashCode() + (UnitWeight.HasValue ? UnitWeight.GetHashCode() : 0);
        }

        public static bool operator ==(WeightData w1, WeightData w2)
        {
            if (object.ReferenceEquals(w1, w2)) return true;
            if (object.ReferenceEquals(w1, null) || object.ReferenceEquals(w2, null)) return false;
            return w1.Equals(w2);
        }

        public static bool operator !=(WeightData w1, WeightData w2)
        {
            return !(w1 == w2);
        }
    }
}
