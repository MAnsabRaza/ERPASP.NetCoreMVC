using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    public class Bank
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public string bank_name { get; set; } = string.Empty;
        public string account_no { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal opening_balance { get; set; }

        public bool status { get; set; }
        public int companyId { get; set; }

        [ForeignKey("companyId")]
        public virtual Company? Company { get; set; }
    }
}