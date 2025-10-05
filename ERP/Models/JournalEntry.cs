using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    public class JournalEntry
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public DateOnly due_date { get; set; }
        public DateOnly posted_date { get; set; }
        public int companyId { get; set; }

        [ForeignKey("companyId")]
        public virtual Company? Company { get; set; }
        public int? userId { get; set; }
        [ForeignKey("userId")]
        public virtual User? User { get; set; }

        public string etype { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public decimal total_debit { get; set; }
        public decimal total_credit { get; set; }
    }
}