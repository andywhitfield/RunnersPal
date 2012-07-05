using System;
using System.ComponentModel.DataAnnotations;
using RunnersPal.Calculators;

namespace RunnersPal.Web.Models
{
    public class NewRunData
    {
        public long? RunLogId { get; set; }

        [Required]
        public DateTime? Date { get; set; }

        public long? Route { get; set; }
        public double? Distance { get; set; }

        [Required]
        public string Time { get; set; }

        public string NormalizedTime
        {
            get
            {
                var calcData = new PaceData { Time = Time, Calc = "normalizetime" };
                new PaceCalculator().Calculate(calcData);
                return calcData.Time;
            }
        }

        public string Comment { get; set; }

        public RouteData NewRoute { get; set; }
    }
}