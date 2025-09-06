using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Models
{
    public class Component
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public string component_name { get; set; }
        public int moduleId { get; set; }
        [ForeignKey("moduleId")]
        public virtual Module? Module { get; set; }
        public bool status { get; set; }


    }
}
