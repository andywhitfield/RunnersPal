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