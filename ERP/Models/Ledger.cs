using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    public class Ledger
    {
        public int Id { get; set; }
        public DateOnly current_date {  get; set; }

        public int companyId { get; set; }
        [ForeignKey("companyId")]
        public virtual Company? Company { get; set; }
        public int chartOfAccountId { get; set; }
        [ForeignKey("chartOfAccountId")]
        public virtual ChartOfAccount? ChartOfAccount { get; set; }
        public int journalEntryId { get; set; }
        [ForeignKey("journalEntryId")]
        public virtual JournalEntry? JournalEntry { get; set; }

        public decimal debit_amount { get; set; }
        public decimal credit_amount { get; set; }
        public decimal running_balance { get; set; }
        public string description { get; set; }
    }
}
