using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    public class SubCategory
    {
        public int Id { get; set; }

        public DateOnly current_date { get; set; }

        [ForeignKey("categoryId")]
        public int categoryId { get; set; }

        public virtual Category? Category { get; set; }

        public string sub_category_name { get; set; }
        public string? sub_category_description { get; set; }
        public bool status { get; set; }
    }
}
