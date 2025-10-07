using Microsoft.AspNetCore.Mvc;

namespace ERP.Controllers.Finance
{
    public class CashPaymentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
