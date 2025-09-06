using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    public class Warehouse
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public string warehouse_name { get; set; }
        public string? warehouse_description { get; set; }
        public string address { get; set; }
        public string? city { get; set; }
        public string? country { get; set; }
        public bool status { get; set; }
        public string type { get; set; }
        public int companyId { get; set; }
        [ForeignKey("companyId")]
        public virtual Company? Company { get; set; }
    }
}
