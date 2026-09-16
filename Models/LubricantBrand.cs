using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPortal_MVC_.Models
{
    [NotMapped]
    public class LubricantBrand
    {
        public int BrandId { get; set; }
        public string BrandName { get; set; }
    }
}
