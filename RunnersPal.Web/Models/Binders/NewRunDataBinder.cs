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
            Trace.TraceInformation("Parsing date: {0}", Get(bindingContext, "date"));
            if (DateTime.TryParseExact(Get(bindingContext, "date"), "ddd, d MMM yyyy HH':'mm':'ss 'UTC'", null, DateTimeStyles.AssumeUniversal, out parsedDate))
                date = parsedDate;
            if (DateTime.TryParseExact(Get(bindingContext, "date"), "ddd, d MMM yyyy HH':'mm':'ss 'GMT'", null, DateTimeStyles.AssumeUniversal, out parsedDate))
                date = parsedDate;

            long? route = null;
            long parsedRoute;
            if (long.TryParse(Get(bindingContext, "route"), out parsedRoute))
                route = parsedRoute;

            double? distance = null;
            double parsedDistance;
            if (double.TryParse(Get(bindingContext, "distance"), out parsedDistance))
                distance = parsedDistance;

            long? runLogId = null;
            long parsedRunLogId;
            if (long.TryParse(Get(bindingContext, "runLogId"), out parsedRunLogId))
                runLogId = parsedRunLogId;

            OnModelUpdating(controllerContext, bindingContext);
            bindingContext.ModelMetadata.Model = new NewRunData
            {
                RunLogId = runLogId,
                Date = date,
                Distance = distance,
                Route = route,
                Time = Get(bindingContext, "time"),
                Comment = Get(bindingContext, "comment").MaxLength(1000)
            };
            OnModelUpdated(controllerContext, bindingContext);
            return bindingContext.Model;
        }

        public string Get(ModelBindingContext bindingContext, string field)
        {
            var value = bindingContext.ValueProvider.GetValue(field);
            return value != null ? value.AttemptedValue : "";
        }
    }
}