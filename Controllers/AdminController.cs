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
    [CustomAuthorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly PortalDbContext _db;

        public AdminController()
        {
            _orderService = new OrderService();
            _db = new PortalDbContext();
        }

        public AdminController(IOrderService orderService, PortalDbContext db)
        {
            _orderService = orderService;
            _db = db;
        }

        // GET: /Admin/Index
        public ActionResult Index()
        {
            var pendingOrders = _db.CustOrders
                .Where(o => o.CancelFlag == 0 && o.ReleasedFlag == 0)
                .OrderByDescending(o => o.OrderCreatedOnDateTime)
                .ToList();

            return View(pendingOrders);
        }

        // GET: /Admin/Approve?op=TG-20260915-0001&cust=CUST-001
        public ActionResult Approve(string op, string cust)
        {
            if (string.IsNullOrEmpty(op)) return RedirectToAction("Index");

            var order = _orderService.GetOrderDetail(op);
            if (order == null) return HttpNotFound();

            var model = new OrderApproveViewModel
            {
                OrderPrefixId = order.OrderPrefixId,
                CustomerId = order.OrderCreatedUser,
                RequiredQuantity = order.RequiredQuantity,
                HoldsFreeBalance = order.HoldsFreeBalance,
                ReleaseQty = order.RequiredQuantity,
                HoldFreeQty = 0m
            };

            return View(model);
        }

        // POST: /Admin/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(OrderApproveViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool success = _orderService.ProcessAdminRelease(model, UserSession.UserId);
            if (success)
            {
                TempData["SuccessMessage"] = $"Order {model.OrderPrefixId} released successfully!";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Failed to process order release.");
            return View(model);
        }

        // GET: /Admin/CustomerOrders
        public ActionResult CustomerOrders()
        {
            var orders = _db.CustOrders
                .OrderByDescending(o => o.OrderCreatedOnDateTime)
                .Take(500)
                .ToList();

            return View(orders);
        }

        // GET: /Admin/CustomerPayments
        public ActionResult CustomerPayments()
        {
            var deposits = _db.Deposits
                .OrderByDescending(d => d.DsDate)
                .Take(500)
                .ToList();

            return View(deposits);
        }

        // GET: /Admin/MergeOrders
        public ActionResult MergeOrders()
        {
            var model = new OrderMergeViewModel();
            return View(model);
        }

        // POST: /Admin/MergeOrders
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MergeOrders(OrderMergeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool success = _orderService.MergeOrders(model, UserSession.UserId);
            if (success)
            {
                TempData["SuccessMessage"] = "Orders merged successfully!";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Failed to merge specified orders.");
            return View(model);
        }

        // GET: /Admin/CancelOrders
        public ActionResult CancelOrders()
        {
            var cancelledOrders = _db.CustOrders
                .Where(o => o.CancelFlag == 1)
                .OrderByDescending(o => o.CancelDateTime)
                .ToList();

            return View(cancelledOrders);
        }

        // GET: /Admin/RevertOrders
        public ActionResult RevertOrders()
        {
            var revertibleOrders = _db.CustOrders
                .Where(o => o.ReleasedFlag == 1 && o.ShippedFlag != 1)
                .OrderByDescending(o => o.ApprovedDate)
                .ToList();

            return View(revertibleOrders);
        }

        // POST: /Admin/RevertOrders
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RevertOrders(string orderPrefixId, decimal releaseQty)
        {
            bool success = _orderService.RevertOrder(orderPrefixId, releaseQty, UserSession.UserId);
            if (success)
            {
                TempData["SuccessMessage"] = $"Order {orderPrefixId} reverted to Holds Free.";
            }
            return RedirectToAction("RevertOrders");
        }

        // GET: /Admin/UploadProforma?op=TG-20260915-0001&cust=CUST-001
        public ActionResult UploadProforma(string op, string cust)
        {
            var model = new ProformaUploadViewModel
            {
                OrderPrefixId = op,
                CustomerId = cust
            };

            return View(model);
        }

        // POST: /Admin/UploadProforma
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadProforma(ProformaUploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool success = _orderService.UploadProforma(model, UserSession.UserId, Server);
            if (success)
            {
                TempData["SuccessMessage"] = "Proforma invoice uploaded successfully!";
                return RedirectToAction("CustomerOrders");
            }

            ModelState.AddModelError("", "File upload failed. Ensure file size is under 400KB and format is PDF, PNG, or JPG.");
            return View(model);
        }

        // POST: /Admin/EditTankLorry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditTankLorry(string orderPrefixId, string carrierCode)
        {
            bool success = _orderService.UpdateTankLorry(orderPrefixId, carrierCode, UserSession.UserId);
            if (success)
            {
                return Json(new { success = true, message = "Tank Lorry updated." });
            }

            return Json(new { success = false, message = "Failed to update Tank Lorry." });
        }

        // GET: /Admin/CheckDynRelease
        [HttpGet]
        public ActionResult CheckDynRelease(string prefix, decimal? tankcap, string cgroup, string customer, decimal? data1, string product)
        {
            if (tankcap.HasValue && tankcap.Value > 0 && data1.HasValue && data1.Value > tankcap.Value)
            {
                return Content($"{{\"status\":\"failed\",\"customer\":\"{customer}\"}}", "application/json");
            }

            return Content("{\"status\":\"success\"}", "application/json");
        }

        // GET: /Admin/ApproveTransaction
        [HttpGet]
        public ActionResult ApproveTransaction(decimal data1, decimal bal, string prefix, string tl, string siteInduction, string whInduction, string locInduction)
        {
            var model = new OrderApproveViewModel
            {
                OrderPrefixId = prefix,
                ReleaseQty = data1,
                HoldFreeQty = bal,
                TankLorry = tl,
                SiteInduction = siteInduction,
                WarehouseInduction = whInduction,
                LocationInduction = locInduction
            };

            bool success = _orderService.ProcessAdminRelease(model, UserSession.UserId);
            if (success)
            {
                return Content("{\"status\":\"success\"}", "application/json");
            }

            return Content("{\"status\":\"failed\"}", "application/json");
        }

        // GET: /Admin/CreateChildOrder
        [HttpGet]
        public ActionResult CreateChildOrder(string prefix, decimal? bal, string customer, string product)
        {
            return Content("{\"status\":\"success\"}", "application/json");
        }

        // GET: /Admin/GetCreditInfo
        [HttpGet]
        public ActionResult GetCreditInfo(string credit)
        {
            decimal balance = 0m;
            decimal creditLimit = 0m;
            decimal available = 0m;

            if (!string.IsNullOrEmpty(credit))
            {
                string custNum = credit.Trim();
                var customerService = new CustomerService();
                creditLimit = customerService.GetCreditLimit(custNum);
                balance = customerService.GetCustomerBalance(custNum);
                available = Math.Max(0m, creditLimit - balance);
            }

            string json = $"{{\"balance\":\"{balance:N2}\",\"credit\":\"{creditLimit:N2}\",\"available\":\"{available:N2}\"}}";
            return Content(json, "application/json");
        }

        // GET: /Admin/UpdateHoldsFreeQty
        [HttpGet]
        public ActionResult UpdateHoldsFreeQty(decimal data1, decimal bal, string prefix)
        {
            bool success = _orderService.UpdateHoldsFreeQty(prefix, bal, UserSession.UserId);
            if (success)
            {
                return Content("{\"status\":\"success\"}", "application/json");
            }

            return Content("{\"status\":\"failed\"}", "application/json");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
