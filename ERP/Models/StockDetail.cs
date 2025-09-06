
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    public class StockDetail
    {
        public int Id {  get; set; }
        public DateOnly current_date {  get; set; }
        public int stockMasterId { get; set; }
        [ForeignKey("stockMasterId")]
        public virtual StockMaster? StockMaster { get; set; }
        public int warehouseId { get; set; }
        [ForeignKey("warehouseId")]
        public virtual Warehouse? Warehouse{ get; set; }
        public int itemId { get; set; }
        [ForeignKey("itemId")]
        public virtual Item? Item { get; set; }

        public int qty { get; set; }
        public decimal rate { get; set; }
        public decimal amount { get; set; }
        public decimal discount_percentage { get; set; }
        public decimal discount_amount { get; set; }
        public decimal net_amount { get; set; }

    }
}
