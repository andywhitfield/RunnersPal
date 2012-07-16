using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using RunnersPal.Web.Controllers;
using RunnersPal.Web.Models.Binders;
using RunnersPal.Web.Models;
using RunnersPal.Calculators;
using RunnersPal.Web.Models.Auth;
using System.Configuration;

namespace RunnersPal.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("errorlogging.axd");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        public static void RegisterModelBinders()
        {
            ModelBinders.Binders.Add(typeof(NewRunData), new NewRunDataBinder());
            ModelBinders.Binders.Add(typeof(PaceData), new PaceDataModelBinder());
            ModelBinders.Binders.Add(typeof(Distance), new DistanceBinder());
            ModelBinders.Binders.Add(typeof(ProfileModel), new ProfileModelBinder());
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterRoutes(RouteTable.Routes);
            RegisterModelBinders();

            var twitterLoginSettings = MassiveDB.Current.FindSettings("TwitterLogin");
            TwitterLogin.ConsumerKey = twitterLoginSettings.Single(s => s.Identifier == "TwitterConsumerKey").SettingValue;
            TwitterLogin.ConsumerSecret = twitterLoginSettings.Single(s => s.Identifier == "TwitterConsumerSecret").SettingValue;
        }

        protected void Session_Start()
        {
            // refresh auth cookie
            var userCookie = HttpContext.Current.Request.Cookies["rp_UserAccount"];
            if (userCookie != null)
            {
                var cookie = new HttpCookie("rp_UserAccount", userCookie.Value);
                cookie.Expires = DateTime.UtcNow.AddYears(1);
                HttpContext.Current.Response.AppendCookie(cookie);
            }
        }
    }
}