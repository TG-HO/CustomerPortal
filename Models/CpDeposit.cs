using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPortal_MVC_.Models
{
    [Table("cp_deposit")]
    public class CpDeposit
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("bank")]
        [StringLength(100)]
        public string Bank { get; set; }

        [Column("ds_date")]
        public DateTime? DsDate { get; set; }

        [Column("description")]
        [StringLength(255)]
        public string Description { get; set; }

        [Column("ds_upload")]
        [StringLength(255)]
        public string DsUpload { get; set; }

        [Column("account_code")]
        [StringLength(50)]
        public string AccountCode { get; set; }

        // Navigation property
        [ForeignKey("AccountCode")]
        public virtual CustTable Customer { get; set; }
    }
}
