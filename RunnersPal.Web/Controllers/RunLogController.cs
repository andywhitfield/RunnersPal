using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using RunnersPal.Web.Extensions;
using RunnersPal.Web.Models;
using System;

namespace RunnersPal.Web.Controllers
{
    public class RunLogController : Controller
    {
        public ActionResult Index()
        {
            return View(new RunLogViewModel(ControllerContext, Enumerable.Empty<dynamic>()) { Routes = RoutePalController.RoutesForCurrentUser(ControllerContext) });
        }

        public ActionResult AllEvents()
        {
            var model = new RunLogViewModel(ControllerContext, MassiveDB.Current.FindRunLogEvents(ControllerContext.UserAccount(), false));
            return new JsonResult { Data = model.RunLogEventsToJson(), JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        public ActionResult Add(NewRunData newRunData)
        {
            return AddRunLogEvent(newRunData).Item1;
        }

        [HttpPost]
        public ActionResult View(int runLogId)
        {
            if (!ControllerContext.HasValidUserAccount())
                return new JsonResult { Data = new { Completed = false, Reason = "Please log in / create an account." } };

            Trace.TraceInformation("Loading run log id {0}", runLogId);
            var runLogEvent = MassiveDB.Current.FindRunLogEvent(runLogId);
            if (runLogEvent == null)
                return new JsonResult { Data = new { Completed = false, Reason = "Cannot find event - please refresh and try again." } };
            if (runLogEvent.UserAccountId != ControllerContext.UserAccount().Id)
                return new JsonResult { Data = new { Completed = false, Reason = "You are not allowed to view this event - please refresh and try again." } };
            if (runLogEvent.LogState == "D")
                return new JsonResult { Data = new { Completed = false, Reason = "This event has been deleted." } };

            var model = new RunLogViewModel(ControllerContext, runLogEvent);
            return new JsonResult { Data = model.RunLogModels.Single().RunLogEventToJson() };
        }

        [HttpPost]
        public ActionResult Delete(long runLogId)
        {
            if (!ControllerContext.HasValidUserAccount())
                return new JsonResult { Data = new { Completed = false, Reason = "Please log in / create an account." } };

            Trace.TraceInformation("Deleting run log id {0}", runLogId);
            var runLogEvent = MassiveDB.Current.FindRunLogEvent(runLogId);
            if (runLogEvent == null)
                return new JsonResult { Data = new { Completed = false, Reason = "Cannot find event - please refresh and try again." } };
            if (runLogEvent.UserAccountId != ControllerContext.UserAccount().Id)
                return new JsonResult { Data = new { Completed = false, Reason = "You are not allowed to delete this event - please refresh and try again." } };

            runLogEvent.LogState = 'D';
            MassiveDB.Current.UpdateRunLogEvent(runLogEvent);

            var model = new RunLogViewModel(ControllerContext, runLogEvent);
            return new JsonResult { Data = model.RunLogModels.Single().RunLogEventToJson() };
        }

        [HttpPost]
        public ActionResult Edit(NewRunData newRunData)
        {
            if (!ModelState.IsValid || (!newRunData.Distance.HasValue && !newRunData.Route.HasValue) || !newRunData.RunLogId.HasValue)
                return new JsonResult { Data = new { Completed = false, Reason = "Please provide a valid route/distance and time." } };
            if (!ControllerContext.HasValidUserAccount())
                return new JsonResult { Data = new { Completed = false, Reason = "Please create an account." } };

            Trace.TraceInformation("Editing run event {0} for date {1}, route {2}, distance {3}, time {4}", newRunData.RunLogId, newRunData.Date, newRunData.Route, newRunData.Distance, newRunData.Time);

            Delete(newRunData.RunLogId.Value);
            var addedItem = AddRunLogEvent(newRunData);
            if (addedItem.Item2 == null)
                return addedItem.Item1;

            dynamic newRunLogEvent = addedItem.Item2;
            newRunLogEvent.ReplacesRunLogId = newRunData.RunLogId;
            MassiveDB.Current.UpdateRunLogEvent(newRunLogEvent);
            return addedItem.Item1;
        }

        private Tuple<JsonResult, object> AddRunLogEvent(NewRunData newRunData)
        {
            if (!ModelState.IsValid || (!newRunData.Distance.HasValue && !newRunData.Route.HasValue))
                return Tuple.Create(new JsonResult { Data = new { Completed = false, Reason = "Please provide a valid route/distance and time." } }, (object)null);
            if (!ControllerContext.HasValidUserAccount())
                return Tuple.Create(new JsonResult { Data = new { Completed = false, Reason = "Please create an account." } }, (object)null);

            Trace.TraceInformation("Creating run event for date {0}, route {1}, distance {2}, time {3}", newRunData.Date, newRunData.Route, newRunData.Distance, newRunData.Time);

            var userUnits = ControllerContext.UserDistanceUnits();
            dynamic route = null;
            Distance distance = new Distance(newRunData.Distance ?? 0, userUnits);
            if (newRunData.Route.HasValue)
            {
                route = MassiveDB.Current.FindRoute(newRunData.Route.Value);
                if (route != null)
                    distance = new Distance(route.Distance, (DistanceUnits)route.DistanceUnits);
            }

            var runLogEvent = MassiveDB.Current.CreateRunLogEvent(ControllerContext.UserAccount(), newRunData.Date.Value, distance, route, newRunData.NormalizedTime, newRunData.Comment);
            var model = new RunLogViewModel(ControllerContext, runLogEvent);

            return Tuple.Create(new JsonResult { Data = model.RunLogModels.Single().RunLogEventToJson() }, (object)runLogEvent);
        }
    }
}
