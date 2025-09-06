using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    public class StockMaster
    {
        public int Id { get; set; }
        public DateOnly current_date {  get; set; }
        public DateOnly due_date {  get; set; }
        public DateOnly posted_date {  get; set; }
        public int companyId { get; set; }
        [ForeignKey("companyId")]
        public virtual Company? Company { get; set; }
        public int venderId { get; set; }
        [ForeignKey("venderId")]
        public virtual Vender? Vender { get; set; }
        public int customerId { get; set; }
        [ForeignKey("customerId")]
        public virtual Customer? Customer { get; set; }
        public int transporterId { get; set; }
        [ForeignKey("transporterId")]
        public virtual Transporter? Transporter { get; set; }
        public string etype {  get; set; }
        public decimal total_amount { get; set; }
        public decimal discount_amount { get; set; }
        public decimal tax_amount { get; set; }
        public decimal net_amount {  get; set; }
        public string remarks { get;set; }

    }
}
