using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Controllers.Finance
{
    public class CashPaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public CashPaymentController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }
        public IActionResult CashPayment()
        {
            return View();
        }
    }
}
