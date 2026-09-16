using System;
using System.ComponentModel.DataAnnotations;

namespace CustomerPortal_MVC_.Models.ViewModels
{
    public class OrderCreateViewModel
    {
        [Required(ErrorMessage = "Product code is required.")]
        [Display(Name = "Product Code")]
        public string ProductCode { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, 100000, ErrorMessage = "Quantity must be greater than 0.")]
        [Display(Name = "Order Quantity (Liters)")]
        public decimal NewOrderQuantity { get; set; }

        [Required(ErrorMessage = "Site location is required.")]
        [Display(Name = "Delivery Site")]
        public string Site { get; set; }

        public decimal AvailableCredit { get; set; }
        public decimal TankCapacity { get; set; }
        public decimal UnitRate { get; set; }
    }

    public class OrderApproveViewModel
    {
        [Required]
        public string OrderPrefixId { get; set; }

        [Required]
        public string CustomerId { get; set; }

        [Display(Name = "Release Quantity")]
        [Range(0, 100000)]
        public decimal ReleaseQty { get; set; }

        [Display(Name = "Holds Free Quantity")]
        [Range(0, 100000)]
        public decimal HoldFreeQty { get; set; }

        public decimal RequiredQuantity { get; set; }
        public decimal HoldsFreeBalance { get; set; }
        public string ActionType { get; set; }

        public string TankLorry { get; set; }
        public string SiteInduction { get; set; }
        public string WarehouseInduction { get; set; }
        public string LocationInduction { get; set; }
    }

    public class OrderMergeViewModel
    {
        [Required(ErrorMessage = "First order is required.")]
        public string Order1PrefixId { get; set; }

        [Required(ErrorMessage = "Second order is required.")]
        public string Order2PrefixId { get; set; }

        [Display(Name = "Merged Total Quantity")]
        public decimal MergedQty { get; set; }
    }
}
