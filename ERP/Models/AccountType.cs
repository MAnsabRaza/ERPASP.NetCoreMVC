namespace ERP.Models
{
    public class AccountType
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public bool status { get; set; }
        public string account_name { get; set; }
    }
}
