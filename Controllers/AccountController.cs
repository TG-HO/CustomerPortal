using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using CustomerPortal_MVC_.DAL;
using CustomerPortal_MVC_.Filters;
using CustomerPortal_MVC_.Helpers;
using CustomerPortal_MVC_.Models;
using CustomerPortal_MVC_.Models.ViewModels;

namespace CustomerPortal_MVC_.Controllers
{
    public class AccountController : Controller
    {
        private readonly PortalDbContext _db = new PortalDbContext();

        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (UserSession.IsAuthenticated)
            {
                if (UserSession.IsAdmin)
                {
                    return RedirectToAction("Index", "Admin");
                }
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string userId = model.UserId.Trim();
            string password = model.Password;

            // -------------------------------------------------------------
            // Step 1: Check Administrator User Directory (USERINFO)
            // -------------------------------------------------------------
            var adminUser = _db.Users.FirstOrDefault(u => u.NetworkAlias == userId && u.Enable == 1);
            if (adminUser != null && PasswordHasher.VerifyPassword(password, adminUser.Password))
            {
                // Authenticate as Administrator
                UserSession.UserId = adminUser.NetworkAlias;
                UserSession.UserRole = 1; // Admin
                UserSession.Username = adminUser.Name ?? adminUser.NetworkAlias;
                UserSession.AdminId = adminUser.NetworkAlias;
                UserSession.LubricantFlag = (adminUser.NetworkAlias == "murtaza.ali" || adminUser.LubFlag == "1") ? "1" : "2";

                // Auto-upgrade legacy plaintext password if applicable
                if (PasswordHasher.IsLegacyPlaintext(adminUser.Password))
                {
                    try
                    {
                        adminUser.Password = PasswordHasher.HashPassword(password);
                        _db.SaveChanges();
                    }
                    catch { /* Log or suppress password upgrade error */ }
                }

                // Log Session Entry into cust_sessionlog
                LogUserSession(adminUser.NetworkAlias);

                // Set Forms Authentication Cookie (supporting persistent Remember Me)
                SetFormsAuthCookie(adminUser.NetworkAlias, model.RememberMe, "Admin");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && !returnUrl.Contains(".php"))
                {
                    return Redirect(returnUrl);
                }

                if (adminUser.NetworkAlias == "murtaza.ali" || adminUser.LubFlag == "1")
                {
                    return RedirectToAction("AdminSubmittedOrders", "Lubricants");
                }

                return RedirectToAction("Index", "Admin");
            }

            // -------------------------------------------------------------
            // Step 2: Check Customer Master Directory (CUSTTABLE)
            // -------------------------------------------------------------
            var customer = _db.Customers.FirstOrDefault(c => c.AccountNum == userId && c.DataAreaId == "tgpl");
            if (customer == null)
            {
                customer = _db.Customers.FirstOrDefault(c => c.AccountNum == userId && c.DataAreaId == "aml");
            }

            if (customer != null && PasswordHasher.VerifyPassword(password, customer.Password))
            {
                // Authenticate as Customer
                UserSession.UserId = customer.AccountNum;
                UserSession.UserRole = 0; // Customer
                UserSession.CustomerCode = customer.AccountNum;
                UserSession.CustomerSite = customer.CustSite;
                UserSession.CustomerName = customer.CustSite;
                UserSession.Username = customer.AccountNum;

                // Check whether customer also has lubricant access in aml dataarea
                bool hasLubeAccess = _db.Customers.Any(c => c.AccountNum == userId && c.DataAreaId == "aml");
                UserSession.LubricantFlag = hasLubeAccess ? "1" : "0";

                // Auto-upgrade legacy plaintext password if applicable
                if (PasswordHasher.IsLegacyPlaintext(customer.Password))
                {
                    try
                    {
                        string newHash = PasswordHasher.HashPassword(password);
                        customer.Password = newHash;
                        customer.ConfirmPw = newHash;
                        _db.SaveChanges();
                    }
                    catch { /* Log or suppress password upgrade error */ }
                }

                // Log Session Entry into cust_sessionlog
                LogUserSession(customer.AccountNum);

                // Set Forms Authentication Cookie (supporting persistent Remember Me)
                SetFormsAuthCookie(customer.AccountNum, model.RememberMe, "Customer");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && !returnUrl.Contains(".php"))
                {
                    return Redirect(returnUrl);
                }

