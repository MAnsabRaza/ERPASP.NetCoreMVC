using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Controllers.Sales
{
    public class SalesOrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public SalesOrderController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }
        public IActionResult SalesOrder()
        {
            return View();
        }
    }
}
