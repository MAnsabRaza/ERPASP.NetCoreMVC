namespace ERP.Models
{
    public class Transporter
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }= DateOnly.FromDateTime(DateTime.Today);
        public string name { get; set; } = string.Empty;
        public string transporter_no { get; set; } = string.Empty;
        public string? phone { get; set; }
        public bool status { get; set; }
        public string? address { get; set; }
        public string? description { get; set; }
    }
}