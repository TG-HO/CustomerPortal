using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPortal_MVC_.Models
{
    [Table("CUSTORDERTABLE")]
    public class CustOrderTable
    {
        [Key]
        [Column("RECID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long RecId { get; set; }

        [Column("ORDERID")]
        public int OrderId { get; set; }

        [Column("ORDERPREFIXID")]
        [StringLength(50)]
        public string OrderPrefixId { get; set; }

        [Column("ORDERCREATEDUSER")]
        [StringLength(50)]
        public string OrderCreatedUser { get; set; }

        [Column("PRODUCTCODE")]
        [StringLength(50)]
        public string ProductCode { get; set; }

        [Column("PRODUCTNAME")]
        [StringLength(100)]
        public string ProductName { get; set; }

        [Column("REQUIREDQUANTITY")]
        public int RequiredQuantity { get; set; }

        [Column("HOLDFREEQTY")]
        public int HoldFreeQty { get; set; }

        [Column("HOLDSFREEBALANCE")]
        public int HoldsFreeBalance { get; set; }

        [Column("PENDING_QTY")]
        public int? PendingQty { get; set; }

        [Column("RELEASEQTY")]
        public int? ReleaseQty { get; set; }

        [Column("HOLDSFREE_FLAG")]
        public int? HoldsFreeFlag { get; set; }

        [Column("PARTIALHF_FLAG")]
        public int? PartialHfFlag { get; set; }

        [Column("PENDING_FLAG")]
        public int? PendingFlag { get; set; }

        [Column("RELEASED_FLAG")]
        public int? ReleasedFlag { get; set; }

        [Column("SHIPPED_FLAG")]
        public int? ShippedFlag { get; set; }

        [Column("CANCEL_FLAG")]
        public int? CancelFlag { get; set; }

        [Column("RECEIVED_FLAG")]
        public int? ReceivedFlag { get; set; }

        [Column("PROF_UPLOAD_FLAG")]
        public int? ProfUploadFlag { get; set; }

        [Column("PROF_UPLOAD")]
        [StringLength(255)]
        public string ProfUpload { get; set; }

        [Column("CARRIERCODE")]
        [StringLength(50)]
        public string CarrierCode { get; set; }

        [Column("SITE")]
        [StringLength(50)]
        public string Site { get; set; }

        [Column("ORDERCREATEDON")]
        public DateTime? OrderCreatedOn { get; set; }

        [Column("ORDERCREATEDONDATETIME")]
        public DateTime? OrderCreatedOnDateTime { get; set; }

        [Column("HOLDSFREEDATE")]
        public DateTime? HoldsFreeDate { get; set; }

        [Column("HOLDSFREEDATETIME")]
        public DateTime? HoldsFreeDateTime { get; set; }

        [Column("SCHEDULEFOR")]
        public DateTime? ScheduleFor { get; set; }

        [Column("APPROVEDDATE")]
        public DateTime? ApprovedDate { get; set; }

        [Column("APPROVEDSTATUS")]
        public int? ApprovedStatus { get; set; }

        [Column("APPROVEDBY")]
        [StringLength(50)]
        public string ApprovedBy { get; set; }

        [Column("SITENAME")]
        [StringLength(100)]
        public string SiteName { get; set; }

        [Column("RECEIVEDDATETIME")]
        public DateTime? ReceivedDateTime { get; set; }

        [Column("CANCELDATETIME")]
        public DateTime? CancelDateTime { get; set; }

        [Column("UNIT")]
        [StringLength(10)]
        public string Unit { get; set; }

        [Column("DATAAREAID")]
        [StringLength(10)]
        public string DataAreaId { get; set; }

        [NotMapped]
        public string ProfDocument
        {
            get { return ProfUpload; }
            set { ProfUpload = value; }
        }

        // Navigation property
        [ForeignKey("OrderCreatedUser")]
        public virtual CustTable Customer { get; set; }
    }
}
