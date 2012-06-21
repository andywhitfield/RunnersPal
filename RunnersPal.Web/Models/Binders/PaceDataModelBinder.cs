using System;
using System.Diagnostics;
using System.Web.Mvc;
using RunnersPal.Calculators;
using RunnersPal.Web.Extensions;

namespace RunnersPal.Web.Models.Binders
{
    public class PaceDataModelBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            long? route = null;
            long parsedRoute;
            if (long.TryParse(Get(bindingContext, "route"), out parsedRoute))
                route = parsedRoute;

            Distance distance;
            double parsedDistance;
            if (double.TryParse(Get(bindingContext, "distance"), out parsedDistance))
                distance = new Distance(parsedDistance, controllerContext.UserDistanceUnits());
            else
                distance = new Distance(0, controllerContext.UserDistanceUnits());

            OnModelUpdating(controllerContext, bindingContext);
            bindingContext.ModelMetadata.Model = new PaceData { Calc = Get(bindingContext, "calc"), Pace = Get(bindingContext, "pace"), Distance = distance, Time = Get(bindingContext, "time"), Route = route };
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