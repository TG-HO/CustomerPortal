using System;
using System.Linq;
using System.Web.Mvc;
using CustomerPortal_MVC_.Filters;
using CustomerPortal_MVC_.Helpers;
using CustomerPortal_MVC_.Models.ViewModels;
using CustomerPortal_MVC_.Services;

namespace CustomerPortal_MVC_.Controllers
{
    [CustomAuthorize]
    public class LubricantsController : Controller
    {
        private readonly ILubricantService _lubeService;

        public LubricantsController()
        {
            _lubeService = new LubricantService();
        }

        public LubricantsController(ILubricantService lubeService)
        {
            _lubeService = lubeService;
        }

        // GET: /Lubricants/Index or /Lubricants/Home
        public ActionResult Index()
        {
            string customerId = UserSession.UserId;
            var orders = _lubeService.GetSubmittedLubeOrders(customerId);
            return View(orders ?? new System.Collections.Generic.List<CustomerPortal_MVC_.Models.LubricantOrderHead>());
        }

        // GET: /Lubricants/NewOrder
        [CustomAuthorize(Roles = "Customer,Admin")]
        public ActionResult NewOrder()
        {
            ViewBag.Brands = new SelectList(_lubeService.GetBrands(), "BrandId", "BrandName");
            return View(new LubeOrderCreateViewModel());
        }

        // POST: /Lubricants/NewOrder
        [HttpPost]
        [CustomAuthorize(Roles = "Customer,Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult NewOrder(LubeOrderCreateViewModel model)
        {
            string customerId = UserSession.UserId;

            if (!ModelState.IsValid)
            {
                ViewBag.Brands = new SelectList(_lubeService.GetBrands(), "BrandId", "BrandName", model.BrandId);
                return View(model);
            }

            try
            {
                var headOrder = _lubeService.CreateLubeOrder(model, customerId);
                _lubeService.FinalizeLubeOrder(headOrder.OrderId, customerId);

                TempData["SuccessMessage"] = "Lubricant order submitted successfully!";
                return RedirectToAction("SubmittedOrders");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error creating lubricant order: " + ex.Message);
                ViewBag.Brands = new SelectList(_lubeService.GetBrands(), "BrandId", "BrandName", model.BrandId);
                return View(model);
            }
        }

        // GET: /Lubricants/SubmittedOrders
        [CustomAuthorize(Roles = "Customer,Admin")]
        public ActionResult SubmittedOrders()
        {
            string customerId = UserSession.UserId;
            var orders = _lubeService.GetSubmittedLubeOrders(customerId);
            return View(orders ?? new System.Collections.Generic.List<CustomerPortal_MVC_.Models.LubricantOrderHead>());
        }

        // GET: /Lubricants/GetProductsByBrand?brandId=1
        [HttpGet]
        public ActionResult GetProductsByBrand(int brandId)
        {
            var products = _lubeService.GetProductsByBrand(brandId)
                .Select(p => new { productCode = p.ProductCode, productName = p.ProductName, price = p.Price });

            return Json(products, JsonRequestBehavior.AllowGet);
        }

        // GET: /Lubricants/GetPrice?productCode=LUBE-001
        [HttpGet]
        public ActionResult GetPrice(string productCode)
        {
            decimal price = _lubeService.GetProductPrice(productCode, UserSession.UserId);
            return Json(new { price = price }, JsonRequestBehavior.AllowGet);
        }

        // GET: /Lubricants/AdminSubmittedOrders
        [CustomAuthorize(Roles = "Admin")]
        public ActionResult AdminSubmittedOrders()
        {
            var finalOrders = _lubeService.AdminGetSubmittedLubeOrders();
            return View(finalOrders);
        }

        // POST: /Lubricants/AdminUpdateStatus
        [HttpPost]
        [CustomAuthorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult AdminUpdateStatus(long id, string status)
        {
            bool success = _lubeService.AdminUpdateLubeStatus(id, status, UserSession.UserId);
            if (success)
            {
                return Json(new { success = true, message = "Status updated to " + status });
            }
            return Json(new { success = false, message = "Failed to update status." });
        }

        // POST: /Lubricants/AdminCancel
        [HttpPost]
        [CustomAuthorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult AdminCancel(long id)
        {
            bool success = _lubeService.AdminCancelLubeOrder(id, UserSession.UserId);
            if (success)
            {
                return Json(new { success = true, message = "Lubricant order cancelled." });
            }
            return Json(new { success = false, message = "Failed to cancel lubricant order." });
        }

        // POST: /Lubricants/AdminEdit
        [HttpPost]
        [CustomAuthorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult AdminEdit(long id, decimal qty, decimal rate)
        {
            bool success = _lubeService.AdminEditLubeOrder(id, qty, rate, UserSession.UserId);
            if (success)
            {
                return Json(new { success = true, message = "Order updated successfully." });
            }
            return Json(new { success = false, message = "Failed to edit order." });
        }
    }
}
