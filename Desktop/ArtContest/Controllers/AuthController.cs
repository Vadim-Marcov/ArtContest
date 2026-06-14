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
            HttpContext.Session.Clear();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Заполните все поля";
                return View();
            }

            login = login.Trim();
            if (login.Length < 3 || login.Length > 50)
            {
                ViewBag.Error = "Логин должен содержать от 3 до 50 символов";
                return View();
            }

            if (password.Length < 6 || password.Length > 100)
            {
                ViewBag.Error = "Пароль должен содержать от 6 до 100 символов";
                return View();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Login == login && u.Password == password);

            if (user == null)
            {
                ViewBag.Error = "Неверный логин или пароль";
                return View();
            }

            HttpContext.Session.SetString("UserLogin", user.Login);
            HttpContext.Session.SetString("UserRoleId", user.IdRole.ToString());

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
        public async Task<IActionResult> Register(string login, string email, string birthDate, string password, string confirmPassword, int region)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(birthDate) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error = "Заполните все поля";
                var regions = await _context.Regions.ToListAsync();
                return View(regions);
            }

            login = login.Trim();
            email = email.Trim();

            if (login.Length < 3 || login.Length > 50)
            {
                ViewBag.Error = "Логин должен содержать от 3 до 50 символов";
                var regions = await _context.Regions.ToListAsync();
                return View(regions);
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(login, @"^[a-zA-Z0-9_]+$"))
            {
                ViewBag.Error = "Логин может содержать только латинские буквы, цифры и символ _";
                var regions = await _context.Regions.ToListAsync();
                return View(regions);
            }

            if (login.Equals(password, StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error = "Пароль не должен совпадать с логином";
                var regions = await _context.Regions.ToListAsync();
                return View(regions);
            }

            if (email.Length > 100)
            {
                ViewBag.Error = "Email не может превышать 100 символов";
                var regions = await _context.Regions.ToListAsync();
                return View(regions);
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$"))
            {
                ViewBag.Error = "Введите корректный email адрес";
                var regions = await _context.Regions.ToListAsync();
                return View(regions);
            }

            if (password.Length < 6 || password.Length > 100)
            {
                ViewBag.Error = "Пароль должен содержать от 6 до 100 символов";
                var regions = await _context.Regions.ToListAsync();
                return View(regions);
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Пароли не совпадают";
                var regions = await _context.Regions.ToListAsync();
                return View(regions);
            }

            if (password.ToLower().Contains(login.ToLower()) && login.Length >= 3)
            {
                ViewBag.Error = "Пароль не должен содержать логин";
                var regions = await _context.Regions.ToListAsync();
                return View(regions);
            }

            if (!DateTime.TryParse(birthDate, out var birth))
            {
                ViewBag.Error = "Некорректная дата рождения";
                var regions = await _context.Regions.ToListAsync();
                return View(regions);
            }

            var age = DateTime.Now.Year - birth.Year;
            if (DateTime.Now.DayOfYear < birth.DayOfYear)
                age--;

            if (age < 5)
            {
                ViewBag.Error = "Возраст должен быть не менее 5 лет";
                var regions = await _context.Regions.ToListAsync();
                return View(regions);
            }
            if (age > 120)
            {
                ViewBag.Error = "Введите корректную дату рождения";
                var regions = await _context.Regions.ToListAsync();
                return View(regions);
            }

            if (region <= 0)
            {
                ViewBag.Error = "Выберите регион";
                var regions = await _context.Regions.ToListAsync();
                return View(regions);
            }

            if (!await _context.Regions.AnyAsync(r => r.Id == region))
            {
                ViewBag.Error = "Выберите корректный регион";
                var regions = await _context.Regions.ToListAsync();
                return View(regions);
            }

            var loginExists = await _context.Users.AnyAsync(u => u.Login == login);
            if (loginExists)
            {
                ViewBag.Error = "Пользователь с таким логином уже существует";
                var regions = await _context.Regions.ToListAsync();
                return View(regions);
            }

            var emailExists = await _context.Users.AnyAsync(u => u.Email == email.ToLower());
            if (emailExists)
            {
                ViewBag.Error = "Пользователь с таким email уже существует";
                var regions = await _context.Regions.ToListAsync();
                return View(regions);
            }

            var user = new User
            {
                Login = login,
                Email = email.ToLower(),
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
