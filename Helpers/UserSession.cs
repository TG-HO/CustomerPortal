using System;
using System.Web;

namespace CustomerPortal_MVC_.Helpers
{
    /// <summary>
    /// Strongly typed wrapper around HttpContext.Current.Session.
    /// Manages all session variables identified in the legacy PHP codebase functional audit.
    /// </summary>
    public static class UserSession
    {
        private const string KeyUserId = "userid";
        private const string KeyUserRole = "userRole";
        private const string KeyLubricantFlag = "lubricantflag";
        private const string KeyCustomerSite = "customersite";
        private const string KeyCustomerName = "customername";
        private const string KeyCustomerCode = "customercode";
        private const string KeySessionId = "sessionid";
        private const string KeyUsername = "Username";
        private const string KeyAdminId = "AdminID";
        private const string KeyOAuth2State = "oauth2state";
        private const string KeyProvider = "provider";
        private const string KeyImage = "image";
        private const string KeyTitle = "title";

        private static HttpSessionStateBase Session
        {
            get
            {
                if (HttpContext.Current == null || HttpContext.Current.Session == null)
                {
                    return null;
                }
                return new HttpSessionStateWrapper(HttpContext.Current.Session);
            }
        }

        public static string UserId
        {
            get
            {
                string id = GetString(KeyUserId);
                if (string.IsNullOrEmpty(id) && HttpContext.Current?.User?.Identity?.IsAuthenticated == true)
                {
                    id = HttpContext.Current.User.Identity.Name;
                    SetString(KeyUserId, id);
                }
                return id;
            }
            set => SetString(KeyUserId, value);
        }

        public static int UserRole
        {
            get => GetInt(KeyUserRole, -1);
            set => SetInt(KeyUserRole, value);
        }

        public static string LubricantFlag
        {
            get => GetString(KeyLubricantFlag);
            set => SetString(KeyLubricantFlag, value);
        }

        public static string CustomerSite
        {
            get => GetString(KeyCustomerSite);
            set => SetString(KeyCustomerSite, value);
        }

        public static string CustomerName
        {
            get => GetString(KeyCustomerName);
            set => SetString(KeyCustomerName, value);
        }

        public static string CustomerCode
        {
            get => GetString(KeyCustomerCode);
            set => SetString(KeyCustomerCode, value);
        }

        public static long SessionId
        {
            get => GetLong(KeySessionId, 0);
            set => SetLong(KeySessionId, value);
        }

        public static string Username
        {
            get => GetString(KeyUsername);
            set => SetString(KeyUsername, value);
        }

        public static string AdminId
        {
            get => GetString(KeyAdminId);
            set => SetString(KeyAdminId, value);
        }

        public static string OAuth2State
        {
            get => GetString(KeyOAuth2State);
            set => SetString(KeyOAuth2State, value);
        }

        public static string Provider
        {
            get => GetString(KeyProvider);
            set => SetString(KeyProvider, value);
        }

        public static string Image
        {
            get => GetString(KeyImage);
            set => SetString(KeyImage, value);
        }

        public static string Title
        {
            get => GetString(KeyTitle);
            set => SetString(KeyTitle, value);
        }

        public static bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

        public static bool IsAdmin => IsAuthenticated && UserRole == 1;

        public static bool IsCustomer => IsAuthenticated && UserRole == 0;

        public static void Clear()
        {
            if (Session != null)
            {
                Session.Clear();
                Session.Abandon();
            }
        }

        #region Private Helper Methods

        private static string GetString(string key)
        {
            return Session?[key] as string;
        }

        private static void SetString(string key, string value)
        {
            if (Session != null)
            {
                Session[key] = value;
            }
        }

        private static int GetInt(string key, int defaultValue)
        {
            var val = Session?[key];
            if (val is int iVal) return iVal;
            if (val != null && int.TryParse(val.ToString(), out int parsed)) return parsed;
            return defaultValue;
        }

        private static void SetInt(string key, int value)
        {
            if (Session != null)
            {
                Session[key] = value;
            }
        }

        private static long GetLong(string key, long defaultValue)
        {
            var val = Session?[key];
            if (val is long lVal) return lVal;
            if (val != null && long.TryParse(val.ToString(), out long parsed)) return parsed;
            return defaultValue;
        }

        private static void SetLong(string key, long value)
        {
            if (Session != null)
            {
                Session[key] = value;
            }
        }

        #endregion
    }
}
