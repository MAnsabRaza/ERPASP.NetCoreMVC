using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    [Table("Customer")]
    public class Customer
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Required, MaxLength(150)]
        public string name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string email { get; set; } = string.Empty;

        public string? address { get; set; }
        public string? city { get; set; }
        public bool status { get; set; }
        public string? country { get; set; }
        public string? phone { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal credit_limit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal current_balance { get; set; }

        public int companyId { get; set; }
        [ForeignKey("companyId")]
        public virtual Company? Company { get; set; }
    }
}