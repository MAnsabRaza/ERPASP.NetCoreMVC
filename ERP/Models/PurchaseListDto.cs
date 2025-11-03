using System;

namespace ERP.Models
{
    public class PurchaseListDto
    {
        public int Id { get; set; }
        public DateOnly CurrentDate { get; set; }
        public string Etype { get; set; } = string.Empty;
        public string? Remarks { get; set; }

        // Match StockMaster → decimal
        public decimal TotalAmount { get; set; }
        public decimal NetAmount { get; set; }

        public string? VenderName { get; set; }
        public string? TransporterNo { get; set; }
    }
}