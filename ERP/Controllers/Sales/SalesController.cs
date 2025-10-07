using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Controllers.Sales
{
    public class SalesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public SalesController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }
        public IActionResult Sales()
        {
            return View();
        }
    }
}
