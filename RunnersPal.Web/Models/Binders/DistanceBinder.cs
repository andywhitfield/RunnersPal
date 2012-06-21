using System.Web.Mvc;
using RunnersPal.Web.Extensions;

namespace RunnersPal.Web.Models.Binders
{
    public class DistanceBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            Distance distance;
            double parsedDistance;
            if (double.TryParse(Get(bindingContext, "distance"), out parsedDistance))
                distance = new Distance(parsedDistance, controllerContext.UserDistanceUnits());
            else
                distance = new Distance(0, controllerContext.UserDistanceUnits());

            OnModelUpdating(controllerContext, bindingContext);
            bindingContext.ModelMetadata.Model = distance;
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