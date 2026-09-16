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
    public class HomeController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly IOrderService _orderService;

        public HomeController()
        {
            _customerService = new CustomerService();
            _orderService = new OrderService();
        }

        public HomeController(ICustomerService customerService, IOrderService orderService)
        {
            _customerService = customerService;
            _orderService = orderService;
        }

        // GET: /Home/Index
        public ActionResult Index()
        {
            string customerId = UserSession.UserId;
            var summary = _customerService.GetDashboardSummary(customerId);
            return View(summary);
        }

        // GET: /Home/OpenOrders
        public ActionResult OpenOrders()
        {
            string customerId = UserSession.UserId;
            var orders = _orderService.GetOpenOrders(customerId);
            return View(orders);
        }

        // GET: /Home/OpenOrderDetail?op=TG-20260915-0001
        public ActionResult OpenOrderDetail(string op)
        {
            if (string.IsNullOrEmpty(op)) return RedirectToAction("OpenOrders");

            var order = _orderService.GetOrderDetail(op);
            if (order == null || order.OrderCreatedUser != UserSession.UserId)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // GET: /Home/InvoicedOrders
        public ActionResult InvoicedOrders()
        {
            string customerId = UserSession.UserId;
            var orders = _orderService.GetInvoicedOrders(customerId);
            return View(orders);
        }

        // GET: /Home/InvoiceOrderDetail?op=TG-20260915-0001
        public ActionResult InvoiceOrderDetail(string op)
        {
            if (string.IsNullOrEmpty(op)) return RedirectToAction("InvoicedOrders");

            var order = _orderService.GetOrderDetail(op);
            if (order == null || order.OrderCreatedUser != UserSession.UserId)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        // GET: /Home/PendingOrders
        public ActionResult PendingOrders()
        {
            string customerId = UserSession.UserId;
            var orders = _orderService.GetPendingOrders(customerId);
            return View(orders);
        }

        // GET: /Home/OrderStatus
        public ActionResult OrderStatus()
        {
            string customerId = UserSession.UserId;
            var orders = _orderService.GetOpenOrders(customerId);
            return View(orders);
        }

        // GET: /Home/HfBalanceDetails?prodCode=01
        public ActionResult HfBalanceDetails(string prodCode)
        {
            string customerId = UserSession.UserId;
            string code = string.IsNullOrEmpty(prodCode) ? "01" : prodCode;
            ViewBag.ProductCode = code;
            ViewBag.ProductName = code == "01" ? "High Speed Diesel" : (code == "02" ? "Premier Motor Gasoline" : "HOBC");

            using (var db = new PortalDbContext())
            {
                var orders = db.CustOrders
                    .Where(o => o.OrderCreatedUser == customerId && o.ProductCode == code && o.HoldFreeQty > 0 && o.HoldsFreeBalance > 0)
                    .OrderByDescending(o => o.OrderId)
                    .ToList();

                return View(orders);
            }
        }

        // GET: /Home/TrainingVideos
        public ActionResult TrainingVideos()
        {
            return View();
        }

        // GET: /Home/AllOrders
        public ActionResult AllOrders()
        {
            string customerId = UserSession.UserId;
            var orders = _orderService.GetAllOrders(customerId);
            return View(orders);
        }

        // GET: /Home/CustomerTransaction
        public ActionResult CustomerTransaction()
        {
            string customerId = UserSession.UserId;
            var summary = _customerService.GetDashboardSummary(customerId);
            var transactions = _customerService.GetCustomerTransactions(customerId);
            var model = new CustomerTransactionPageViewModel
            {
                CustomerBalance = summary.CurrentBalance,
                CreditLimit = summary.CreditLimit,
                AvailableCreditLimit = summary.AvailableCreditLimit,
                Transactions = transactions
            };
            return View(model);
        }

        // GET: /Home/GetBalanceDetails
        [HttpGet]
        public ActionResult GetBalanceDetails()
        {
            string customerId = UserSession.UserId;
            var summary = _customerService.GetDashboardSummary(customerId);
            return Json(new
            {
                Balance = summary.CurrentBalance.ToString("N0"),
                CreditLimit = summary.CreditLimit.ToString("N0"),
                AvailableCreditLimit = summary.AvailableCreditLimit.ToString("N0")
            }, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult WebManifest()
        {
            string path = Server.MapPath("~/site.webmanifest");
            if (System.IO.File.Exists(path))
            {
                return File(path, "application/manifest+json");
            }
            return HttpNotFound();
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Favicon32()
        {
            string path = Server.MapPath("~/favicon-32x32.png");
            if (System.IO.File.Exists(path))
            {
                return File(path, "image/png");
            }
            path = Server.MapPath("~/Content/icon/favicon-32x32.png");
            if (System.IO.File.Exists(path))
            {
                return File(path, "image/png");
            }
            return HttpNotFound();
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Favicon16()
        {
            string path = Server.MapPath("~/favicon-16x16.png");
            if (System.IO.File.Exists(path))
            {
                return File(path, "image/png");
            }
            path = Server.MapPath("~/Content/icon/favicon-16x16.png");
            if (System.IO.File.Exists(path))
            {
                return File(path, "image/png");
            }
            return HttpNotFound();
        }
    }
}
