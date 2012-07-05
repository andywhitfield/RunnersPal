using System.Web.Mvc;
using RunnersPal.Web.Models;
using RunnersPal.Web.Extensions;
using System.Diagnostics;
using System;
using System.Linq;
using System.Collections.Generic;

namespace RunnersPal.Web.Controllers
{
    public class RoutePalController : Controller
    {
        public ActionResult Index()
        {
            if (Request.Params["save"] == "true")
            {
                try
                {
                    var routeData = Session["rp_RouteInfoPreLogin"] as RouteData;
                    if (routeData != null)
                    {
                        var saveInfo = Save(routeData) as JsonResult;
                        if (saveInfo != null)
                        {
                            dynamic json = saveInfo.Data;
                            if (json.Completed)
                            {
                                return Redirect(Url.Action("index", "routepal", new { route = json.Route.Id }));
                            }
                        }
                    }
                }
                finally
                {
                    Session.Remove("rp_RouteInfoPreLogin");
                }
            }

            return View();
        }

        [HttpPost]
        public ActionResult MyRoutes()
        {
            return new JsonResult
            {
                Data = new { Completed = true, Routes = RoutePalViewModel.RoutesForCurrentUser(ControllerContext) }
            };
        }

        [HttpPost]
        public ActionResult Load(long? id)
        {
            if (!id.HasValue || id < 1)
                return new JsonResult { Data = new { Completed = false, Reason = "No route provided." } };

            var route = MassiveDB.Current.FindRoute(id.Value);
            if (route == null)
                return new JsonResult { Data = new { Completed = false, Reason = "Cannot find specified route." } };
            if (route.RouteType != Route.PublicRoute.ToString() && route.RouteType != Route.PrivateRoute.ToString())
                return new JsonResult { Data = new { Completed = false, Reason = "Cannot find your route." } };

            var currentUser = ControllerContext.UserAccount();
            var isRouteOwnedByAnotherUser = currentUser == null || currentUser.Id != route.Creator;
            if (route.RouteType == Route.PrivateRoute.ToString() && isRouteOwnedByAnotherUser)
                return new JsonResult { Data = new { Completed = false, Reason = "The route you are trying to load was either not created by you or is not public. Please check you are logged in and try again." } };

            return new JsonResult { Data = new { Completed = true, Route = new { Id = route.Id, Name = route.Name, Notes = route.Notes ?? "", Public = route.RouteType == Route.PublicRoute.ToString(), Points = string.IsNullOrWhiteSpace(route.MapPoints) ? "[]" : route.MapPoints, Distance = route.Distance, PublicOther = route.RouteType == Route.PublicRoute.ToString() && isRouteOwnedByAnotherUser } } };
        }

        [HttpPost]
        public ActionResult Save(RouteData routeData)
        {
            if (!ModelState.IsValid)
                return new JsonResult { Data = new { Completed = false, Reason = "Please provide a route name." } };
            if (!ControllerContext.HasValidUserAccount())
                return new JsonResult { Data = new { Completed = false, Reason = "Please create an account." } };

            var userUnits = ControllerContext.UserDistanceUnits();
            Distance distance = new Distance(routeData.Distance, userUnits);

            Trace.TraceInformation("Saving route {0} name {1}, notes {2}, is public? {3}, points: {4}", routeData.Id, routeData.Name, routeData.Notes, routeData.Public, routeData.Points);

            string lastRun;
            string lastRunBy;
            if (routeData.Id == 0)
            {
                var newRoute = MassiveDB.Current.CreateRoute(ControllerContext.UserAccount(), routeData.Name, routeData.Notes ?? "", distance, (routeData.Public ?? false) ? Route.PublicRoute : Route.PrivateRoute, routeData.Points);
                routeData.Id = Convert.ToInt64(newRoute.Id);
                lastRun = "";
                lastRunBy = "";
            }
            else
            {
                var currentRoute = MassiveDB.Current.FindRoute(routeData.Id);
                var currentUser = ControllerContext.UserAccount();
                var isRouteOwnedByAnotherUser = currentUser.Id != currentRoute.Creator;

                if (isRouteOwnedByAnotherUser && currentRoute.RouteType != Route.PublicRoute.ToString())
                    return new JsonResult { Data = new { Completed = false, Reason = "Cannot save the route - you can only save routes you have created." } };

                if (isRouteOwnedByAnotherUser || currentRoute.MapPoints != routeData.Points)
                {
                    if (!isRouteOwnedByAnotherUser)
                    {
                        // delete old
                        currentRoute.RouteType = Route.DeletedRoute;
                        MassiveDB.Current.UpdateRoute(currentRoute);
                    }

                    // add new
                    currentRoute = MassiveDB.Current.CreateRoute(currentUser, routeData.Name, routeData.Notes ?? "", distance, (routeData.Public ?? false) ? Route.PublicRoute : Route.PrivateRoute, routeData.Points, currentRoute.Id);
                    routeData.Id = Convert.ToInt64(currentRoute.Id);

                    lastRun = "";
                    lastRunBy = "";
                }
                else
                {
                    currentRoute.Name = routeData.Name;
                    currentRoute.Notes = routeData.Notes ?? "";
                    currentRoute.RouteType = (routeData.Public ?? false) ? Route.PublicRoute : Route.PrivateRoute;

                    MassiveDB.Current.UpdateRoute(currentRoute);

                    // get last run info
                    var runInfo = MassiveDB.Current.FindLatestRunLogForRoutes(new[] { routeData.Id }).FirstOrDefault();
                    if (runInfo == null)
                    {
                        lastRunBy = "";
                        lastRun = "";
                    }
                    else
                    {
                        lastRunBy = runInfo.DisplayName;
                        lastRun = runInfo.Date == null ? "" : runInfo.Date.ToString("ddd, dd/MMM/yyyy");
                    }
                }
            }

            return new JsonResult { Data = new { Completed = true, Route = new { Id = routeData.Id, Name = routeData.Name, Notes = routeData.Notes ?? "", Public = routeData.Public ?? false, Points = routeData.Points, Distance = distance.BaseDistance, LastRun = lastRun, PublicOther = false } } };
        }

