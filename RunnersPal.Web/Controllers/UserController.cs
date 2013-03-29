using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using RunnersPal.Calculators;
using RunnersPal.Web.Extensions;
using RunnersPal.Web.Models;

namespace RunnersPal.Web.Controllers
{
    public class UserController : Controller
    {
        public ActionResult FirstTime() { return View(); }
        public ActionResult Index() { return ByWeek(); }
        public ActionResult Profile(bool? saved)
        {
            var model = new ProfileModel(ControllerContext);
            if (saved.HasValue && saved.Value) model.Message = "Saved successfully.";
            return View(model);
        }
        public ActionResult UpdateProfile(ProfileModel model)
        {
            var userAccount = ControllerContext.UserAccount();

            if (!ModelState.IsValid || userAccount == null)
            {
                if (model.Weight == null) model.Weight = ProfileModel.DefaultWeightData();
                model.Message = "Please enter your name";
                return View("Profile", model);
            }

            userAccount.DisplayName = model.Name;
            if (Enum.IsDefined(typeof(DistanceUnits), model.DistUnits))
            {
                var distUnits = (DistanceUnits)model.DistUnits;
                userAccount.DistanceUnits = distUnits;
                HttpContext.Session["rp_UserDistanceUnits"] = distUnits;
            }

            MassiveDB.Current.UpdateUser(userAccount);

            var newModel = new ProfileModel(ControllerContext);
            if (newModel.Weight != model.Weight)
                MassiveDB.Current.CreatePref(userAccount, model.Weight.UnitWeight, model.Weight.Units);

            return RedirectToAction("profile", new { saved = true });
        }

        public ActionResult Download()
        {
            if (!ControllerContext.HasValidUserAccount())
                return ByWeek();
            var userAccount = ControllerContext.UserAccount();

            var csv = string.Format("Date,Distance ({0}),Time,Pace (min/{1}),Route Name,Comment{2}", ControllerContext.UserDistanceUnits("a"), ControllerContext.UserDistanceUnits("a.s"), Environment.NewLine);
            
            IEnumerable<dynamic> runEvents = MassiveDB.Current.FindRunLogEvents(userAccount, false);

            csv += string.Join(Environment.NewLine, runEvents
                .OrderBy(e => e.Date)
                .Select(e => new { Date = (DateTime)e.Date, TimeTaken = (string)e.TimeTaken, Comment = CsvSafeString(e.Comment), DistanceAndPace = (Tuple<Distance, PaceData, string>)DistanceAndPaceOfLogEvent(e) })
                .Select(d => string.Format("{0},{1},{2},{3},\"{4}\",\"{5}\"", d.Date.ToString("yyyy-MM-dd"), d.DistanceAndPace.Item1.BaseDistance, d.TimeTaken, d.DistanceAndPace.Item2.Pace, d.DistanceAndPace.Item3, d.Comment)));
            
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "runlogevents.csv");
        }

        private string CsvSafeString(string s)
        {
            return s.Replace("\n", " ").Replace("\"", "\"\"");
        }

