using Microsoft.AspNetCore.Mvc;

namespace ArtContest.Controllers
{
    public class ModeratorController : Controller
    {
        public IActionResult Home()
        {
            return View();
        }

        public IActionResult ContestItems()
        {
            return View();
        }

        public IActionResult Review()
        {
            return View();
        }

        public IActionResult Log()
        {
            return View();
        }
    }
}
