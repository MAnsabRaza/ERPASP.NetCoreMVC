namespace ERP.Models.UserManagement
{
    public class Role
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public string? role_name { get; set; }
        public bool status {  get; set; }
    }

}
