using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPortal_MVC_.Models
{
    [Table("tankCapacity")]
    public class TankCapacity
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("CUSTACCOUNT")]
        [StringLength(50)]
        public string CustAccount { get; set; }

        [Column("custgroup")]
        [StringLength(50)]
        public string CustGroup { get; set; }

        [Column("capacity")]
        public decimal? Capacity { get; set; }

        [Column("site")]
        [StringLength(50)]
        public string Site { get; set; }

        // Navigation property
        [ForeignKey("CustAccount")]
        public virtual CustTable Customer { get; set; }
    }
}
