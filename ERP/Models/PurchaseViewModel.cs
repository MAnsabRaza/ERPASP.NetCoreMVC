namespace ERP.Models
{
    public class PurchaseViewModel
    {
        public StockMaster StockMaster { get; set; }
        public List<StockDetail> StockDetail { get; set; }
        public PaymentVoucher PaymentVoucher { get; set; }
        // public JournalEntry JournalEntry { get; set; }
        //public List<JournalDetail> JournalDetail { get; set; }
        //public List<Ledger> Ledger { get; set; }
        //public List<Ledger> Ledger { get; set; }
    }
}
