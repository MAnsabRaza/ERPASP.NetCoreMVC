using System.ComponentModel.DataAnnotations.Schema;
using ERP.Models.Master;
using ERP.Models.UserManagement;

namespace ERP.Models.Account
{
    public class PaymentVoucher
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public DateOnly voucher_date { get; set; }
        public decimal amount { get; set; }
        public string method { get; set; }
        public bool status { get; set; }
        public int companyId { get; set; }
        [ForeignKey("companyId")]
        public virtual Company? Company { get; set; }
        public int venderId { get; set; }
        [ForeignKey("venderId")]
        public virtual Vender? Vender { get; set; }
        public int bankAccountId { get; set; }
        [ForeignKey("bankAccountId")]
        public virtual Bank? Bank { get; set; }
    }
}
