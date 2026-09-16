using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPortal_MVC_.Models
{
    [Table("cust_sessionlog")]
    public class CustSessionLog
    {
        [Key]
        [Column("sessionid")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long SessionId { get; set; }

        [Column("timein")]
        public DateTime? TimeIn { get; set; }

        [Column("timeout")]
        public DateTime? TimeOut { get; set; }

        [Column("ip")]
        [StringLength(50)]
        public string Ip { get; set; }

        [Column("username")]
        [StringLength(50)]
        public string Username { get; set; }

        // Navigation property
        [ForeignKey("Username")]
        public virtual UserInfo User { get; set; }
    }
}
