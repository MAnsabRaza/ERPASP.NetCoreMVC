using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    public class FinancialYear
    {
        public int Id { get; set; }
        public DateOnly current_date { get;set; }
        public string year_name { get;set ; }
        public DateOnly start_date { get;set; }
        public DateOnly end_date { get;set; }
        public int? companyId { get; set; }
        [ForeignKey("companyId")]
        public virtual Company? Company { get; set; }

        public int? userId { get; set; }
        [ForeignKey("userId")]
        public virtual User? User {  get; set; }

        public string status { get; set; } = "open";
    }
}
