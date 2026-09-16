using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPortal_MVC_.Models
{
    [Table("LUBRICANT_PRODUCTS")]
    public class LubricantProduct
    {
        public LubricantProduct()
        {
            OrderLines = new HashSet<LubricantOrderLine>();
        }

        [Key]
        [Column("PRODUCT_CODE")]
        [StringLength(50)]
        public string ProductCode { get; set; }

        [Column("PRODUCT_NAME")]
        [StringLength(150)]
        public string ProductName { get; set; }

        [Column("BRAND_ID")]
        public int BrandId { get; set; }

        [Column("PACK_SIZE")]
        [StringLength(50)]
        public string PackSize { get; set; }

        [Column("PRICE")]
        public decimal Price { get; set; }

        // Navigation properties
        [ForeignKey("BrandId")]
        public virtual LubricantBrand Brand { get; set; }

        public virtual ICollection<LubricantOrderLine> OrderLines { get; set; }
    }
}
