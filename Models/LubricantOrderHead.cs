using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPortal_MVC_.Models
{
    [Table("LUBRICANT_ORDERS_HEAD")]
    public class LubricantOrderHead
    {
        [Key]
        [Column("ORDERID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }

        [Column("ORDERPREFIX")]
        [StringLength(50)]
        public string OrderPrefix { get; set; }

        [Column("ORDERNUMBER")]
        [StringLength(60)]
        public string OrderNumber { get; set; }

        [Column("ORDER_STATUS")]
        [StringLength(50)]
        public string OrderStatus { get; set; }

        [Column("CUSTOMERCODE")]
        [StringLength(50)]
        public string CustomerCode { get; set; }

        [Column("CUSTSITE")]
        [StringLength(50)]
        public string CustSite { get; set; }

        [Column("CREATEDDATE")]
        public DateTime? CreatedDate { get; set; }

        [Column("CREATEDDATETIME")]
        public DateTime? CreatedDateTime { get; set; }

        [Column("STATUS_FLAG")]
        public int? StatusFlag { get; set; }

        [Column("DATAAREAID")]
        [StringLength(50)]
        public string DataAreaId { get; set; }

        [Column("CANCEL_FLAG")]
        public int? CancelFlag { get; set; }

        [Column("FINAL_STATUS")]
        public int? FinalStatus { get; set; }

        [Column("SITE")]
        [StringLength(50)]
        public string Site { get; set; }

        [Column("WAREHOUSE")]
        [StringLength(50)]
        public string Warehouse { get; set; }

        [Column("isnew")]
        public int IsNew { get; set; }

        [NotMapped]
        public int? TotalQty { get; set; }

        [NotMapped]
        public decimal? GrandTotal { get; set; }

        [NotMapped]
        public DateTime? OrderDate => CreatedDateTime ?? CreatedDate;
    }
}
