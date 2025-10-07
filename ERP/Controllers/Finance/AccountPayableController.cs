using Microsoft.AspNetCore.Mvc;

namespace ERP.Controllers.Finance
{
    public class AccountPayableController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
