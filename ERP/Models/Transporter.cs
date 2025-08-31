namespace ERP.Models
{
    public class Transporter
    {
        public int Id {  get; set; }
        public DateOnly current_date { get; set; }
        public string name {  get; set; }
        public string transporter_no {  get; set; }
        public string? phone {  get; set; }
        public bool status {  get; set; }
        public string? address {  get; set; }
        public string? description { get; set; }
    }
}
