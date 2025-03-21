using Microsoft.AspNetCore.Mvc;

namespace EliteEscapes.Web.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
