using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models.Account
{
    public class Bank
    {
        public int Id { get; set; }
        public DateOnly current_date {  get; set; }
        public string bank_name { get; set; }
        public string account_no {  get; set; }
        public string name { get; set; }
        public decimal opening_balance {  get; set; }
        public bool status {  get; set; }
        public int companyId { get; set; }
        [ForeignKey("companyId")]
        public virtual Company? Company { get; set; }
    }
}
