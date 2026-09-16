using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPortal_MVC_.Models
{
    [Table("CUSTTABLE")]
    public class CustTable
    {
        public CustTable()
        {
            Orders = new HashSet<CustOrderTable>();
            Deposits = new HashSet<CpDeposit>();
            TankCapacities = new HashSet<TankCapacity>();
        }

        [Key]
        [Column("ACCOUNTNUM")]
        [StringLength(50)]
        public string AccountNum { get; set; }

        [Column("PASSWORD")]
        [StringLength(255)]
        public string Password { get; set; }

        [Column("CONFIRMPW")]
        [StringLength(255)]
        public string ConfirmPw { get; set; }

        [Column("CustSite")]
        [StringLength(100)]
        public string CustSite { get; set; }

        [Column("PARTY")]
        public long? Party { get; set; }

        [Column("DATAAREAID")]
        [StringLength(10)]
        public string DataAreaId { get; set; }

        [Column("CREDITMAX")]
        public decimal? CreditMax { get; set; }

        // Navigation properties
        [ForeignKey("Party")]
        public virtual DirPartyTable PartyEntity { get; set; }

        public virtual ICollection<CustOrderTable> Orders { get; set; }
        public virtual ICollection<CpDeposit> Deposits { get; set; }
        public virtual ICollection<TankCapacity> TankCapacities { get; set; }
    }
}
