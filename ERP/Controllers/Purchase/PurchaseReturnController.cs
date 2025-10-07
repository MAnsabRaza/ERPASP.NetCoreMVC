using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Controllers.Purchase
{
    public class PurchaseReturnController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public PurchaseReturnController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }
        public IActionResult PurchaseReturn()
        {
            return View();
        }
    }
}
