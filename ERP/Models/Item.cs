using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    public class Item
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public string? remark { get; set; }
        public string item_name { get; set; } = string.Empty;
        public string item_barcode { get; set; } = string.Empty;
        public bool status { get; set; }

        public int categoryId { get; set; }
        [ForeignKey("categoryId")]
        public virtual Category? Category { get; set; }

        public int subCategoryId { get; set; }
        [ForeignKey("subCategoryId")]
        public virtual SubCategory? SubCategory { get; set; }

        public int uomId { get; set; }
        [ForeignKey("uomId")]
        public virtual UOM? UOM { get; set; }

        public int brandId { get; set; }
        [ForeignKey("brandId")]
        public virtual Brand? Brand { get; set; }

        public int qty { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal purchase_rate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal sale_rate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal rate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? discount_amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal total_amount { get; set; }

        public string? description { get; set; }
    }
}