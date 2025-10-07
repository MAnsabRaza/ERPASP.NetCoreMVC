using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Controllers.Sales
{
    public class SalesReturnController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public SalesReturnController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }
        public IActionResult SalesReturn()
        {
            return View();
        }
    }
}
