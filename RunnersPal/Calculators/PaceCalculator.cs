using System;

namespace RunnersPal.Calculators
{
    public class PaceCalculator
    {
        public void Calculate(PaceData calcData)
        {
            switch ((calcData.Calc ?? "").ToLower())
            {
                case "distance":
                    CalculateDistance(calcData);
                    break;
                case "time":
                    CalculateTime(calcData);
                    break;
                case "pace":
                    CalculatePace(calcData);
                    break;
                case "pacekmunits":
                    ConvertPace(calcData, DistanceUnits.Kilometers);
                    break;
                case "pacemilesunits":
                    ConvertPace(calcData, DistanceUnits.Miles);
                    break;
                case "normalizetime":
                    NormalizeTime(calcData);
                    break;
            }
        }

        private void CalculateDistance(PaceData calcData)
        {
            if (!calcData.HasPace || !calcData.HasTime || calcData.Distance == null) return;

            var pace = 0; // pace in seconds / miles
            var pacePortions = calcData.Pace.Split(':');

            var paceMinutes = 0;
            var paceSeconds = 0;

            // mm or mm:ss
            if (pacePortions.Length == 1) // mm
            {
                int.TryParse(pacePortions[0], out paceMinutes);
            }
            else // mm:ss
            {
                int.TryParse(pacePortions[0], out paceMinutes);
                int.TryParse(pacePortions[1], out paceSeconds);
            }
            pace = (paceMinutes * 60) + paceSeconds;

            var time = 0; // seconds
            if (string.IsNullOrWhiteSpace(calcData.Time))
                return;

            var timePortions = calcData.Time.Split(':');

            var hours = 0;
            var minutes = 0;
            var seconds = 0;

            // mm or mm:ss or hh:mm:ss
            if (timePortions.Length == 1) // mm
            {
                int.TryParse(timePortions[0], out minutes);
            }
            else if (timePortions.Length == 2) // mm:ss
            {
                int.TryParse(timePortions[0], out minutes);
                int.TryParse(timePortions[1], out seconds);
            }
            else // hh:mm:ss
            {
                int.TryParse(timePortions[0], out hours);
                int.TryParse(timePortions[1], out minutes);
                int.TryParse(timePortions[2], out seconds);
            }
            time = (hours * 60 * 60) + (minutes * 60) + seconds;

            if (pace == 0 || time == 0) return;

            var distance = time / (double) pace;
            calcData.Distance = new Distance(distance, calcData.Distance.BaseUnits);
        }

        private void CalculateTime(PaceData calcData)
        {
            calcData.Time = "0";

            if (!calcData.HasDistance || !calcData.HasPace) return;

            var pace = 0; // pace in seconds / miles
            var pacePortions = calcData.Pace.Split(':');

            var paceMinutes = 0;
            var paceSeconds = 0;

            // mm or mm:ss
            if (pacePortions.Length == 1) // mm
            {
                int.TryParse(pacePortions[0], out paceMinutes);
            }
            else // mm:ss
            {
                int.TryParse(pacePortions[0], out paceMinutes);
                int.TryParse(pacePortions[1], out paceSeconds);
            }
            pace = (paceMinutes * 60) + paceSeconds;

            if (!calcData.HasDistance)
                return;
            var distance = calcData.Distance.BaseDistance; // distance in user units

            if (pace == 0 || distance == 0) return;

            calcData.Time = TimeSpan.FromSeconds(distance * pace).ToString("hh\\:mm\\:ss");
        }

        private void CalculatePace(PaceData calcData)
        {
            calcData.Pace = "0";

            if (!calcData.HasDistance || !calcData.HasTime) return;

            if (!calcData.HasDistance)
                return;
            var distance = calcData.Distance.BaseDistance; // distance in user's units

            var time = 0; // seconds
            if (string.IsNullOrWhiteSpace(calcData.Time))
                return;

            var timePortions = calcData.Time.Split(':');

            var hours = 0;
            var minutes = 0;
            var seconds = 0;

            // mm or mm:ss or hh:mm:ss
            if (timePortions.Length == 1) // mm
            {
                int.TryParse(timePortions[0], out minutes);
            }
            else if (timePortions.Length == 2) // mm:ss
            {
                int.TryParse(timePortions[0], out minutes);
                int.TryParse(timePortions[1], out seconds);
            }
            else // hh:mm:ss
            {
                int.TryParse(timePortions[0], out hours);
                int.TryParse(timePortions[1], out minutes);
                int.TryParse(timePortions[2], out seconds);
            }
            time = (hours * 60 * 60) + (minutes * 60) + seconds;

            if (time == 0 || distance == 0) return;

            calcData.PaceInSeconds = time / distance;
            calcData.Pace = PaceToString(calcData.PaceInSeconds.Value);
        }

        public string PaceToString(double pace)
        {
            return string.Format("{0}:{1}", Convert.ToInt32(Math.Floor(pace / 60)).ToString("0"), Math.Floor(pace % 60).ToString("00"));
        }

        private void ConvertPace(PaceData calcData, DistanceUnits fromUnits)
        {
            if (!calcData.HasPace) return;
            
            double pace = 0; // pace in seconds / miles
            var pacePortions = calcData.Pace.Split(':');

            var paceMinutes = 0;
            var paceSeconds = 0;

            // mm or mm:ss
            if (pacePortions.Length == 1) // mm
            {
                int.TryParse(pacePortions[0], out paceMinutes);
            }
            else // mm:ss
            {
                int.TryParse(pacePortions[0], out paceMinutes);
                int.TryParse(pacePortions[1], out paceSeconds);
            }
            pace = (paceMinutes * 60) + paceSeconds;

            if (fromUnits == DistanceUnits.Miles)
                pace *= DistanceCalculator.MilesToKilometers;
            else if (fromUnits == DistanceUnits.Kilometers)
                pace *= DistanceCalculator.KilometersToMiles;
            else return;

            calcData.Pace = string.Format("{0}:{1}", Convert.ToInt32(Math.Floor(pace / 60)).ToString("0"), Math.Floor(pace % 60).ToString("00"));
        }

        private void NormalizeTime(PaceData calcData)
        {
            if (!calcData.HasTime) return;

            var time = 0; // seconds
            if (string.IsNullOrWhiteSpace(calcData.Time))
                return;

            var timePortions = calcData.Time.Split(':');

            var hours = 0;
            var minutes = 0;
            var seconds = 0;

            // mm or mm:ss or hh:mm:ss
            if (timePortions.Length == 1) // mm
            {
                int.TryParse(timePortions[0], out minutes);
            }
            else if (timePortions.Length == 2) // mm:ss
            {
                int.TryParse(timePortions[0], out minutes);
                int.TryParse(timePortions[1], out seconds);
            }
            else // hh:mm:ss
            {
                int.TryParse(timePortions[0], out hours);
                int.TryParse(timePortions[1], out minutes);
                int.TryParse(timePortions[2], out seconds);
            }
            time = (hours * 60 * 60) + (minutes * 60) + seconds;
            var timeVal = TimeSpan.FromSeconds(time);
            calcData.Time = timeVal.ToString(timeVal.TotalHours > 0 ? "hh\\:mm\\:ss" : "mm\\:ss");
        }
    }
}