using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPortal_MVC_.Models
{
    [Table("LUBRICANT_ORDERS_FINAL")]
    public class LubricantOrderFinal
    {
        [Key]
        [Column("LINENUMBER")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long LineNumber { get; set; }

        [Column("ORDERHEAD_REF")]
        [StringLength(50)]
        public string OrderHeadRef { get; set; }

        [Column("CUSTSITE")]
        [StringLength(50)]
        public string CustSite { get; set; }

        [Column("SITENAME")]
        [StringLength(50)]
        public string SiteName { get; set; }

        [Column("CUSTOMERCODE")]
        [StringLength(50)]
        public string CustomerCode { get; set; }

        [Column("PRODUCTBRAND")]
        [StringLength(50)]
        public string ProductBrand { get; set; }

        [Column("PRODUCTCATEGORY")]
        [StringLength(50)]
        public string ProductCategory { get; set; }

        [Column("PRODUCTCODE")]
        [StringLength(50)]
        public string ProductCode { get; set; }

        [Column("PRODUCTNAME")]
        [StringLength(50)]
        public string ProductName { get; set; }

        [Column("PRODUCTRATES")]
        public double? ProductRates { get; set; }

        [Column("UNIT")]
        [StringLength(50)]
        public string Unit { get; set; }

        [Column("REQUIREDQTY")]
        public int? RequiredQty { get; set; }

        [Column("TOTALAMOUNT")]
        public double? TotalAmount { get; set; }

        [Column("DATAAREAID")]
        [StringLength(50)]
        public string DataAreaId { get; set; }

        [Column("CREATEDBY")]
        [StringLength(50)]
        public string CreatedBy { get; set; }

        [Column("CREATEDON")]
        public DateTime? CreatedOn { get; set; }

        [Column("CREATEDONDATETIME")]
        public DateTime? CreatedOnDateTime { get; set; }

        [Column("ORDERSTATUS")]
        [StringLength(50)]
        public string OrderStatus { get; set; }

        [Column("STATUS_FLAG")]
        public int? StatusFlag { get; set; }

        [Column("CANCEL_FLAG")]
        public int? CancelFlag { get; set; }

        [Column("CANCELDATETIME")]
        public DateTime? CancelDateTime { get; set; }

        [Column("CANCELLED_BY")]
        [StringLength(50)]
        public string CancelledBy { get; set; }

        [Column("UPLOADED_DATETIME")]
        public DateTime? UploadedDateTime { get; set; }

        [Column("UPLOADED_BY")]
        [StringLength(50)]
        public string UploadedBy { get; set; }

        [Column("SITE")]
        [StringLength(50)]
        public string Site { get; set; }

        [Column("WAREHOUSE")]
        [StringLength(50)]
        public string Warehouse { get; set; }

        [Column("APPROVEDQTY")]
        public int? ApprovedQty { get; set; }

        [Column("BALANCEQTY")]
        public int? BalanceQty { get; set; }

        [Column("isnew")]
        public int IsNew { get; set; }
    }
}
