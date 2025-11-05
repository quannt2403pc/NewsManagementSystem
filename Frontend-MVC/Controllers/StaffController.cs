using Microsoft.AspNetCore.Mvc;

namespace Frontend_MVC.Controllers
{
    public class StaffController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
