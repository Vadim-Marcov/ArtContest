using Microsoft.AspNetCore.Mvc;

namespace ArtContest.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Analytics()
        {
            return View();
        }

        public IActionResult Users()
        {
            return View();
        }

        public IActionResult Contests()
        {
            return View();
        }

        public IActionResult CreateContest()
        {
            return View();
        }

        public IActionResult EditContest()
        {
            return View();
        }
    }
}
