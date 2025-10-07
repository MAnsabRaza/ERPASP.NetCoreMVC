using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Controllers.Finance
{
    public class AccountPayableController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public AccountPayableController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }
        public IActionResult AccountPayable()
        {
            return View();
        }
    }
}
