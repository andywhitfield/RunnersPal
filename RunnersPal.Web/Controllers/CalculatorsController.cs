using System;
using System.Diagnostics;
using System.Globalization;
using System.Web.Mvc;
using RunnersPal.Calculators;
using RunnersPal.Web.Extensions;
using RunnersPal.Web.Models;

namespace RunnersPal.Web.Controllers
{
    public class CalculatorsController : Controller
    {
        private PaceCalculator paceCalc = new PaceCalculator();
        private DistanceCalculator distanceCalc = new DistanceCalculator();
        private WeightCalculator weightCalc = new WeightCalculator();
        private CaloriesCalculator caloriesCalc = new CaloriesCalculator();

        public ActionResult Index() { return View(); }
        public ActionResult Pace() { return View(); }
        public ActionResult Distance() { return View(); }
        public ActionResult Calories() { return View(new CaloriesCalculatorModel(ControllerContext)); }

        [HttpPost]
        public ActionResult CalcPace(PaceData paceCalculation)
        {
            if (!ModelState.IsValid)
                return new JsonResult { Data = paceCalculation };

            Trace.TraceInformation("Calculating pace (route/dist/time/pace/calc): {0}/{1}/{2}/{3}/{4}", paceCalculation.Route, paceCalculation.Distance, paceCalculation.Time, paceCalculation.Pace, paceCalculation.Calc);
            if (paceCalculation.HasRoute)
            {
                var userUnits = paceCalculation.Distance.BaseUnits;
                var route = MassiveDB.Current.FindRoute(paceCalculation.Route.Value);
                if (route != null)
                    paceCalculation.Distance = new Distance(route.Distance, (DistanceUnits)route.DistanceUnits).ConvertTo(userUnits);
            }

            paceCalc.Calculate(paceCalculation);
            Trace.TraceInformation("Calculated pace (route/dist/time/pace/calc): {0}/{1}/{2}/{3}/{4}", paceCalculation.Route, paceCalculation.Distance, paceCalculation.Time, paceCalculation.Pace, paceCalculation.Calc);

            return new JsonResult { Data = paceCalculation };
        }

        [HttpPost]
        public ActionResult CalcDistance(DistanceData distanceCalculation)
        {
            if (!ModelState.IsValid)
                return new JsonResult { Data = distanceCalculation };

            distanceCalc.Calculate(distanceCalculation);

            return new JsonResult { Data = distanceCalculation };
        }

        [HttpPost]
        public ActionResult CalcWeight(WeightData weightCalculation)
        {
            if (!ModelState.IsValid)
                return new JsonResult { Data = new { Result = false, Calc = weightCalculation } };

            weightCalc.Calculate(weightCalculation);

            return new JsonResult { Data = new { Result = true, Calc = weightCalculation } };
        }

        [HttpPost]
        public ActionResult CalcCalories(WeightData weightData, Distance distanceData)
        {
            if (!ModelState.IsValid)
                return new JsonResult { Data = new { Result = false } };

            return new JsonResult { Data = new { Result = true, Calories = caloriesCalc.Calculate(distanceData, weightData) } };
        }

        [HttpPost]
        public ActionResult AutoCalcCalories(string date, int? route, double? distance)
        {
            WeightData weight = null;

            if (ControllerContext.HasValidUserAccount() && date != null)
            {
                var userAccount = ControllerContext.UserAccount();

                DateTime onDate;
                if (!DateTime.TryParseExact(date, "ddd, d MMM yyyy HH':'mm':'ss 'UTC'", null, DateTimeStyles.AssumeUniversal, out onDate))
                    onDate = DateTime.UtcNow;

                weight = ProfileModel.DefaultWeightData();
                var userPref = ((object)userAccount).UserPrefs().Latest(onDate);
                if (userPref != null)
                {
                    weight.Units = userPref.WeightUnits;
                    switch (weight.Units)
                    {
                        case "kg":
                            weight.Kg = userPref.Weight;
                            break;
                        case "lbs":
                        case "st":
                            weight.Lbs = userPref.Weight;
                            weight.Units = "lbs";
                            break;
                    }
                    weight.UpdateFromUnits();
                    if (userPref.WeightUnits == "st")
                        weight.Units = "st";
                }
            }

            if (weight == null || weight.UnitWeight == 0 || (!route.HasValue && !distance.HasValue))
                return new JsonResult { Data = new { Result = false } };

            Distance actualDistance = null;

            if (route.HasValue && route.Value > 0)
            {
                var dbRoute = MassiveDB.Current.FindRoute(route.Value);
                if (dbRoute != null)
                    actualDistance = new Distance(dbRoute.Distance, (DistanceUnits)dbRoute.DistanceUnits).ConvertTo(ControllerContext.UserDistanceUnits());
            }

            if (distance.HasValue && distance.Value > 0)
                actualDistance = new Distance(distance.Value, ControllerContext.UserDistanceUnits());

            if (actualDistance == null)
                return new JsonResult { Data = new { Result = false } };

            return new JsonResult { Data = new { Result = true, Calories = caloriesCalc.Calculate(actualDistance, weight) } };
        }
    }
}
