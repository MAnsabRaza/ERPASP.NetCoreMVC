using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Controllers.Finance
{
    public class JVController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotyfService _notyf;
        public JVController(AppDbContext context, INotyfService notyf)
        {
            _context = context;
            _notyf = notyf;
        }
        public IActionResult JV()
        {
            return View();
        }
    }
}
