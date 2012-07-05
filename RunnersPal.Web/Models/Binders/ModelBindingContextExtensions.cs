using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RunnersPal.Web.Models.Binders
{
    public static class ModelBindingContextExtensions
    {
        public static string GetString(this ModelBindingContext bindingContext, string field)
        {
            var value = bindingContext.ValueProvider.GetValue(field);
            return value != null ? value.AttemptedValue : "";
        }

        public static int? GetInt(this ModelBindingContext bindingContext, string field)
        {
            int? val = null;
            int parsedVal;
            if (int.TryParse(bindingContext.GetString(field), out parsedVal))
                val = parsedVal;
            return val;
        }

        public static double? GetDouble(this ModelBindingContext bindingContext, string field)
        {
            double? val = null;
            double parsedVal;
            if (double.TryParse(bindingContext.GetString(field), out parsedVal))
                val = parsedVal;
            return val;
        }

        public static long? GetLong(this ModelBindingContext bindingContext, string field)
        {
            long? val = null;
            long parsedVal;
            if (long.TryParse(bindingContext.GetString(field), out parsedVal))
                val = parsedVal;
            return val;
        }

        public static bool? GetBool(this ModelBindingContext bindingContext, string field)
        {
            bool? val = null;
            bool parsedVal;
            if (bool.TryParse(bindingContext.GetString(field), out parsedVal))
                val = parsedVal;
            return val;
        }
    }
}