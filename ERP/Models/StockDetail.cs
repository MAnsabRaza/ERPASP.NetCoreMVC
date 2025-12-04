using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    [Table("StockDetail")]
    public class StockDetail
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }

        public int StockMasterId { get; set; }
        [ForeignKey("StockMasterId")]
        public StockMaster StockMaster { get; set; }


        public int? warehouseId { get; set; }
        [ForeignKey("warehouseId")]
        public virtual Warehouse? Warehouse { get; set; }

        public int itemId { get; set; }
        [ForeignKey("itemId")]
        public virtual Item? Item { get; set; }

        public int qty { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal rate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? discount_percentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? discount_amount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal net_amount { get; set; }
    }
}