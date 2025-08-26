namespace ERP.Models
{
    public class Brand
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public string brand_name {  get; set; }
        public string brand_description {  get; set; }
        public bool status {  get; set; }

    }
}
