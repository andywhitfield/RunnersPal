using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RunnersPal.Web.Extensions;

namespace RunnersPal.Web.Models
{
    public class RoutePalViewModel
    {
        public static IEnumerable<RoutePalViewModel.RouteModel> RoutesForCurrentUser(ControllerContext context)
        {
            dynamic currentUser = context.HasValidUserAccount() ? context.UserAccount() : null;

            IEnumerable<dynamic> routes = MassiveDB.Current.FindRoutes(currentUser);
            routes = routes.Where(r => !string.IsNullOrWhiteSpace(r.MapPoints));

            IEnumerable<dynamic> runInfos = MassiveDB.Current.FindLatestRunLogForRoutes(routes.Select(r => (long)r.Id));

            var routeModels = routes.Select(route => new RoutePalViewModel.RouteModel(context, route)).ToList();
            foreach (var route in routeModels)
            {
                var runInfo = runInfos.FirstOrDefault(r => r.RouteId == route.Id);
                if (runInfo == null) continue;
                route.LastRunBy = runInfo.DisplayName;
                route.LastRunDate = runInfo.Date;
            }

            return routeModels.OrderByDescending(r => r.LastRunDate ?? r.CreatedDate);
        }

        public class RouteModel
        {
            private readonly ControllerContext context;
            private readonly dynamic route;

            public RouteModel(ControllerContext context, dynamic route)
            {
                this.context = context;
                this.route = route;
            }

            public long Id { get { return route.Id; } }
            public string Name { get { return route.Name; } }
            public string Notes { get { return string.IsNullOrWhiteSpace(route.Notes) ? "" : route.Notes; } }
            public string Distance
            {
                get
                {
                    var dist = new Distance(route.Distance, (DistanceUnits)route.DistanceUnits).ConvertTo(context.UserDistanceUnits());
                    return dist.BaseDistance.ToString("0.##");
                }
            }
            public DateTime CreatedDate { get { return route.CreatedDate; } }
            public string LastRunBy { get; set; }
            public string LastRun { get { return LastRunDate.HasValue ? LastRunDate.Value.ToString("ddd, dd/MMM/yyyy") : ""; } }
            public DateTime? LastRunDate { get; set; }
        }

        public RoutePalViewModel()
        {
            Routes = Enumerable.Empty<RouteModel>();
        }

        public IEnumerable<RouteModel> Routes { get; set; }
    }
}