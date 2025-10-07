using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Controllers.Purchase
{
    public class PurchaseOrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public PurchaseOrderController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }
        public IActionResult PurchaseOrder()
        {
            return View();
        }
    }
}
