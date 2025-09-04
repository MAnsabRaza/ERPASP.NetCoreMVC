using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models.UserManagement
{
    public class User
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public string? name { get; set; }
        public int companyId { get; set; }
        [ForeignKey("companyId")]
        public virtual Company? Company { get; set; }
        public int roleId { get; set; }
        [ForeignKey("roleId")]
        public virtual Role? Role { get; set; }
        public string? address { get; set; }
        public string? phone_number { get; set; }
        public string? email { get; set; }
        public string? password { get; set; }
        public string? image { get; set; }
        public bool status { get; set; }
    }
}
