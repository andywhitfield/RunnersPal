using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RunnersPal.Web.Models
{
    public class MyStatsModel
    {
        public enum StatsPeriod { Week, Month, Year }

        public MyStatsModel()
        {
            Period = StatsPeriod.Week;
            DistanceStats = Enumerable.Empty<StatValue>();
            PaceStats = Enumerable.Empty<StatValue>();
        }

        public StatsPeriod Period { get; set; }
        public IEnumerable<StatValue> DistanceStats { get; set; }
        public IEnumerable<StatValue> PaceStats { get; set; }

        public string TooltipPeriod { get { return Period == StatsPeriod.Week ? "w/e " : ""; } }
        public string DistanceStatCategories() { return string.Join(", ", DistanceStats.Select(s => "'" + s.Category + "'")); }
        public string DistanceStatValues() { return string.Join(", ", DistanceStats.Select(s => s.Value.ToString("###0.00"))); }
        public string PaceStatCategories() { return string.Join(", ", PaceStats.Select(s => "'" + s.Category + "'")); }
        public string PaceStatValues() { return string.Join(", ", PaceStats.Select(s => s.Value.ToString("###0.00"))); }
    }

    public class StatValue
    {
        public string Category { get; set; }
        public double Value { get; set; }
    }
}