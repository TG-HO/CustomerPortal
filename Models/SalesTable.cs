using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPortal_MVC_.Models
{
    [Table("SALESTABLE")]
    public class SalesTable
    {
        [Key]
        [Column("SALESID")]
        [StringLength(50)]
        public string SalesId { get; set; }

        [Column("PURCHORDERFORMNUM")]
        [StringLength(50)]
        public string PurchOrderFormNum { get; set; }

        [Column("SALESSTATUS")]
        public int? SalesStatus { get; set; }
    }
}
