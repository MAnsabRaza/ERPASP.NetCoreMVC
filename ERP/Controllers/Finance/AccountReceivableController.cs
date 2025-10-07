using Microsoft.AspNetCore.Mvc;

namespace ERP.Controllers.Finance
{
    public class AccountReceivableController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
