using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Controllers.Finance
{
    public class AccountReceivableController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public AccountReceivableController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }
        public IActionResult AccountReceivable()
        {
            return View();
        }
    }
}
