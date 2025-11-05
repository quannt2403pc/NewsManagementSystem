using Microsoft.AspNetCore.Mvc;

namespace Frontend_MVC.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
