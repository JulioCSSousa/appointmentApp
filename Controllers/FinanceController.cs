using Microsoft.AspNetCore.Mvc;

namespace appointmentApp.Controllers
{
    public class FinanceController : Controller
    {
        public FinanceController() { }

        [HttpGet]
        public ActionResult Index() { return View(); }
    }
}
