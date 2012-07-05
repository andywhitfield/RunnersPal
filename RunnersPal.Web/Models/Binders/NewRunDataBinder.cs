using System;
using System.Diagnostics;
using System.Globalization;
using System.Web.Mvc;
using RunnersPal.Web.Extensions;

namespace RunnersPal.Web.Models.Binders
{
    public class NewRunDataBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            DateTime? date = null;
            DateTime parsedDate;
            Trace.TraceInformation("Parsing date: {0}", bindingContext.GetString("date"));
            if (DateTime.TryParseExact(bindingContext.GetString("date"), "ddd, d MMM yyyy HH':'mm':'ss 'UTC'", null, DateTimeStyles.AssumeUniversal, out parsedDate))
                date = parsedDate;
            if (DateTime.TryParseExact(bindingContext.GetString("date"), "ddd, d MMM yyyy HH':'mm':'ss 'GMT'", null, DateTimeStyles.AssumeUniversal, out parsedDate))
                date = parsedDate;

            OnModelUpdating(controllerContext, bindingContext);
            bindingContext.ModelMetadata.Model = new NewRunData
            {
                RunLogId = bindingContext.GetLong("runLogId"),
                Date = date,
                Distance = bindingContext.GetDouble("distance"),
                Route = bindingContext.GetLong("route"),
                Time = bindingContext.GetString("time"),
                Comment = bindingContext.GetString("comment").MaxLength(1000),
                NewRoute = new RouteData
                {
                    Distance = bindingContext.GetDouble("distance") ?? 0.0,
                    Name = bindingContext.GetString("newRouteName"),
                    Notes = bindingContext.GetString("newRouteNotes"),
                    Points = bindingContext.GetString("newRoutePoints"),
                    Public = bindingContext.GetBool("newRoutePublic")
                }
            };
            OnModelUpdated(controllerContext, bindingContext);
            return bindingContext.Model;
        }
    }
}