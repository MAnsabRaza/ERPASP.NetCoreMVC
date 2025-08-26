namespace ERP.Models
{
    public class Module
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public string module_name { get; set; }
        public string module_icon {  get; set; }
        public string moduel_href {  get; set; }
        public bool status {  get; set; }
    }
}
