namespace ERP.Models
{
    public class Company
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public string company_name { get; set; }
        public string address { get; set; }
        public bool status { get; set; }
        public string website_path { get; set; }
        public string company_email { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string zipcode { get; set; }
        public string phone { get; set; }
        public string logo { get; set; }

    }
}
