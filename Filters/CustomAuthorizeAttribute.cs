using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using CustomerPortal_MVC_.Helpers;

namespace CustomerPortal_MVC_.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        public string RequiredRole { get; set; }
        public string RequiredLubricantFlag { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            // Check if Forms Authentication or Session user is logged in
            bool isAuthenticated = httpContext.User.Identity.IsAuthenticated || UserSession.IsAuthenticated;
            if (!isAuthenticated)
            {
                return false;
            }

            // If a specific role is specified via Roles property or RequiredRole
            string rolesToCheck = string.IsNullOrEmpty(Roles) ? RequiredRole : Roles;
            if (!string.IsNullOrEmpty(rolesToCheck))
            {
                bool hasRole = false;
                string[] allowedRoles = rolesToCheck.Split(',');

                foreach (var role in allowedRoles)
                {
                    string trimmedRole = role.Trim();

                    if (string.Equals(trimmedRole, "Admin", StringComparison.OrdinalIgnoreCase) && UserSession.IsAdmin)
                    {
                        hasRole = true;
                        break;
                    }

                    if (string.Equals(trimmedRole, "Customer", StringComparison.OrdinalIgnoreCase) && UserSession.IsCustomer)
                    {
                        hasRole = true;
                        break;
                    }

                    if (trimmedRole == "1" && UserSession.UserRole == 1)
                    {
                        hasRole = true;
                        break;
                    }

                    if (trimmedRole == "0" && UserSession.UserRole == 0)
                    {
                        hasRole = true;
                        break;
                    }
                }

                if (!hasRole)
                {
                    return false;
                }
            }

            // Check LubricantFlag if required
            if (!string.IsNullOrEmpty(RequiredLubricantFlag))
            {
                if (!string.Equals(UserSession.LubricantFlag, RequiredLubricantFlag, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException(nameof(filterContext));
            }

            // If it's an AJAX or API request, return JSON instead of redirecting to login HTML
            var req = filterContext.HttpContext.Request;
            bool isAjax = req.IsAjaxRequest() ||
                          (req.Headers["X-Requested-With"] == "XMLHttpRequest") ||
                          req.Path.IndexOf("/Api", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isAjax)
            {
                filterContext.HttpContext.Response.StatusCode = 200;
                filterContext.Result = new JsonResult
                {
                    Data = new { result = "error", message = "Session expired. Please log in again.", data = new object[0] },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
                return;
            }

            if (!UserSession.IsAuthenticated && !filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                // User is not logged in -> redirect to login with ReturnUrl
                string returnUrl = filterContext.HttpContext.Request.RawUrl;
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                        { "controller", "Account" },
                        { "action", "Login" },
                        { "returnUrl", returnUrl }
                    });
            }
            else
            {
                // User is logged in but lacks required role -> redirect based on actual role
                if (UserSession.IsAdmin)
                {
                    filterContext.Result = new RedirectToRouteResult(
                        new RouteValueDictionary
                        {
                            { "controller", "Admin" },
                            { "action", "Index" }
                        });
                }
                else
                {
                    filterContext.Result = new RedirectToRouteResult(
                        new RouteValueDictionary
                        {
                            { "controller", "Home" },
                            { "action", "Index" }
                        });
                }
            }
        }
    }
}
