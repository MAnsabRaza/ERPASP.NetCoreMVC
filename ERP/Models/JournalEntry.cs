using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    public class JournalEntry
    {
        public int Id {  get; set; }
        public DateOnly current_date { get; set; }
        public DateOnly due_date { get; set; }
        public DateOnly posted_date {  get; set; }
        public int companyId { get; set; }
        [ForeignKey("companyId")]
        public virtual Company? Company { get; set; }
        public string etype { get; set; }
        public string description { get; set; }
        public decimal total_debit { get; set; }
        public decimal total_credit { get; set; }

    }
}
