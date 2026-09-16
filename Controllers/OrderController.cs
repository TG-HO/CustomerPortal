using System;
using System.Linq;
using System.Web.Mvc;
using CustomerPortal_MVC_.DAL;
using CustomerPortal_MVC_.Filters;
using CustomerPortal_MVC_.Helpers;
using CustomerPortal_MVC_.Models.ViewModels;
using CustomerPortal_MVC_.Services;

namespace CustomerPortal_MVC_.Controllers
{
    [CustomAuthorize(Roles = "Customer")]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;

        public OrderController()
        {
            _orderService = new OrderService();
            _customerService = new CustomerService();
        }

        public OrderController(IOrderService orderService, ICustomerService customerService)
        {
            _orderService = orderService;
            _customerService = customerService;
        }

        // GET: /Order/OrderNow
        public ActionResult OrderNow()
        {
            string customerId = UserSession.UserId;
            PopulateOrderNowViewBag(customerId);

            var model = new OrderCreateViewModel
            {
                Site = UserSession.CustomerSite ?? "Main Depot",
                AvailableCredit = ViewBag.AvailableCreditLimitNum != null ? (decimal)ViewBag.AvailableCreditLimitNum : 0m,
                TankCapacity = _customerService.GetTankCapacity(customerId, "01"),
                UnitRate = _customerService.GetDealerRate(customerId, "01")
            };

            return View(model);
        }

        // POST: /Order/OrderNow
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OrderNow(OrderCreateViewModel model)
        {
            string customerId = UserSession.UserId;

            if (string.IsNullOrEmpty(model.ProductCode) || model.ProductCode == "-1")
            {
                ModelState.AddModelError("ProductCode", "Please select a product.");
            }

            if (model.NewOrderQuantity < 8000)
            {
                ModelState.AddModelError("NewOrderQuantity", "Minimum Order placed should contain 8,000 quantity.");
            }

            if (!ModelState.IsValid)
            {
                PopulateOrderNowViewBag(customerId);
                return View(model);
            }

            try
            {
                var createdOrder = _orderService.CreateOrder(model, customerId, UserSession.LubricantFlag);

                decimal rate = _customerService.GetDealerRate(customerId, model.ProductCode);
                decimal totalAmount = rate * model.NewOrderQuantity;

                ViewBag.OrderPlaced = true;
                ViewBag.OrderPrefixId = createdOrder.OrderPrefixId;
                ViewBag.OrderedQuantity = model.NewOrderQuantity.ToString("N0");
                ViewBag.Site = model.Site ?? UserSession.CustomerSite ?? "Main Depot";
                ViewBag.DealerRate = rate.ToString("N2");
                ViewBag.TotalAmount = totalAmount.ToString("N0");
                ViewBag.CreditLimit = _customerService.GetCreditLimit(customerId).ToString("N0");
                ViewBag.Balance = _customerService.GetCustomerBalance(customerId).ToString("N0");

                PopulateOrderNowViewBag(customerId);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error creating order: " + ex.Message);
                PopulateOrderNowViewBag(customerId);
                return View(model);
            }
        }

