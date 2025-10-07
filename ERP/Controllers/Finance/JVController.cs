using Microsoft.AspNetCore.Mvc;

namespace ERP.Controllers.Finance
{
    public class JVController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
