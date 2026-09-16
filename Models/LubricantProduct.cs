using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerPortal_MVC_.Models
{
    [NotMapped]
    public class LubricantProduct
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int BrandId { get; set; }
        public string PackSize { get; set; }
        public decimal Price { get; set; }
    }
}
