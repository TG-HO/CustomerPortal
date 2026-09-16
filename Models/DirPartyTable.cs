using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPortal_MVC_.Models
{
    [Table("DIRPARTYTABLE")]
    public class DirPartyTable
    {
        public DirPartyTable()
        {
            Customers = new HashSet<CustTable>();
        }

        [Key]
        [Column("RECID")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long RecId { get; set; }

        [Column("NAME")]
        [StringLength(200)]
        public string Name { get; set; }

        // Navigation properties
        public virtual ICollection<CustTable> Customers { get; set; }
    }
}
