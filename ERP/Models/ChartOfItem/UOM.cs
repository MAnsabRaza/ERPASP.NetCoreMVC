namespace ERP.Models.ChartOfItem
{
    public class UOM
    {
        public int Id { get; set; }
        public DateOnly current_date { get; set; }
        public string uom_name { get; set; }
        public bool status { get; set; }
    }
}
