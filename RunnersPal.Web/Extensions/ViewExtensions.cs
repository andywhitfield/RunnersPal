using System.Web.Mvc;
using Massive;
using System;
using System.Linq;

namespace RunnersPal.Web.Extensions
{
    public static class ViewExtensions
    {
        public static string UnitsToString(this DistanceUnits distanceUnit, string format)
        {
            switch (format.ToLower())
            {
                case "a": // plural...i.e. x miles
                case "abbrv":
                    switch (distanceUnit)
                    {
                        case DistanceUnits.Miles:
                            return "miles";
                        case DistanceUnits.Kilometers:
                            return "km";
                    }
                    break;
                case "a.s": // singular...i.e. min/mile
                case "abbrv.s":
                    switch (distanceUnit)
                    {
                        case DistanceUnits.Miles:
                            return "mile";
                        case DistanceUnits.Kilometers:
                            return "km";
                    }
                    break;
            }
            return distanceUnit.ToString();
        }

        public static string UserDistanceUnits(this ControllerContext context, string format)
        {
            return UnitsToString(context.UserDistanceUnits(), format);
        }

        public static DistanceUnits UserDistanceUnits(this ControllerContext context)
        {
            var distanceUnits = DistanceUnits.Miles;
            if (context.HasValidUserAccount())
            {
                object userUnits = context.UserAccount().DistanceUnits;
                if (Enum.IsDefined(typeof(DistanceUnits), userUnits))
                    distanceUnits = (DistanceUnits)userUnits;
            }
            else
            {
                // save/retrieve from the session
                var sessionDistanceUnits = context.HttpContext.Session["rp_UserDistanceUnits"];
                if (sessionDistanceUnits == null)
                    context.HttpContext.Session["rp_UserDistanceUnits"] = distanceUnits;
                else
                    distanceUnits = (DistanceUnits)sessionDistanceUnits;
            }
            return distanceUnits;
        }

        public static bool HasValidUserAccount(this ControllerContext context)
        {
            var userAccount = UserAccount(context);
            return userAccount != null && userAccount.UserType != "N"; // 'New' - user is not quite a user until they give us their DisplayName.
        }

        public static dynamic UserAccount(this ControllerContext context)
        {
            var userAccount = context.HttpContext.Session["rp_UserAccount"] as dynamic;

            if (userAccount == null)
            {
                var userCookie = context.HttpContext.Request.Cookies["rp_UserAccount"];
                if (userCookie != null)
                {
                    long userId;
                    if (long.TryParse(Secure.DecryptValue(userCookie.Value, null), out userId))
                    {
                        userAccount = MassiveDB.Current.FindUser(userId);
                        if (userAccount != null)
                        {
                            context.HttpContext.Session["rp_UserAccount"] = userAccount;
                        }
                    }
                }
            }

            return userAccount;
        }

        public static bool HasUserAccountWeight(this ControllerContext context)
        {
            object userAccount = UserAccount(context);
            if (userAccount == null) return false;
            return userAccount.UserPrefs().Any();
        }
    }
}