        public ActionResult ByWeek()
        {
            return ShowStats(new MyStatsModel { Period = MyStatsModel.StatsPeriod.Week }, (u, m) =>
            {
                Trace.TraceInformation("Getting run log events by week");
                IEnumerable<dynamic> runEvents = MassiveDB.Current.FindRunLogEvents(u, false);
                runEvents = runEvents.ToList();
                Trace.TraceInformation("Loaded {0} run log events", runEvents.Count());
                if (!runEvents.Any()) return;

                var datesAndDistances = runEvents.Select(e => new { Date = (DateTime)e.Date, DistanceAndPace = (Tuple<Distance, PaceData, string>)DistanceAndPaceOfLogEvent(e) });
                var groupedByWeek = datesAndDistances
                    .GroupBy(d => WeekEnding(d.Date))
                    .OrderBy(g => g.Key);

                var aggregated = Enumerable
                    .Repeat(groupedByWeek.Last().Key, 40)
                    .Select((dt, idx) => dt.AddDays(idx * -7))
                    .Select(dt =>
                    {
                        var weekGrouping = groupedByWeek.SingleOrDefault(g => g.Key == dt);
                        return new { WeekEnding = dt, Distance = weekGrouping == null ? 0 : weekGrouping.Sum(a => a.DistanceAndPace.Item1.BaseDistance), Pace = weekGrouping == null ? 0 : weekGrouping.Average(a => a.DistanceAndPace.Item2.PaceInSeconds.Value / 60) };
                    })
                    .TakeWhile(agg => agg.WeekEnding >= groupedByWeek.First().Key)
                    .Reverse()
                    .ToList();

                m.DistanceStats = aggregated.Select(g => new StatValue { Category = g.WeekEnding.ToString("dd MMM"), Value = g.Distance });
                m.PaceStats = aggregated.Select(g => new StatValue { Category = g.WeekEnding.ToString("dd MMM"), Value = g.Pace });
            });
        }
        public ActionResult ByMonth()
        {
            return ShowStats(new MyStatsModel { Period = MyStatsModel.StatsPeriod.Month }, (u, m) =>
            {
                Trace.TraceInformation("Getting run log events by month");
                IEnumerable<dynamic> runEvents = MassiveDB.Current.FindRunLogEvents(u, false);
                runEvents = runEvents.ToList();
                Trace.TraceInformation("Loaded {0} run log events", runEvents.Count());
                if (!runEvents.Any()) return;

                var datesAndDistances = runEvents.Select(e => new { Date = (DateTime)e.Date, DistanceAndPace = (Tuple<Distance, PaceData, string>)DistanceAndPaceOfLogEvent(e) });
                var groupedByMonth = datesAndDistances
                    .GroupBy(d => new DateTime(d.Date.Year, d.Date.Month, 1))
                    .OrderBy(g => g.Key);

                var aggregated = Enumerable
                    .Repeat(groupedByMonth.Last().Key, 40)
                    .Select((dt, idx) => dt.AddMonths(-idx))
                    .Select(dt =>
                    {
                        var monthGrouping = groupedByMonth.SingleOrDefault(g => g.Key == dt);
                        return new { Month = dt, Distance = monthGrouping == null ? 0 : monthGrouping.Sum(a => a.DistanceAndPace.Item1.BaseDistance), Pace = monthGrouping == null ? 0 : monthGrouping.Average(a => a.DistanceAndPace.Item2.PaceInSeconds.Value / 60) };
                    })
                    .TakeWhile(agg => agg.Month >= groupedByMonth.First().Key)
                    .Reverse()
                    .ToList();

                m.DistanceStats = aggregated.Select(g => new StatValue { Category = g.Month.ToString("MMM"), Value = g.Distance });
                m.PaceStats = aggregated.Select(g => new StatValue { Category = g.Month.ToString("MMM"), Value = g.Pace });
            });
        }
        public ActionResult ByYear()
        {
            return ShowStats(new MyStatsModel { Period = MyStatsModel.StatsPeriod.Year }, (u, m) =>
            {
                Trace.TraceInformation("Getting run log events by year");
                IEnumerable<dynamic> runEvents = MassiveDB.Current.FindRunLogEvents(u, false);
                runEvents = runEvents.ToList();
                Trace.TraceInformation("Loaded {0} run log events", runEvents.Count());
                if (!runEvents.Any()) return;

                var datesAndDistances = runEvents.Select(e => new { Date = (DateTime)e.Date, DistanceAndPace = (Tuple<Distance, PaceData, string>)DistanceAndPaceOfLogEvent(e) });
                var groupedByYear = datesAndDistances
                    .GroupBy(d => new DateTime(d.Date.Year, 1, 1))
                    .OrderBy(g => g.Key);

                var aggregated = Enumerable
                    .Repeat(groupedByYear.Last().Key, 40)
                    .Select((dt, idx) => dt.AddYears(-idx))
                    .Select(dt =>
                    {
                        var yearGrouping = groupedByYear.SingleOrDefault(g => g.Key == dt);
                        return new { Year = dt, Distance = yearGrouping == null ? 0 : yearGrouping.Sum(a => a.DistanceAndPace.Item1.BaseDistance), Pace = yearGrouping == null ? 0 : yearGrouping.Average(a => a.DistanceAndPace.Item2.PaceInSeconds.Value / 60) };
                    })
                    .TakeWhile(agg => agg.Year >= groupedByYear.First().Key)
                    .Reverse();

                m.DistanceStats = aggregated.Select(g => new StatValue { Category = g.Year.ToString("yyyy"), Value = g.Distance });
                m.PaceStats = aggregated.Select(g => new StatValue { Category = g.Year.ToString("yyyy"), Value = g.Pace });
            });
        }

        private Tuple<Distance,PaceData,string> DistanceAndPaceOfLogEvent(dynamic runLogEvent)
        {
            var route = ((object)runLogEvent).Route();
            var distance = ((object)route).Distance().ConvertTo(ControllerContext.UserDistanceUnits());
            var paceData = new PaceData { Distance = distance, Time = runLogEvent.TimeTaken, Calc = "Pace" };
            var paceCalc = new PaceCalculator();
            paceCalc.Calculate(paceData);
            return Tuple.Create(distance, paceData, route.Name.Replace('"', '\''));
        }
        private DateTime WeekEnding(DateTime dt)
        {
            var date = dt.Date;
            while (date.DayOfWeek != DayOfWeek.Sunday) date = date.AddDays(1);
            return date;
        }

        private ActionResult ShowStats(MyStatsModel model, Action<dynamic, MyStatsModel> loadEventsCallback)
        {
            if (!ControllerContext.HasValidUserAccount()) return View("NotLoggedIn");
            loadEventsCallback(ControllerContext.UserAccount(), model);

            Trace.TraceInformation("Completed stats, returning view");

            if (!model.DistanceStats.Any() || !model.PaceStats.Any()) return View("NoLoggedEvents");
            return View("Index", model);
        }

        [HttpPost]
        public ActionResult Logout()
        {
            var userCookie = new HttpCookie("rp_UserAccount") { Expires = DateTime.UtcNow.AddYears(-1) };
            Response.Cookies.Set(userCookie);
            Session.Remove("rp_UserAccount");

            return new JsonResult { Data = new { LoggedIn = false } };
        }

        [HttpPost]
        public ActionResult AddName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ViewData["ErrorMessage"] = "Your name is required - please type your name and try again.";
                return View("FirstTime");
            }

            var userAccount = ControllerContext.UserAccount();
            if (userAccount != null)
            {
                userAccount.DisplayName = name;
                userAccount.UserType = "U";
                MassiveDB.Current.UpdateUser(userAccount);
            }

            var returnPage = Session["login_returnPage"] as string;
            if (string.IsNullOrWhiteSpace(returnPage))
                return RedirectToAction("Index", "Home");
            return Redirect(returnPage);
        }
    }
}
