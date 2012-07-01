using System;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.RelyingParty;
using System.Web;
using RunnersPal.Web.Extensions;
using RunnersPal.Web.Models.Auth;
using System.Globalization;
using System.Configuration;

namespace RunnersPal.Web.Controllers
{
    public class HomeController : Controller
    {
        private static OpenIdRelyingParty openid = new OpenIdRelyingParty();

        public ActionResult Index()
        {
            Response.AppendHeader(
                "X-XRDS-Location",
                new Uri(Request.Url, Response.ApplyAppPathModifier("~/home/xrds")).AbsoluteUri);

            return View();
        }

        public ActionResult Xrds() { return View(); }

        [ValidateInput(false)]
        public ActionResult Login()
        {
            var uri = Request.RawUrl;

            var response = openid.GetResponse();
            if (response == null)
            {
                if (!string.IsNullOrEmpty(Request["oauth_token"]))
                {
                    if ((Session["login_service"] as string) == "twitter")
                    {
                        string name;
                        int id;
                        if (TwitterLogin.TryFinishSignInWithTwitter(out name, out id))
                        {
                            var openId = string.Format(CultureInfo.InvariantCulture, "http://twitter.com/{0}#{1}", name, id);
                            
                            Trace.TraceInformation("Completed openid auth: " + openId);

                            var userAccount = MassiveDB.Current.FindUser(openId);
                            if (userAccount == null)
                                userAccount = MassiveDB.Current.CreateUser(openId, Request.UserHostAddress, ControllerContext.UserDistanceUnits());

                            HttpContext.Session["rp_UserAccount"] = userAccount;
                            var cookie = new HttpCookie("rp_UserAccount", Secure.EncryptValue(userAccount.Id.ToString()));
                            cookie.Expires = DateTime.UtcNow.AddYears(1);
                            HttpContext.Response.AppendCookie(cookie);

                            if (userAccount.UserType == "N")
                            {
                                Session["login_friendlyname"] = name;
                                return RedirectToAction("FirstTime", "User");
                            }

                            var returnPage = Session["login_returnPage"] as string;
                            if (string.IsNullOrWhiteSpace(returnPage))
                                return RedirectToAction("Index", "Home");
                            return Redirect(returnPage);
                        }
                    }
                }

                // login requested - redirect to openid or openauth provider
                var loginUri = Request.Form["openid_identifier"];

                if (loginUri == "https://twitter.com/")
                {
                    var returnPage = Request.Form["return_page"];
                    if (string.IsNullOrWhiteSpace(returnPage)) returnPage = Url.Content("~/");
                    Uri returnPageUri;
                    Uri.TryCreate(returnPage, UriKind.RelativeOrAbsolute, out returnPageUri);
                    if (returnPageUri == null || returnPageUri.IsAbsoluteUri)
                        returnPage = Url.Content("~/");

                    Session["login_returnPage"] = returnPage;
                    Session["login_service"] = "twitter";
                    return TwitterLogin.StartSignInWithTwitter(false).AsActionResult();
                }
                else
                {
                    Identifier id;
                    if (Identifier.TryParse(loginUri, out id))
                    {
                        try
                        {
                            var returnPage = Request.Form["return_page"];
                            if (string.IsNullOrWhiteSpace(returnPage)) returnPage = Url.Content("~/");
                            Uri returnPageUri;
                            Uri.TryCreate(returnPage, UriKind.RelativeOrAbsolute, out returnPageUri);
                            if (returnPageUri == null || returnPageUri.IsAbsoluteUri)
                                returnPage = Url.Content("~/");

                            Session["login_returnPage"] = returnPage;
                            return openid.CreateRequest(Request.Form["openid_identifier"]).RedirectingResponse.AsActionResult();
                        }
                        catch (ProtocolException ex)
                        {
                            Trace.TraceError("Error sending to openid: " + ex);
                            ViewData["Message"] = ex.Message;
                            return View("Index");
                        }
                    }
                    else
                    {
                        Trace.TraceWarning("Invalid openid identifier!");
                        ViewData["Message"] = "Invalid identifier";
                        return View("Index");
                    }
                }
            }
            else
            {
                // OpenID provider sending assertion response
                switch (response.Status)
                {
                    case AuthenticationStatus.Authenticated:
                        Trace.TraceInformation("Completed openid auth: " + response.FriendlyIdentifierForDisplay);

                        var userAccount = MassiveDB.Current.FindUser(response.ClaimedIdentifier.ToString());
                        if (userAccount == null)
                            userAccount = MassiveDB.Current.CreateUser(response.ClaimedIdentifier.ToString(), Request.UserHostAddress, ControllerContext.UserDistanceUnits());

                        HttpContext.Session["rp_UserAccount"] = userAccount;
                        var cookie = new HttpCookie("rp_UserAccount", Secure.EncryptValue(userAccount.Id.ToString()));
                        cookie.Expires = DateTime.UtcNow.AddYears(1);
                        HttpContext.Response.AppendCookie(cookie);

                        if (userAccount.UserType == "N")
                            return RedirectToAction("FirstTime", "User");

                        var returnPage = Session["login_returnPage"] as string;
                        if (string.IsNullOrWhiteSpace(returnPage))
                            return RedirectToAction("Index", "Home");
                        return Redirect(returnPage);
                    case AuthenticationStatus.Canceled:
                        Trace.TraceInformation("Canceled openid auth");
                        ViewData["Message"] = "Canceled at provider";
                        return View("Index");
                    case AuthenticationStatus.Failed:
                        Trace.TraceError("Failed authenticating openid: " + response.Exception);
                        ViewData["Message"] = response.Exception.Message;
                        return View("Index");
                }
            }
            return new EmptyResult();
        }

        [HttpPost]
        public ActionResult UpdateDistanceUnits(int distanceUnit)
        {
            Trace.WriteLine("Updating units for user to " + distanceUnit);
            if (!Enum.IsDefined(typeof(DistanceUnits), distanceUnit))
                return new JsonResult { Data = new { Completed = false, Reason = "Invalid distance unit." } };

            var newDistanceUnit = (DistanceUnits)distanceUnit;
            HttpContext.Session["rp_UserDistanceUnits"] = newDistanceUnit;

            var userAccount = ControllerContext.UserAccount();
            if (userAccount != null)
            {
                userAccount.DistanceUnits = newDistanceUnit;
                MassiveDB.Current.UpdateUser(userAccount);
            }

            return new JsonResult { Data = new { Completed = true } };
        }
    }
}
