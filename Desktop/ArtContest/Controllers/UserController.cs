using Microsoft.AspNetCore.Mvc;

namespace ArtContest.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Home()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Gallery()
        {
            return View();
        }

        public IActionResult Submit()
        {
            return View();
        }

        public IActionResult Winners()
        {
            return View();
        }
    }
}
