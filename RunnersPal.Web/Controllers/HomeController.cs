using System;
using System.Diagnostics;
using System.Globalization;
using System.Web;
using System.Web.Mvc;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.RelyingParty;
using RunnersPal.Web.Extensions;
using RunnersPal.Web.Models.Auth;

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
                            
                            Trace.TraceInformation("Completed twitter auth...token: " + openId);

                            var userAccount = UserLoggingIn(openId);
                            if (userAccount.UserType == "N")
                            {
                                Session["login_friendlyname"] = name;
                                return RedirectToAction("FirstTime", "User");
                            }

                            return RedirectToReturnPage();
                        }
                    }
                }

                SaveReturnPageToSession();

                // login requested - redirect to openid or openauth provider
                var loginUri = Request.Form["openid_identifier"];

                if (loginUri == "https://twitter.com/")
                {
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
                            return openid.CreateRequest(Request.Form["openid_identifier"]).RedirectingResponse.AsActionResult();
                        }
                        catch (ProtocolException ex)
                        {
                            Trace.TraceWarning("Cannot send request to OpenID provider: ", ex);
                            return RedirectToReturnPage("Could not send the login request to the OpenID provider. Check the service is available and try again. The error message is: " + ex.Message);
                        }
                    }
                    else
                    {
                        Trace.TraceWarning("Invalid openid identifier!");
                        return RedirectToReturnPage("Could not login using provided OpenID identifier. Please check the URL provided and try again.");
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

                        var userAccount = UserLoggingIn(response.ClaimedIdentifier.ToString());
                        if (userAccount.UserType == "N")
                            return RedirectToAction("FirstTime", "User");

                        return RedirectToReturnPage();
                    case AuthenticationStatus.Canceled:
                        Trace.TraceInformation("Canceled openid auth");
                        return RedirectToReturnPage("The login process was cancelled. Please try logging in again.");
                    case AuthenticationStatus.Failed:
                        Trace.TraceError("Failed authenticating openid: " + response.Exception);
                        return RedirectToReturnPage("A failure occurred trying to login. This may be a temporary problem, so please try again. The error message is: " + (response.Exception == null ? "Unknown error." : response.Exception.Message));
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

        private dynamic UserLoggingIn(string openId)
        {
            var userAccount = MassiveDB.Current.FindUser(openId);
            if (userAccount == null)
                userAccount = MassiveDB.Current.CreateUser(openId, Request.UserHostAddress, ControllerContext.UserDistanceUnits());

            HttpContext.Session["rp_UserAccount"] = userAccount;
            var cookie = new HttpCookie("rp_UserAccount", Secure.EncryptValue(userAccount.Id.ToString()));
            cookie.Expires = DateTime.UtcNow.AddYears(1);
            HttpContext.Response.AppendCookie(cookie);
            return userAccount;
        }

        private void SaveReturnPageToSession()
        {
            var returnPage = Request.Form["return_page"];
            if (string.IsNullOrWhiteSpace(returnPage)) returnPage = Url.Content("~/");
            Uri returnPageUri;
            Uri.TryCreate(returnPage, UriKind.RelativeOrAbsolute, out returnPageUri);
            if (returnPageUri == null || returnPageUri.IsAbsoluteUri)
                returnPage = Url.Content("~/");
            Session["login_returnPage"] = returnPage;
        }

        private ActionResult RedirectToReturnPage(string errorMessage = null)
        {
            Session["login_errorMessage"] = errorMessage;
            var returnPage = Session["login_returnPage"] as string;
            if (string.IsNullOrWhiteSpace(returnPage))
                return RedirectToAction("Index", "Home");
            return Redirect(returnPage);
        }
    }
}
