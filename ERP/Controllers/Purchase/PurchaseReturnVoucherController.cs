using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Controllers.Purchase
{
    public class PurchaseReturnVoucherController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public PurchaseReturnVoucherController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }
        public IActionResult PurchaseReturnVoucher()
        {
            return View("~/Views/Purchase/PurchaseReturnVoucher.cshtml");
        }
    }
}