        [HttpPost]
        public ActionResult Delete(long? id)
        {
            if (!id.HasValue || id < 1)
                return new JsonResult { Data = new { Completed = false, Reason = "No route provided." } };

            var currentUser = ControllerContext.UserAccount();
            if (currentUser == null)
                return new JsonResult { Data = new { Completed = false, Reason = "You must be logged in to delete the route. Please confirm you are logged in and try again." } };

            var route = MassiveDB.Current.FindRoute(id.Value);
            if (route == null)
                return new JsonResult { Data = new { Completed = false, Reason = "Cannot find specified route." } };
            if (route.RouteType != Route.PublicRoute.ToString() && route.RouteType != Route.PrivateRoute.ToString())
                return new JsonResult { Data = new { Completed = false, Reason = "Cannot find your route." } };

            if (currentUser.Id != route.Creator || currentUser.UserType == "A")
                return new JsonResult { Data = new { Completed = false, Reason = "The route you are trying to load was not created by you. Please check you are logged in as the correct account and try again." } };

            route.RouteType = Route.DeletedRoute;
            MassiveDB.Current.UpdateRoute(route);
            return new JsonResult { Data = new { Completed = true } };
        }

        [HttpPost]
        public ActionResult Find(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return new JsonResult { Data = new { Completed = true, Routes = new object[0] } };

            dynamic currentUser = ControllerContext.HasValidUserAccount() ? ControllerContext.UserAccount() : null;

            IEnumerable<dynamic> routes = MassiveDB.Current.SearchForRoutes(currentUser, q);
            IEnumerable<dynamic> runInfos = MassiveDB.Current.FindLatestRunLogForRoutes(routes.Select(r => (long)r.Id));

            var routeModels = routes.Select(route => new RoutePalViewModel.RouteModel(ControllerContext, route)).ToList();
            foreach (var route in routeModels)
            {
                var runInfo = runInfos.FirstOrDefault(r => r.RouteId == route.Id);
                if (runInfo == null) continue;
                route.LastRunBy = runInfo.DisplayName;
                route.LastRunDate = runInfo.Date;
            }

            return new JsonResult
            {
                Data = new { Completed = true, Routes = routeModels.OrderByDescending(r => r.LastRunDate ?? r.CreatedDate) }
            };
        }

        [HttpPost]
        public ActionResult BeforeLogin(RouteData routeData)
        {
            if (!ModelState.IsValid)
                return new JsonResult { Data = new { Completed = false, Reason = "Please provide a route name." } };

            Trace.TraceInformation("Saving route before login process - id {0}, name {1}, notes {2}, is public? {3}, points: {4}", routeData.Id, routeData.Name, routeData.Notes, routeData.Public, routeData.Points);
            Session["rp_RouteInfoPreLogin"] = routeData;

            return new JsonResult { Data = new { Completed = true } };
        }
    }
}
