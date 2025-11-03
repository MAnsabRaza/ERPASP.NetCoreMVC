using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    [Table("Vender")]
    public class Vender
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }= DateOnly.FromDateTime(DateTime.Today);

        [Required, MaxLength(150)]
        public string name { get; set; } = string.Empty;

        public string? email { get; set; }
        public string? address { get; set; }
        public string? city { get; set; }
        public bool status { get; set; }
        public string? country { get; set; }
        public string? phone { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal current_balance { get; set; }

        public int companyId { get; set; }
        [ForeignKey("companyId")]
        public virtual Company? Company { get; set; }
    }
}