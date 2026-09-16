using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPortal_MVC_.Models
{
    [Table("LUBRICANT_BRANDS")]
    public class LubricantBrand
    {
        public LubricantBrand()
        {
            Products = new HashSet<LubricantProduct>();
        }

        [Key]
        [Column("BRAND_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BrandId { get; set; }

        [Column("BRAND_NAME")]
        [StringLength(100)]
        public string BrandName { get; set; }

        // Navigation property
        public virtual ICollection<LubricantProduct> Products { get; set; }
    }
}
