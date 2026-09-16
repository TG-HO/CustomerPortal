using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPortal_MVC_.Models
{
    [Table("USERINFO")]
    public class UserInfo
    {
        public UserInfo()
        {
            SessionLogs = new HashSet<CustSessionLog>();
        }

        [Key]
        [Column("NETWORKALIAS")]
        [StringLength(50)]
        public string NetworkAlias { get; set; }

        [Column("NAME")]
        [StringLength(100)]
        public string Name { get; set; }

        [Column("PASSWORD")]
        [StringLength(255)]
        public string Password { get; set; }

        [Column("ENABLE")]
        public int? Enable { get; set; }

        [Column("lub_flag")]
        [StringLength(10)]
        public string LubFlag { get; set; }

        // Navigation properties
        public virtual ICollection<CustSessionLog> SessionLogs { get; set; }
    }
}
