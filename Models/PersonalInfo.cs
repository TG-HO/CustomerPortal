using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPortal_MVC_.Models
{
    [Table("personal_info")]
    public class PersonalInfo
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("name")]
        [StringLength(100)]
        public string Name { get; set; }

        [Column("address")]
        [StringLength(255)]
        public string Address { get; set; }
    }
}
