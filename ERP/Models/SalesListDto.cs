namespace ERP.Models
{
    public class SalesListDto
    {
        public int Id { get; set; }
        public DateOnly CurrentDate { get; set; }
        public string Etype { get; set; } = string.Empty;
        public string? Remarks { get; set; }

        // Match StockMaster → decimal
        public decimal TotalAmount { get; set; }
        public decimal NetAmount { get; set; }

        public string? CustomerName { get; set; }
    }
}