        private void PopulateOrderNowViewBag(string customerId)
        {
            decimal creditLimit = _customerService.GetCreditLimit(customerId);
            decimal custBalance = _customerService.GetCustomerBalance(customerId);
            decimal rateHsd = _customerService.GetDealerRate(customerId, "01");
            decimal ratePmg = _customerService.GetDealerRate(customerId, "02");
            decimal rateHobc = _customerService.GetDealerRate(customerId, "007");

            using (var db = new PortalDbContext())
            {
                try
                {
                    decimal dynHsd = db.Database.SqlQuery<decimal?>(
                        "select isnull(sum(sl.SALESQTY),0) from SALESLINE as sl where sl.SALESSTATUS in (1,2) and itemid = '01' and sl.DATAAREAID = 'tgpl' and sl.CUSTACCOUNT = @p0", customerId).FirstOrDefault() ?? 0m;
                    decimal dynPmg = db.Database.SqlQuery<decimal?>(
                        "select isnull(sum(sl.SALESQTY),0) from SALESLINE as sl where sl.SALESSTATUS in (1,2) and itemid = '02' and sl.DATAAREAID = 'tgpl' and sl.CUSTACCOUNT = @p0", customerId).FirstOrDefault() ?? 0m;
                    decimal dynHobc = db.Database.SqlQuery<decimal?>(
                        "select isnull(sum(sl.SALESQTY),0) from SALESLINE as sl where sl.SALESSTATUS in (1,2) and itemid = '007' and sl.DATAAREAID = 'tgpl' and sl.CUSTACCOUNT = @p0", customerId).FirstOrDefault() ?? 0m;

                    decimal otHsdHfFull = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(HOLDFREEQTY),0) from custordertable where ORDERCREATEDUSER = @p0 AND PRODUCTCODE = '01' AND HOLDSFREE_FLAG = 1 and (RELEASED_FLAG = 0 or RELEASED_FLAG is null) and (CANCEL_FLAG = 0 or CANCEL_FLAG is null) and (SHIPPED_FLAG = 0 or SHIPPED_FLAG is null)", customerId).FirstOrDefault() ?? 0m;
                    decimal otHsdHfPartial = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(HOLDFREEQTY),0) from custordertable where ORDERCREATEDUSER = @p0 AND PRODUCTCODE = '01' AND PARTIALHF_FLAG = 1 and PENDING_FLAG = 1 AND (RELEASED_FLAG = 0 or RELEASED_FLAG is null) and (CANCEL_FLAG = 0 or CANCEL_FLAG is null) and (SHIPPED_FLAG = 0 or SHIPPED_FLAG is null)", customerId).FirstOrDefault() ?? 0m;
                    decimal otHsdHfBal = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(HOLDSFREEBALANCE),0) from custordertable where ORDERCREATEDUSER = @p0 AND PRODUCTCODE = '01' AND HOLDSFREEBALANCE > 0 and (RELEASED_FLAG = 1 OR SHIPPED_FLAG = 1) and (CANCEL_FLAG = 0 or CANCEL_FLAG is null)", customerId).FirstOrDefault() ?? 0m;
                    decimal otHsdRelease = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(RELEASEQTY),0) from custordertable where ORDERCREATEDUSER = @p0 AND PRODUCTCODE = '01' AND RELEASED_FLAG = 1 AND (SHIPPED_FLAG = 0 OR SHIPPED_FLAG IS NULL) AND (CANCEL_FLAG = 0 or CANCEL_FLAG is null)", customerId).FirstOrDefault() ?? 0m;
                    decimal otHsdPending = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(PENDING_QTY),0) from custordertable where ORDERCREATEDUSER = @p0 AND PRODUCTCODE = '01' AND PENDING_FLAG = 1 and (CANCEL_FLAG = 0 or CANCEL_FLAG is null)", customerId).FirstOrDefault() ?? 0m;
                    decimal otHsdTotal = otHsdHfFull + otHsdHfPartial + otHsdHfBal + otHsdRelease + otHsdPending;

                    decimal otPmgHfFull = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(HOLDFREEQTY),0) from custordertable where ORDERCREATEDUSER = @p0 AND PRODUCTCODE = '02' AND HOLDSFREE_FLAG = 1 and (RELEASED_FLAG = 0 or RELEASED_FLAG is null) and (CANCEL_FLAG = 0 or CANCEL_FLAG is null) and (SHIPPED_FLAG = 0 or SHIPPED_FLAG is null)", customerId).FirstOrDefault() ?? 0m;
                    decimal otPmgHfPartial = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(HOLDFREEQTY),0) from custordertable where ORDERCREATEDUSER = @p0 AND PRODUCTCODE = '02' AND PARTIALHF_FLAG = 1 and PENDING_FLAG = 1 AND (RELEASED_FLAG = 0 or RELEASED_FLAG is null) and (CANCEL_FLAG = 0 or CANCEL_FLAG is null) and (SHIPPED_FLAG = 0 or SHIPPED_FLAG is null)", customerId).FirstOrDefault() ?? 0m;
                    decimal otPmgHfBal = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(HOLDSFREEBALANCE),0) from custordertable where ORDERCREATEDUSER = @p0 AND PRODUCTCODE = '02' AND HOLDSFREEBALANCE > 0 and (RELEASED_FLAG = 1 OR SHIPPED_FLAG = 1) and (CANCEL_FLAG = 0 or CANCEL_FLAG is null)", customerId).FirstOrDefault() ?? 0m;
                    decimal otPmgRelease = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(RELEASEQTY),0) from custordertable where ORDERCREATEDUSER = @p0 AND PRODUCTCODE = '02' AND RELEASED_FLAG = 1 AND (SHIPPED_FLAG = 0 OR SHIPPED_FLAG IS NULL) AND (CANCEL_FLAG = 0 or CANCEL_FLAG is null)", customerId).FirstOrDefault() ?? 0m;
                    decimal otPmgPending = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(PENDING_QTY),0) from custordertable where ORDERCREATEDUSER = @p0 AND PRODUCTCODE = '02' AND PENDING_FLAG = 1 and (CANCEL_FLAG = 0 or CANCEL_FLAG is null)", customerId).FirstOrDefault() ?? 0m;
                    decimal otPmgTotal = otPmgHfFull + otPmgHfPartial + otPmgHfBal + otPmgRelease + otPmgPending;

                    decimal otHobcHfFull = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(HOLDFREEQTY),0) from custordertable where ORDERCREATEDUSER = @p0 AND PRODUCTCODE = '007' AND HOLDSFREE_FLAG = 1 and (RELEASED_FLAG = 0 or RELEASED_FLAG is null) and (CANCEL_FLAG = 0 or CANCEL_FLAG is null) and (SHIPPED_FLAG = 0 or SHIPPED_FLAG is null)", customerId).FirstOrDefault() ?? 0m;
                    decimal otHobcHfPartial = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(HOLDFREEQTY),0) from custordertable where ORDERCREATEDUSER = @p0 AND PRODUCTCODE = '007' AND PARTIALHF_FLAG = 1 and PENDING_FLAG = 1 AND (RELEASED_FLAG = 0 or RELEASED_FLAG is null) and (CANCEL_FLAG = 0 or CANCEL_FLAG is null) and (SHIPPED_FLAG = 0 or SHIPPED_FLAG is null)", customerId).FirstOrDefault() ?? 0m;
                    decimal otHobcHfBal = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(HOLDSFREEBALANCE),0) from custordertable where ORDERCREATEDUSER = @p0 AND PRODUCTCODE = '007' AND HOLDSFREEBALANCE > 0 and (RELEASED_FLAG = 1 OR SHIPPED_FLAG = 1) and (CANCEL_FLAG = 0 or CANCEL_FLAG is null)", customerId).FirstOrDefault() ?? 0m;
                    decimal otHobcRelease = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(RELEASEQTY),0) from custordertable where ORDERCREATEDUSER = @p0 AND PRODUCTCODE = '007' AND RELEASED_FLAG = 1 AND (SHIPPED_FLAG = 0 OR SHIPPED_FLAG IS NULL) AND (CANCEL_FLAG = 0 or CANCEL_FLAG is null)", customerId).FirstOrDefault() ?? 0m;
                    decimal otHobcPending = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(PENDING_QTY),0) from custordertable where ORDERCREATEDUSER = @p0 AND PRODUCTCODE = '007' AND PENDING_FLAG = 1 and (CANCEL_FLAG = 0 or CANCEL_FLAG is null)", customerId).FirstOrDefault() ?? 0m;
                    decimal otHobcTotal = otHobcHfFull + otHobcHfPartial + otHobcHfBal + otHobcRelease + otHobcPending;

                    decimal totalBalance = (dynHsd * rateHsd) + (dynPmg * ratePmg) + (dynHobc * rateHobc)
                                         + (otHsdTotal * rateHsd) + (otPmgTotal * ratePmg) + (otHobcTotal * rateHobc)
                                         + custBalance;

                    decimal acl = creditLimit - totalBalance;

                    int hsdOpenCount = db.Database.SqlQuery<int>(
                        "select count(ORDERID) from custordertable where ORDERCREATEDUSER = @p0 and PRODUCTCODE = '01' and PENDING_FLAG = 1 and HOLDFREEQTY = 0 AND PENDING_QTY > 0 AND (CANCEL_FLAG = 0 OR CANCEL_FLAG IS NULL) and APPROVEDSTATUS = 0", customerId).FirstOrDefault();
                    int pmgOpenCount = db.Database.SqlQuery<int>(
                        "select count(ORDERID) from custordertable where ORDERCREATEDUSER = @p0 and PRODUCTCODE = '02' and PENDING_FLAG = 1 and HOLDFREEQTY = 0 AND PENDING_QTY > 0 AND (CANCEL_FLAG = 0 OR CANCEL_FLAG IS NULL) and APPROVEDSTATUS = 0", customerId).FirstOrDefault();
                    int hobcOpenCount = db.Database.SqlQuery<int>(
                        "select count(ORDERID) from custordertable where ORDERCREATEDUSER = @p0 and PRODUCTCODE = '007' and PENDING_FLAG = 1 and HOLDFREEQTY = 0 AND PENDING_QTY > 0 AND (CANCEL_FLAG = 0 OR CANCEL_FLAG IS NULL) and APPROVEDSTATUS = 0", customerId).FirstOrDefault();

                    decimal hsdOpenQty = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(PENDING_QTY),0) from custordertable where ORDERCREATEDUSER = @p0 and PRODUCTCODE = '01' and PENDING_FLAG = 1 and HOLDFREEQTY = 0 AND PENDING_QTY > 0 AND (CANCEL_FLAG = 0 OR CANCEL_FLAG IS NULL) and APPROVEDSTATUS = 0", customerId).FirstOrDefault() ?? 0m;
                    decimal pmgOpenQty = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(PENDING_QTY),0) from custordertable where ORDERCREATEDUSER = @p0 and PRODUCTCODE = '02' and PENDING_FLAG = 1 and HOLDFREEQTY = 0 AND PENDING_QTY > 0 AND (CANCEL_FLAG = 0 OR CANCEL_FLAG IS NULL) and APPROVEDSTATUS = 0", customerId).FirstOrDefault() ?? 0m;
                    decimal hobcOpenQty = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(PENDING_QTY),0) from custordertable where ORDERCREATEDUSER = @p0 and PRODUCTCODE = '007' and PENDING_FLAG = 1 and HOLDFREEQTY = 0 AND PENDING_QTY > 0 AND (CANCEL_FLAG = 0 OR CANCEL_FLAG IS NULL) and APPROVEDSTATUS = 0", customerId).FirstOrDefault() ?? 0m;

                    decimal hsdHfQty = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(HOLDSFREEBALANCE),0) from custordertable where ORDERCREATEDUSER = @p0 and PRODUCTCODE = '01' and HOLDFREEQTY > 0 AND HOLDSFREEBALANCE > 0", customerId).FirstOrDefault() ?? 0m;
                    decimal pmgHfQty = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(HOLDSFREEBALANCE),0) from custordertable where ORDERCREATEDUSER = @p0 and PRODUCTCODE = '02' and HOLDFREEQTY > 0 AND HOLDSFREEBALANCE > 0", customerId).FirstOrDefault() ?? 0m;
                    decimal hobcHfQty = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(HOLDSFREEBALANCE),0) from custordertable where ORDERCREATEDUSER = @p0 and PRODUCTCODE = '007' and HOLDFREEQTY > 0 AND HOLDSFREEBALANCE > 0", customerId).FirstOrDefault() ?? 0m;

                    int hsdHfRecords = db.Database.SqlQuery<int>(
                        "select COUNT(HOLDSFREEBALANCE) from custordertable where ORDERCREATEDUSER = @p0 and PRODUCTCODE = '01' and HOLDFREEQTY > 0 AND HOLDSFREEBALANCE > 0", customerId).FirstOrDefault();
                    int pmgHfRecords = db.Database.SqlQuery<int>(
                        "select COUNT(HOLDSFREEBALANCE) from custordertable where ORDERCREATEDUSER = @p0 and PRODUCTCODE = '02' and HOLDFREEQTY > 0 AND HOLDSFREEBALANCE > 0", customerId).FirstOrDefault();
                    int hobcHfRecords = db.Database.SqlQuery<int>(
                        "select COUNT(HOLDSFREEBALANCE) from custordertable where ORDERCREATEDUSER = @p0 and PRODUCTCODE = '007' and HOLDFREEQTY > 0 AND HOLDSFREEBALANCE > 0", customerId).FirstOrDefault();

                    decimal hsdAvailOpen = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(PENDING_QTY),0) from custordertable where ORDERCREATEDUSER = @p0 and PENDING_FLAG = 1 and PRODUCTNAME = 'High Speed Diesel' and APPROVEDSTATUS = 0", customerId).FirstOrDefault() ?? 0m;
                    decimal pmgAvailOpen = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(PENDING_QTY),0) from custordertable where ORDERCREATEDUSER = @p0 and PENDING_FLAG = 1 and PRODUCTNAME = 'Premier Motor Gasoline' and APPROVEDSTATUS = 0", customerId).FirstOrDefault() ?? 0m;
                    decimal hobcAvailOpen = db.Database.SqlQuery<decimal?>(
                        "select isnull(SUM(PENDING_QTY),0) from custordertable where ORDERCREATEDUSER = @p0 and PENDING_FLAG = 1 and PRODUCTNAME = 'HOBC' and APPROVEDSTATUS = 0", customerId).FirstOrDefault() ?? 0m;

                    ViewBag.CustomerBalance = custBalance.ToString("N0");
                    ViewBag.CreditLimit = creditLimit.ToString("N0");
                    ViewBag.AvailableCreditLimit = acl.ToString("N0");
                    ViewBag.AvailableCreditLimitNum = acl;

                    ViewBag.HsdOpenQty = hsdOpenQty.ToString("N0");
                    ViewBag.PmgOpenQty = pmgOpenQty.ToString("N0");
                    ViewBag.HobcOpenQty = hobcOpenQty.ToString("N0");

                    ViewBag.HsdOpenOrdersCount = hsdOpenCount;
                    ViewBag.PmgOpenOrdersCount = pmgOpenCount;
                    ViewBag.HobcOpenOrdersCount = hobcOpenCount;

                    ViewBag.HsdHfQty = hsdHfQty.ToString("N0");
                    ViewBag.PmgHfQty = pmgHfQty.ToString("N0");
                    ViewBag.HobcHfQty = hobcHfQty.ToString("N0");

                    ViewBag.HsdHfRecordsCount = hsdHfRecords;
                    ViewBag.PmgHfRecordsCount = pmgHfRecords;
                    ViewBag.HobcHfRecordsCount = hobcHfRecords;

                    ViewBag.HsdAvailableOpenQty = hsdAvailOpen.ToString("N0");
                    ViewBag.PmgAvailableOpenQty = pmgAvailOpen.ToString("N0");
                    ViewBag.HobcAvailableOpenQty = hobcAvailOpen.ToString("N0");
                }
                catch
                {
                    ViewBag.CustomerBalance = custBalance.ToString("N0");
                    ViewBag.CreditLimit = creditLimit.ToString("N0");
                    ViewBag.AvailableCreditLimit = creditLimit.ToString("N0");
                    ViewBag.AvailableCreditLimitNum = creditLimit;

                    ViewBag.HsdOpenQty = "0";
                    ViewBag.PmgOpenQty = "0";
                    ViewBag.HobcOpenQty = "0";
                    ViewBag.HsdOpenOrdersCount = 0;
                    ViewBag.PmgOpenOrdersCount = 0;
                    ViewBag.HobcOpenOrdersCount = 0;
                    ViewBag.HsdHfQty = "0";
                    ViewBag.PmgHfQty = "0";
                    ViewBag.HobcHfQty = "0";
                    ViewBag.HsdHfRecordsCount = 0;
                    ViewBag.PmgHfRecordsCount = 0;
                    ViewBag.HobcHfRecordsCount = 0;
                    ViewBag.HsdAvailableOpenQty = "0";
                    ViewBag.PmgAvailableOpenQty = "0";
                    ViewBag.HobcAvailableOpenQty = "0";
                }
            }
        }

        // GET: /Order/ActionPendingQty?ddlCode=01
        [HttpGet]
        public ActionResult ActionPendingQty(string ddlCode)
        {
            string customerId = UserSession.UserId;
            if (string.IsNullOrEmpty(customerId))
            {
                return Json(new { status = "No record", value = 0 }, JsonRequestBehavior.AllowGet);
            }

            using (var db = new PortalDbContext())
            {
                try
                {
                    decimal pending = db.Database.SqlQuery<decimal?>(
                        @"select isnull(SUM(PENDING_QTY),0) from CUSTORDERTABLE
                          where ORDERCREATEDUSER = @p0 and PRODUCTCODE = @p1 and PENDING_FLAG = 1
                          and PENDING_QTY > 0 AND HOLDFREEQTY > 0 AND (CANCEL_FLAG = 0 OR CANCEL_FLAG IS NULL) AND RELEASED_FLAG = 1",
                        customerId, ddlCode).FirstOrDefault() ?? 0m;

                    return Json(new { status = "success", value = pending }, JsonRequestBehavior.AllowGet);
                }
                catch
                {
                    return Json(new { status = "No record", value = 0 }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        // GET: /Order/ActionHfRemQty?ddlCode=01
        [HttpGet]
        public ActionResult ActionHfRemQty(string ddlCode)
        {
            string customerId = UserSession.UserId;
            if (string.IsNullOrEmpty(customerId))
            {
                return Json(new { status = "No record", value = 0 }, JsonRequestBehavior.AllowGet);
            }

            using (var db = new PortalDbContext())
            {
                try
                {
                    decimal hfBal = db.Database.SqlQuery<decimal?>(
                        @"select isnull(SUM(HOLDSFREEBALANCE),0) from CUSTORDERTABLE
                          where ORDERCREATEDUSER = @p0 and PRODUCTCODE = @p1 
                          and HOLDSFREEBALANCE > 0 AND HOLDFREEQTY > 0 AND (CANCEL_FLAG = 0 OR CANCEL_FLAG IS NULL)",
                        customerId, ddlCode).FirstOrDefault() ?? 0m;

                    return Json(new { status = "success", value = hfBal }, JsonRequestBehavior.AllowGet);
                }
                catch
                {
                    return Json(new { status = "No record", value = 0 }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        // GET: /Order/ActionOrder?productvalue=xxx
        [HttpGet]
        public ActionResult ActionOrder(string productvalue)
        {
            using (var db = new PortalDbContext())
            {
                try
                {
                    string sql = @"select e.NAME FROM INVENTTABLE IT
                                   inner join ECORESPRODUCTTRANSLATION as e on e.PRODUCT = IT.PRODUCT
                                   WHERE IT.DATAAREAID = 'TGPL' AND IT.ITEMID = @p0";
                    string name = db.Database.SqlQuery<string>(sql, productvalue).FirstOrDefault();
                    return Content(name ?? "");
                }
                catch
                {
                    return Content("");
                }
            }
        }

        // GET: /Order/ActionCancelOrder?orderPrefix=xxx
        [HttpGet]
        public ActionResult ActionCancelOrder(string orderPrefix)
        {
            using (var db = new PortalDbContext())
            {
                try
                {
                    string sql = @"update CUSTORDERTABLE set CANCEL_FLAG = 1, CANCELDATETIME = GETDATE() 
                                   where ORDERPREFIXID = @p0 and (RELEASED_FLAG = 0 or RELEASED_FLAG is null) 
                                   and (SHIPPED_FLAG = 0 or SHIPPED_FLAG is null)";
                    int affected = db.Database.ExecuteSqlCommand(sql, orderPrefix);
                    return Json(new { status = affected > 0 ? "success" : "failed", message = affected > 0 ? "record updated" : "failed" }, JsonRequestBehavior.AllowGet);
                }
                catch
                {
                    return Json(new { status = "failed", message = "failed" }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        // POST: /Order/ActionReceivedOrder
        [HttpPost]
        public ActionResult ActionReceivedOrder(string orderPrefix, string mmsh, string lssh)
        {
            using (var db = new PortalDbContext())
            {
                try
                {
                    string sql = @"update CUSTORDERTABLE set RECEIVED_FLAG = 1, RECEIVEDDATETIME = GETDATE(),
                                   SHIPPED_FLAG = 0, SHORTAGE_MM = @p1, SHORTAGE_LTRS = @p2
                                   where ORDERPREFIXID = @p0";
                    int affected = db.Database.ExecuteSqlCommand(sql, orderPrefix, mmsh ?? "", lssh ?? "");
                    return Json(new { status = "success", message = "record updated", order = orderPrefix, mm = mmsh, ls = lssh });
                }
                catch
                {
                    return Json(new { status = "failed", message = "failed", order = orderPrefix, mm = mmsh, ls = lssh });
                }
            }
        }

        // POST: /Order/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancel(string orderPrefixId)
        {
            if (string.IsNullOrEmpty(orderPrefixId))
            {
                return Json(new { success = false, message = "Invalid order prefix." });
            }

            bool result = _orderService.CancelOrder(orderPrefixId, UserSession.UserId);
            if (result)
            {
                return Json(new { success = true, message = "Order cancelled successfully." });
            }

            return Json(new { success = false, message = "Order could not be cancelled." });
        }

        // POST: /Order/Receive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Receive(string orderPrefixId)
        {
            if (string.IsNullOrEmpty(orderPrefixId))
            {
                return Json(new { success = false, message = "Invalid order prefix." });
            }

            bool result = _orderService.ConfirmReceivedOrder(orderPrefixId, UserSession.UserId);
            if (result)
            {
                return Json(new { success = true, message = "Order receipt confirmed successfully." });
            }

            return Json(new { success = false, message = "Failed to confirm order receipt." });
        }

        // GET: /Order/GetProductName?productvalue=xxx
        [HttpGet]
        public ActionResult GetProductName(string productvalue)
        {
            if (string.IsNullOrWhiteSpace(productvalue))
            {
                return Content("");
            }

            string code = productvalue.Trim().ToUpperInvariant();
            switch (code)
            {
                case "HSD": return Content("High Speed Diesel");
                case "PMG": return Content("Premier Motor Gasoline");
                case "HOBC": return Content("High Octane Blended Component");
                default:
                    using (var db = new PortalDbContext())
                    {
                        try
                        {
                            string sql = @"select top 1 ec.NAME from INVENTTABLE as i
                                           inner join ECORESPRODUCTTRANSLATION as ec on ec.PRODUCT = i.PRODUCT
                                           where i.ITEMID = @p0 and i.DATAAREAID = 'aml'";
                            var name = db.Database.SqlQuery<string>(sql, code).FirstOrDefault();
                            if (!string.IsNullOrEmpty(name))
                            {
                                return Content(name);
                            }
                        }
                        catch { }
                    }
                    return Content(productvalue);
            }
        }
    }
}
