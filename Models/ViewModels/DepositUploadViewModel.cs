using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace CustomerPortal_MVC_.Models.ViewModels
{
    public class DepositUploadViewModel
    {
        [Required(ErrorMessage = "Deposit amount is required.")]
        [Display(Name = "Deposit Amount (PKR)")]
        public string Amount { get; set; }

        public decimal NumericAmount
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Amount)) return 0m;
                string clean = Amount.Replace(",", "").Trim();
                return decimal.TryParse(clean, out decimal d) ? d : 0m;
            }
        }

        [Required(ErrorMessage = "Bank name is required.")]
        [StringLength(100)]
        [Display(Name = "Bank Name")]
        public string Bank { get; set; }

        [Required(ErrorMessage = "Deposit date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Deposit Date")]
        public DateTime DsDate { get; set; }

        [StringLength(255)]
        [Display(Name = "Description / Remarks")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Please select a deposit slip file.")]
        [Display(Name = "Deposit Slip File (JPG, PNG, PDF max 400KB)")]
        public HttpPostedFileBase DsUpload { get; set; }
    }

    public class ProformaUploadViewModel
    {
        [Required]
        public string OrderPrefixId { get; set; }

        [Required]
        public string CustomerId { get; set; }

        [Required(ErrorMessage = "Please select a proforma invoice file.")]
        [Display(Name = "Proforma Invoice File (JPG, PNG, PDF max 400KB)")]
        public HttpPostedFileBase ProformaFile { get; set; }
    }

    public class LubeOrderCreateViewModel
    {
        [Required(ErrorMessage = "Brand selection is required.")]
        public int BrandId { get; set; }

        [Required(ErrorMessage = "Product selection is required.")]
        public string ProductCode { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, 10000)]
        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }
        public string ProductName { get; set; }
    }

    public class DashboardSummaryViewModel
    {
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerSite { get; set; }
        public int TotalOrdersCount { get; set; }
        public int OpenOrdersCount { get; set; }
        public int PendingOrdersCount { get; set; }
        public int InvoicedOrdersCount { get; set; }
        public decimal TotalHoldsFreeBalance { get; set; }
        public decimal AvailableCreditLimit { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal AmountToBeDelivered { get; set; }
        public decimal DealerRatesHsd { get; set; }
        public decimal DealerRatesPmg { get; set; }
        public decimal DealerRatesHobc { get; set; }

        // Pipeline Order lists for interactive hover on Home Dashboard
        public List<CustOrderTable> PendingOrdersList { get; set; } = new List<CustOrderTable>();
        public List<CustOrderTable> HoldsFreeOrdersList { get; set; } = new List<CustOrderTable>();
        public List<CustOrderTable> ScheduledOrdersList { get; set; } = new List<CustOrderTable>();
        public List<CustOrderTable> ShippedOrdersList { get; set; } = new List<CustOrderTable>();
        public List<CustOrderTable> DeliveredOrdersList { get; set; } = new List<CustOrderTable>();
    }
}
