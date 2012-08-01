using System;
using System.IO.Compression;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using RunnersPal.Calculators;
using RunnersPal.Web.Models;
using RunnersPal.Web.Models.Auth;
using RunnersPal.Web.Models.Binders;

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

        protected void Application_PreRequestHandlerExecute(object sender, EventArgs e)
        {
            var app = sender as HttpApplication;
            if (app == null) return;

            var acceptEncoding = app.Request.Headers["Accept-Encoding"];
            if (acceptEncoding == null || acceptEncoding.Length == 0)
                return;
            acceptEncoding = acceptEncoding.ToLower();

            var prevUncompressedStream = app.Response.Filter;

            if (!(app.Context.CurrentHandler is IHttpHandler) ||
                app.Request["HTTP_X_MICROSOFTAJAX"] != null)
                return;

            if (acceptEncoding.Contains("deflate") || acceptEncoding == "*")
            {
                // deflate
                app.Response.Filter = new DeflateStream(prevUncompressedStream, CompressionMode.Compress);
                app.Response.AppendHeader("Content-Encoding", "deflate");
            }
            else if (acceptEncoding.Contains("gzip"))
            {
                // gzip
                app.Response.Filter = new GZipStream(prevUncompressedStream, CompressionMode.Compress);
                app.Response.AppendHeader("Content-Encoding", "gzip");
            }
        }
    }
}