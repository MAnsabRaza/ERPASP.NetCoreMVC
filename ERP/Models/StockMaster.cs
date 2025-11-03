using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    [Table("StockMaster")]
    public class StockMaster
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

        public int? venderId { get; set; }
        [ForeignKey("venderId")]
        public virtual Vender? Vender { get; set; }

        public int? customerId { get; set; }
        [ForeignKey("customerId")]
        public virtual Customer? Customer { get; set; }

        public int transporterId { get; set; }
        [ForeignKey("transporterId")]
        public virtual Transporter? Transporter { get; set; }

        [Required, MaxLength(50)]
        public string etype { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal total_amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal discount_amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal tax_amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal net_amount { get; set; }

        public string remarks { get; set; } = string.Empty;

    }
}