namespace ERP.Models
{
    public class Company
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public string company_name { get; set; } = string.Empty;
        public string address { get; set; } = string.Empty;
        public bool status { get; set; }
        public string website_path { get; set; } = string.Empty;
        public string company_email { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string country { get; set; } = string.Empty;
        public string zipcode { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
        public string logo { get; set; } = string.Empty;
    }
}