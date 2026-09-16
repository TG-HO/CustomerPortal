using System;
using System.Web.Mvc;
using CustomerPortal_MVC_.Filters;
using CustomerPortal_MVC_.Helpers;
using CustomerPortal_MVC_.Models.ViewModels;
using CustomerPortal_MVC_.Services;

namespace CustomerPortal_MVC_.Controllers
{
    [CustomAuthorize(Roles = "Customer,Admin")]
    public class DepositController : Controller
    {
        private readonly ICustomerService _customerService;

        public DepositController()
        {
            _customerService = new CustomerService();
        }

        public DepositController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        // GET: /Deposit/Index
        public ActionResult Index()
        {
            return RedirectToAction("Upload");
        }

        // GET: /Deposit/Upload
        public ActionResult Upload()
        {
            var model = new DepositUploadViewModel
            {
                DsDate = DateTime.Now
            };

            return View(model);
        }

        // POST: /Deposit/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Upload(DepositUploadViewModel model)
        {
            string customerId = UserSession.UserId;

            if (model.NumericAmount <= 0)
            {
                ModelState.AddModelError("Amount", "Please enter a valid deposit amount greater than zero.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool success = _customerService.SubmitDepositSlip(model, customerId, Server);
            if (success)
            {
                TempData["SuccessMessage"] = "Deposit slip uploaded successfully!";
                return RedirectToAction("Upload");
            }

            ModelState.AddModelError("", "File upload failed. Ensure file size is under 400KB and format is PDF, PNG, or JPG.");
            return View(model);
        }

        // GET: /Deposit/Ledger
        public ActionResult Ledger()
        {
            string customerId = UserSession.UserId;
            var summary = _customerService.GetDashboardSummary(customerId);
            var payments = _customerService.GetCustomerPayments(customerId);

            ViewBag.Summary = summary;
            return View(payments);
        }

        // GET: /Deposit/TransactionHistory
        public ActionResult TransactionHistory()
        {
            string customerId = UserSession.UserId;
            var payments = _customerService.GetCustomerPayments(customerId);
            return View(payments);
        }
    }
}
