using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    [Table("JournalDetail")]
    public class JournalDetail
    {
        public int Id { get; set; }
        public DateOnly? current_date { get; set; }

        public int journalEntryId { get; set; }
        [ForeignKey("journalEntryId")]
        public virtual JournalEntry? JournalEntry { get; set; }

        public int chartOfAccountId { get; set; }
        [ForeignKey("chartOfAccountId")]
        public virtual ChartOfAccount? ChartOfAccount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal debit_amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal credit_amount { get; set; }

        public string? description { get; set; }
    }
}