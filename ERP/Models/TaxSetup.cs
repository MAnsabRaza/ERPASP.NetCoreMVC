using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    public class TaxSetup
    {
        public int Id { get; set; }
        public string tax_name { get;set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? percentage { get; set; } = 0;

        public string applicable_on { get; set; } = "Both";
        public int? companyId { get; set; }
        [ForeignKey("companyId")]
        public virtual Company? Company { get; set; }

        public bool status { get; set; }

    }
}
