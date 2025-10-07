using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Controllers.Purchase
{
    public class PurchaseController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public PurchaseController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }
        public IActionResult Purchase()
        {
            return View();
        }
    }
}
