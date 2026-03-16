using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    public class UOM
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public string uom_name { get; set; }
        public bool status { get; set; }
        public int? companyId { get; set; }
        [ForeignKey("companyId")]
        public virtual Company? Company { get; set; }
    }
}
