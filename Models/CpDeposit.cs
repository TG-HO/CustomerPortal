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
        [StringLength(50)]
        public string Amount { get; set; }

        [NotMapped]
        public decimal NumericAmount
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Amount)) return 0m;
                string clean = Amount.Replace(",", "").Trim();
                return decimal.TryParse(clean, out decimal d) ? d : 0m;
            }
            set
            {
                Amount = value.ToString("N0");
            }
        }

        [Column("bank")]
        [StringLength(100)]
        public string Bank { get; set; }

        [NotMapped]
        public string DsBank => Bank;

        [Column("ds_date")]
        public DateTime? DsDate { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [NotMapped]
        public string DsDesc => Description;

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