                // Always route customer to Home/Index
                return RedirectToAction("Index", "Home");
            }

            // Authentication Failed
            ModelState.AddModelError("", "Invalid User ID or Password.");
            return View(model);
        }

        // GET/POST: /Account/Logout
        public ActionResult Logout()
        {
            // Record session logout timestamp in cust_sessionlog if active
            long currentSessionId = UserSession.SessionId;
            if (currentSessionId > 0)
            {
                try
                {
                    var logRecord = _db.SessionLogs.Find(currentSessionId);
                    if (logRecord != null)
                    {
                        logRecord.TimeOut = DateTime.Now;
                        _db.SaveChanges();
                    }
                }
                catch { /* Suppress database logout audit exception */ }
            }

            // Clear strongly typed session
            UserSession.Clear();

            // Sign out of Forms Authentication
            FormsAuthentication.SignOut();

            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/ChangePassword
        [CustomAuthorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [CustomAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string currentUserId = UserSession.UserId;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login");
            }

            if (UserSession.IsAdmin)
            {
                var admin = _db.Users.FirstOrDefault(u => u.NetworkAlias == currentUserId);
                if (admin == null || !PasswordHasher.VerifyPassword(model.OldPassword, admin.Password))
                {
                    ModelState.AddModelError("OldPassword", "Current password does not match.");
                    return View(model);
                }

                admin.Password = PasswordHasher.HashPassword(model.NewPassword);
                _db.SaveChanges();
            }
            else
            {
                var cust = _db.Customers.FirstOrDefault(c => c.AccountNum == currentUserId);
                if (cust == null || !PasswordHasher.VerifyPassword(model.OldPassword, cust.Password))
                {
                    ModelState.AddModelError("OldPassword", "Current password does not match.");
                    return View(model);
                }

                string newHash = PasswordHasher.HashPassword(model.NewPassword);
                cust.Password = newHash;
                cust.ConfirmPw = newHash;
                _db.SaveChanges();
            }

            ViewBag.SuccessMessage = "Password updated successfully.";
            return View();
        }

        // GET: /Account/GetAdminName?idvalue=xxx
        [HttpGet]
        [AllowAnonymous]
        [Route("Account/GetAdminName")]
        [Route("actionadmin.php")]
        [Route("pages/actionadmin.php")]
        [Route("Account/actionadmin")]
        public ActionResult GetAdminName(string idvalue)
        {
            if (string.IsNullOrWhiteSpace(idvalue))
            {
                return Content("");
            }

            try
            {
                string trimmed = idvalue.Trim();
                string sql = @"SELECT TOP 1 NAME 
                               FROM USERINFO 
                               WHERE RTRIM(LTRIM(NETWORKALIAS)) = @p0 AND ENABLE = 1";
                var adminName = _db.Database.SqlQuery<string>(sql, trimmed).FirstOrDefault();
                if (!string.IsNullOrEmpty(adminName))
                {
                    return Content(adminName.Trim());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in GetAdminName: " + ex.Message);
            }

            return Content("");
        }

        // GET: /Account/GetCustomerName?idvalue=xxx
        [HttpGet]
        [AllowAnonymous]
        [Route("Account/GetCustomerName")]
        [Route("actionUsername.php")]
        [Route("pages/actionUsername.php")]
        [Route("Account/actionUsername")]
        public ActionResult GetCustomerName(string idvalue)
        {
            if (string.IsNullOrWhiteSpace(idvalue))
            {
                return Content("");
            }

            try
            {
                string trimmedId = idvalue.Trim();
                // 1:1 match with original PHP actionUsername.php (prioritizes tgpl then aml)
                string sql = @"SELECT TOP 1 ISNULL(DPT.NAME, CT.CustSite) AS CustName
                               FROM CUSTTABLE CT 
                               LEFT JOIN DIRPARTYTABLE DPT ON CT.PARTY = DPT.RECID 
                               WHERE RTRIM(LTRIM(CT.ACCOUNTNUM)) = @p0 
                               ORDER BY CASE WHEN CT.DATAAREAID = 'tgpl' THEN 1 WHEN CT.DATAAREAID = 'aml' THEN 2 ELSE 3 END";
                var custName = _db.Database.SqlQuery<string>(sql, trimmedId).FirstOrDefault();
                if (!string.IsNullOrEmpty(custName))
                {
                    return Content(custName.Trim());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in GetCustomerName: " + ex.Message);
            }

            return Content("");
        }

        // GET: /Account/VerifyCurrentPassword?oldpass=xxx
        [HttpGet]
        [CustomAuthorize]
        public ActionResult VerifyCurrentPassword(string oldpass)
        {
            string currentUserId = UserSession.UserId;
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(oldpass))
            {
                return Content("{\"message\":\"error\"}", "application/json");
            }

            bool isValid = false;
            if (UserSession.IsAdmin)
            {
                var admin = _db.Users.FirstOrDefault(u => u.NetworkAlias == currentUserId);
                if (admin != null && PasswordHasher.VerifyPassword(oldpass, admin.Password))
                {
                    isValid = true;
                }
            }
            else
            {
                var cust = _db.Customers.FirstOrDefault(c => c.AccountNum == currentUserId);
                if (cust != null && PasswordHasher.VerifyPassword(oldpass, cust.Password))
                {
                    isValid = true;
                }
            }

            if (isValid)
            {
                return Content("{\"message\":\"success\"}", "application/json");
            }

            return Content("{\"message\":\"error\"}", "application/json");
        }

        #region Helper Methods

        private void LogUserSession(string username)
        {
            try
            {
                string clientIp = Request.UserHostAddress ?? Request.ServerVariables["REMOTE_ADDR"];
                long maxId = _db.SessionLogs.Select(s => (long?)s.SessionId).Max() ?? 0;
                var sessionLog = new CustSessionLog
                {
                    SessionId = maxId + 1,
                    TimeIn = DateTime.Now,
                    Ip = clientIp,
                    Username = username
                };

                _db.SessionLogs.Add(sessionLog);
                _db.SaveChanges();

                UserSession.SessionId = sessionLog.SessionId;
            }
            catch { /* Suppress audit logging error to allow login completion */ }
        }

        private void SetFormsAuthCookie(string username, bool rememberMe, string roleData)
        {
            int expirationMinutes = rememberMe ? 43200 : 60; // 30 days if Remember Me, else 60 mins
            var ticket = new FormsAuthenticationTicket(
                1,
                username,
                DateTime.Now,
                DateTime.Now.AddMinutes(expirationMinutes),
                rememberMe,
                roleData,
                FormsAuthentication.FormsCookiePath);

            string encryptedTicket = FormsAuthentication.Encrypt(ticket);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
            {
                HttpOnly = true,
                Secure = Request.IsSecureConnection,
                Path = FormsAuthentication.FormsCookiePath
            };

            if (rememberMe)
            {
                cookie.Expires = ticket.Expiration;
            }

            Response.Cookies.Add(cookie);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetUsername(string idvalue)
        {
            var user = _db.Customers
                         .Where(c => c.AccountNum == idvalue)
                         .Select(c => c.CustSite ?? c.AccountNum)
                         .FirstOrDefault();

            return Json(new { success = true, username = user ?? "" }, JsonRequestBehavior.AllowGet);
        }
    }
}
