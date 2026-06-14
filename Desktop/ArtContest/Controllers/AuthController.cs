using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArtContest.Data;
using ArtContest.Models;

namespace ArtContest.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string login, string password)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Login == login && u.Password == password);

            if (user == null)
            {
                ViewBag.Error = "Неверный логин или пароль";
                return View();
            }

            switch (user.IdRole)
            {
                case 1:
                    return RedirectToAction("Analytics", "Admin");
                case 2:
                    return RedirectToAction("Home", "Moderator");
                case 3:
                    return RedirectToAction("Home", "Jury");
                case 4:
                    return RedirectToAction("Home", "User");
                default:
                    return RedirectToAction("Home", "User");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            var regions = await _context.Regions.ToListAsync();
            return View(regions);
        }

        [HttpPost]
        public async Task<IActionResult> Register(string login, string email, string birthDate, string password, int region)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == login || u.Email == email);

            if (existingUser != null)
            {
                ViewBag.Error = "Пользователь с таким логином или email уже существует";
                var regions = await _context.Regions.ToListAsync();
                return View(regions);
            }

            var user = new User
            {
                Login = login,
                Email = email,
                BirthDate = birthDate,
                Password = password,
                IdRegion = region,
                IdRole = 4
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }
    }
}
