using System;
using System.IO;
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

        #region Portal Page Actions

        // GET: /Lubricants/Home or /Lubricants
        public ActionResult Home()
        {
            ViewBag.Title = "Taj Gasoline - Lubricants";
            ViewBag.ActiveLubeMenu = "Home";
            return View("Home");
        }

        // GET: /Lubricants/Index
        public ActionResult Index()
        {
            return RedirectToAction("Home");
        }

        // GET: /Lubricants/NewLubeOrder
        public ActionResult NewLubeOrder()
        {
            ViewBag.Title = "Taj Gasoline - New Lubricant Order";
            ViewBag.ActiveLubeMenu = "NewLubeOrder";
            return View("NewLubeOrder");
        }

        // GET: /Lubricants/NewOrder
        public ActionResult NewOrder()
        {
            return RedirectToAction("NewLubeOrder");
        }

        // GET: /Lubricants/SubmittedOrders
        public ActionResult SubmittedOrders()
        {
            ViewBag.Title = "Taj Gasoline - Submitted Orders";
            ViewBag.ActiveLubeMenu = "SubmittedOrders";
            return View("SubmittedOrders");
        }

        // GET: /Lubricants/DownloadPriceList?fileName=Mastersheet.pdf
        public ActionResult DownloadPriceList(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return HttpNotFound();
            }

            string cleanFileName = Path.GetFileName(fileName);
            string filePath = Server.MapPath("~/Content/lubricants/PriceList/" + cleanFileName);

            if (!System.IO.File.Exists(filePath))
            {
                return HttpNotFound("Price list file not found.");
            }

            string ext = Path.GetExtension(cleanFileName).ToLowerInvariant();
            string contentType = "application/octet-stream";
            if (ext == ".pdf") contentType = "application/pdf";
            else if (ext == ".xlsx") contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            else if (ext == ".xls") contentType = "application/vnd.ms-excel";

            return File(filePath, contentType, cleanFileName);
        }

        #endregion

        #region Portal API Actions (Pure MVC routing)

        // GET/POST: /Lubricants/ApiHome
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult ApiHome()
        {
            var stats = _lubeService.GetHomeStats(UserSession.UserId);
            return Json(stats, JsonRequestBehavior.AllowGet);
        }

        // GET/POST: /Lubricants/ApiNewOrderHead
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult ApiNewOrderHead()
        {
            string customerCode = UserSession.UserId;
            string custSite = UserSession.CustomerName ?? UserSession.UserId;

            bool success = _lubeService.CreateNewOrderHead(customerCode, custSite, out int newOrderId, out string error);
            if (success)
            {
                return Json(new { status = "success", message = "record updated", orderId = newOrderId }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { status = "failed", message = error ?? "Failed to create order head" }, JsonRequestBehavior.AllowGet);
        }

        // POST: /Lubricants/ApiDeleteOrderHead
        [HttpPost]
        public ActionResult ApiDeleteOrderHead(string orderHeadRef)
        {
            if (string.IsNullOrEmpty(orderHeadRef) || !int.TryParse(orderHeadRef.Replace("TLB-", "").Trim(), out int orderId))
            {
                return Json(new { status = "failed", message = "Invalid order reference", orderlines = "" });
            }

            bool success = _lubeService.DeleteOrderHead(orderId, out bool hasLines, out string error);
            if (hasLines)
            {
                return Json(new { status = "", message = "", orderlines = "exist" });
            }

            if (success)
            {
                return Json(new { status = "success", message = "record updated", orderlines = "" });
            }

            return Json(new { status = "failed", message = error ?? "Could not delete order head", orderlines = "" });
        }

        // GET/POST: /Lubricants/ApiGetBrands
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult ApiGetBrands()
        {
            var res = _lubeService.GetBrandsAndCategoriesHtml();
            return Json(new { Brands = res.BrandsHtml, Categories = res.CategoriesHtml }, JsonRequestBehavior.AllowGet);
        }

        // POST: /Lubricants/ApiGetFilteredProducts
        [HttpPost]
        public ActionResult ApiGetFilteredProducts(int? Brand, int? Category)
        {
            string html = _lubeService.GetFilteredProductsHtml(Brand ?? 0, Category ?? 0);
            return Json(new { status = "success", result = html });
        }

        // POST: /Lubricants/ApiGetPrice
        [HttpPost]
        public ActionResult ApiGetPrice(string Products)
        {
            var res = _lubeService.GetProductPriceAndUnit(Products);
            return Json(new
            {
                status = "success",
                ProductsPrice = res.Price,
                ProductsName = res.ProductName,
                Unit = res.Unit,
                result = ""
            });
        }

        // POST: /Lubricants/ApiActionNewLubeOrder
        [HttpPost]
        public ActionResult ApiActionNewLubeOrder(string siteName, string productname, string product,
                                                 int? prod_brand, int? prod_category, double? rates,
                                                 string unit, int? qty, string orderRef)
        {
            string customerCode = UserSession.UserId;
            string custSite = siteName ?? UserSession.CustomerName ?? customerCode;

            bool success = _lubeService.AddOrderLine(
                siteName: siteName,
                custSite: custSite,
                customerCode: customerCode,
                prodBrand: prod_brand ?? 0,
                prodCategory: prod_category ?? 0,
                productCode: product,
                productName: productname,
                rates: rates ?? 0,
                unit: unit,
                qty: qty ?? 0,
                orderRef: orderRef,
                error: out string error);

            if (success)
            {
                return Json(new { status = "success", message = "record updated" });
            }

            return Json(new { status = "failed", message = error ?? "Failed to add lubricant line" });
        }

        // GET/POST: /Lubricants/ApiTblNewLubeOrder
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult ApiTblNewLubeOrder()
        {
            var data = _lubeService.GetActiveOrderHeadsData(UserSession.UserId);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        // POST: /Lubricants/ApiTblOrderDetails
        [HttpPost]
        public ActionResult ApiTblOrderDetails(string ORDERNUMBERss, string ORDERNUMBER)
        {
            string ordNum = !string.IsNullOrEmpty(ORDERNUMBERss) ? ORDERNUMBERss : ORDERNUMBER;
            var res = _lubeService.GetOrderDetailsData(ordNum);

            return Json(new
            {
                Tabledata = res.TableHtml,
                status = "success",
                result = "success",
                message = "query success",
                data = res.Data
            });
        }

        // POST: /Lubricants/ApiActionFinalOrder
        [HttpPost]
        public ActionResult ApiActionFinalOrder(string orderHead)
        {
            bool success = _lubeService.FinalizeOrder(orderHead, UserSession.UserId, out string error);
            if (success)
            {
                return Json(new { status = "success", message = "All queries ran successfully" });
            }
            return Json(new { status = "failed", message = error ?? "Failed to finalize lubricant order" });
        }

        // POST: /Lubricants/ApiDeleteLubeOrders3
        [HttpPost]
        public ActionResult ApiDeleteLubeOrders3(long id)
        {
            bool success = _lubeService.CancelTempOrderLine(id, UserSession.UserId, out string error);
            if (success)
            {
                return Json(new { status = "success", message = "record updated" });
            }
            return Json(new { status = "failed", message = error ?? "Failed to delete line" });
        }

        // GET/POST: /Lubricants/ApiTblSubmittedOrders
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult ApiTblSubmittedOrders()
        {
            var data = _lubeService.GetSubmittedOrdersData(UserSession.UserId);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        // POST: /Lubricants/ApiDeleteLubeOrders
        [HttpPost]
        public ActionResult ApiDeleteLubeOrders(long id)
        {
            bool success = _lubeService.CancelSubmittedOrderLine(id, UserSession.UserId, out string error);
            if (success)
            {
                return Json(new { status = "success", message = "record updated" });
            }
            return Json(new { status = "failed", message = error ?? "Failed to delete line" });
        }

        #endregion

        #region Admin Section Actions

        // GET: /Lubricants/AdminSubmittedOrders
        [CustomAuthorize(Roles = "Admin")]
        public ActionResult AdminSubmittedOrders()
        {
            ViewBag.ActiveMenu = "LubeAdmin";
            var finalOrders = _lubeService.AdminGetSubmittedLubeOrders();
            return View("AdminSubmittedOrders", finalOrders);
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

        #endregion
    }
}
