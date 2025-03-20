using Microsoft.AspNetCore.Mvc;

namespace PayMeChat_V._1.Controllers
{
    public class AdminDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
