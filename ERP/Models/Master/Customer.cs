using ERP.Models.UserManagement;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models.Master
{
    public class Customer
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string? address { get; set; }
        public string? city { get; set; }
        public bool status { get; set; }
        public string? country { get; set; }
        public string? phone { get; set; }
        public decimal credit_limit { get; set; }
        public decimal current_balance { get; set; }
        public int companyId { get; set; }
        [ForeignKey("companyId")]
        public virtual Company? Company { get; set; }

    }
}
