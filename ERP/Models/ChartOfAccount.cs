using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    public class ChartOfAccount
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public string name { get; set; } = string.Empty;

        public int companyId { get; set; }
        [ForeignKey("companyId")]
        public virtual Company? Company { get; set; }

        public int accountTypeId { get; set; }
        [ForeignKey("accountTypeId")]
        public virtual AccountType? AccountType { get; set; }

        public int? parentAccountId { get; set; }
        [ForeignKey("parentAccountId")]
        public virtual ChartOfAccount? ParentAccount { get; set; }

    
    }
}