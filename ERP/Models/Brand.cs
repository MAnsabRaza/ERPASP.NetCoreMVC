using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    public class Brand
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public string brand_name { get; set; } = string.Empty;
        public string? brand_description { get; set; }
        public bool status { get; set; }
        public int? companyId { get; set; }
        [ForeignKey("companyId")]
        public virtual Company? Company { get; set; }
    }
}