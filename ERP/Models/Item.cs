using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    public class Item
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public string remark { get; set; }
        public string item_name { get; set; }
        public string item_barcode { get; set; }
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
        public decimal purchase_rate { get; set; }
        public decimal sale_rate { get; set; }
        public decimal rate { get; set; }
        public decimal discount_amount { get; set; }
        public decimal total_amount { get; set; }
        public string description { get; set; }
    }
}
