using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models.UserManagement
{
    public class Permission
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public int roleId { get; set; }
        [ForeignKey("roleId")]
        public virtual Role? Role { get; set; }
        public int moduleId { get; set; }
        [ForeignKey("moduleId")]
        public virtual Module? Module { get; set; }
        public int componentId { get; set; }
        [ForeignKey("componentId")]
        public virtual Component? Component { get; set; }
        public bool view { get; set; }
        public bool create { get; set; }
        public bool delete { get; set; }
        public bool edit { get; set; }
    }
}
