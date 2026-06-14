using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArtContest.Data;
using ArtContest.Models;

namespace ArtContest.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var login = HttpContext.Session.GetString("UserLogin");
            if (string.IsNullOrEmpty(login)) return null;
            var user = _context.Users.FirstOrDefault(u => u.Login == login);
            return user?.Id;
        }

        private async Task<UserProfileViewModel> GetUserProfileViewModel()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return null;

            var user = await _context.Users
                .Include(u => u.Region)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;

            var regions = await _context.Regions.ToListAsync();
            var submissions = await _context.Submissions
                .Include(s => s.Contest)
                .Include(s => s.ModeratorLog)
                .Where(s => s.IdUser == userId)
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            return new UserProfileViewModel
            {
                User = user,
                Regions = regions,
                Submissions = submissions
            };
        }

        public async Task<IActionResult> Home(string search = "", string category = "", string sort = "new")
        {
            var query = _context.Contests
                .Include(c => c.Category)
                .Include(c => c.ApplicationPeriod)
                .Include(c => c.Stage)
                .Where(c => c.IdStage == 1 || c.IdStage == 2)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.Title.Contains(search) || c.Rules.Contains(search));

            if (!string.IsNullOrEmpty(category) && int.TryParse(category, out int catId))
                query = query.Where(c => c.IdCategory == catId);

            query = sort switch
            {
                "asc" => query.OrderBy(c => c.Title),
                "desc" => query.OrderByDescending(c => c.Title),
                _ => query.OrderByDescending(c => c.Id)
            };

            var contests = await query.ToListAsync();
            var categories = await _context.ContestCategories.ToListAsync();
            var userId = GetCurrentUserId();

            ViewBag.Categories = categories;
            ViewBag.Search = search;
            ViewBag.SelectedCategory = category;
            ViewBag.Sort = sort;
            ViewBag.UserId = userId;

            return View(contests);
        }

        public async Task<IActionResult> Profile()
        {
            var vm = await GetUserProfileViewModel();
            if (vm == null) return RedirectToAction("Login", "Auth");
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(string login, string email, string birthDate, int region, IFormFile profilePhoto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(login) || login.Trim().Length < 3)
            {
                ViewBag.Error = "Логин должен содержать минимум 3 символа";
                return await Profile();
            }
            if (login.Length > 50)
            {
                ViewBag.Error = "Логин не может превышать 50 символов";
                return await Profile();
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(login, @"^[a-zA-Z0-9_]+$"))
            {
                ViewBag.Error = "Логин может содержать только латинские буквы, цифры и _";
                return await Profile();
            }
            var existingLogin = await _context.Users.FirstOrDefaultAsync(u => u.Login == login.Trim() && u.Id != userId);
            if (existingLogin != null)
            {
                ViewBag.Error = "Пользователь с таким логином уже существует";
                return await Profile();
            }
            if (string.IsNullOrWhiteSpace(email) || !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$"))
            {
                ViewBag.Error = "Введите корректный email адрес";
                return await Profile();
            }
            var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower() && u.Id != userId);
            if (existingEmail != null)
            {
                ViewBag.Error = "Пользователь с таким email уже существует";
                return await Profile();
            }
            if (!DateTime.TryParse(birthDate, out var birth))
            {
                ViewBag.Error = "Некорректная дата рождения";
                return await Profile();
            }
            var age = DateTime.Now.Year - birth.Year;
            if (DateTime.Now.DayOfYear < birth.DayOfYear) age--;
            if (age < 5 || age > 120)
            {
                ViewBag.Error = "Введите корректную дату рождения";
                return await Profile();
            }
            if (region <= 0 || !await _context.Regions.AnyAsync(r => r.Id == region))
            {
                ViewBag.Error = "Выберите корректный регион";
                return await Profile();
            }

            if (profilePhoto != null && profilePhoto.Length > 0)
            {
                var allowedExt = new[] { ".jpg", ".jpeg", ".png" };
                var photoExt = Path.GetExtension(profilePhoto.FileName).ToLower();
                if (!allowedExt.Contains(photoExt))
                {
                    ViewBag.Error = "Допустимые форматы фото: JPG, JPEG, PNG";
                    return await Profile();
                }
                if (profilePhoto.Length > 5 * 1024 * 1024)
                {
                    ViewBag.Error = "Размер фото не может превышать 5 МБ";
                    return await Profile();
                }
                var photoName = Guid.NewGuid().ToString() + photoExt;
                var photoPath = Path.Combine("wwwroot", "images", "profiles", photoName);
                Directory.CreateDirectory(Path.GetDirectoryName(photoPath)!);
                using (var stream = new FileStream(photoPath, FileMode.Create))
                {
                    await profilePhoto.CopyToAsync(stream);
                }
                user.ProfilePhoto = "profiles/" + photoName;
            }

            user.Login = login.Trim();
            user.Email = email.Trim().ToLower();
            user.BirthDate = birthDate;
            user.IdRegion = region;

            await _context.SaveChangesAsync();
            HttpContext.Session.SetString("UserLogin", user.Login);

            ViewBag.Success = "Профиль успешно обновлён";
            return await Profile();
        }

        public async Task<IActionResult> Submit(int? contestId = null)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var contests = await _context.Contests
                .Include(c => c.ApplicationPeriod)
                .Include(c => c.Category)
                .Where(c => c.IdStage == 1)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            var existingSubmissionIds = await _context.Submissions
                .Where(s => s.IdUser == userId)
                .Select(s => s.IdContest)
                .ToListAsync();

            ViewBag.ContestId = contestId;
            ViewBag.ExistingSubmissionIds = existingSubmissionIds;
            return View(contests);
        }

        [HttpPost]
        public async Task<IActionResult> Submit(int contestId, string title, string description, IFormFile artFile)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            if (contestId <= 0)
            {
                ViewBag.Error = "Выберите конкурс";
                return await Submit(contestId);
            }

            var contest = await _context.Contests
                .Include(c => c.ApplicationPeriod)
                .FirstOrDefaultAsync(c => c.Id == contestId);

            if (contest == null || contest.IdStage != 1)
            {
                ViewBag.Error = "Конкурс не найден или приём заявок завершён";
                return await Submit(contestId);
            }

            if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 3)
            {
                ViewBag.Error = "Название работы должно содержать минимум 3 символа";
                return await Submit(contestId);
            }
            if (title.Trim().Length > 200)
            {
                ViewBag.Error = "Название работы не может превышать 200 символов";
                return await Submit(contestId);
            }
            if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < 10)
            {
                ViewBag.Error = "Описание должно содержать минимум 10 символов";
                return await Submit(contestId);
            }
            if (description.Trim().Length > 5000)
            {
                ViewBag.Error = "Описание не может превышать 5000 символов";
                return await Submit(contestId);
            }
            if (artFile == null || artFile.Length == 0)
            {
                ViewBag.Error = "Загрузите изображение работы";
                return await Submit(contestId);
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(artFile.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
            {
                ViewBag.Error = "Допустимые форматы: JPG, JPEG, PNG";
                return await Submit(contestId);
            }
            if (artFile.Length > 10 * 1024 * 1024)
            {
                ViewBag.Error = "Размер файла не может превышать 10 МБ";
                return await Submit(contestId);
            }

            var existingSubmission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.IdUser == userId && s.IdContest == contestId);
            if (existingSubmission != null)
            {
                ViewBag.Error = "Вы уже подали заявку на этот конкурс";
                return await Submit(contestId);
            }

            var fileName = Guid.NewGuid().ToString() + ext;
            var filePath = Path.Combine("wwwroot", "images", "submissions", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await artFile.CopyToAsync(stream);
            }

            var submission = new Submission
            {
                Title = title.Trim(),
                SubmissionImage = "submissions/" + fileName,
                SubmissionDate = DateTime.Now.ToString("yyyy-MM-dd"),
                AuthorDescription = description.Trim(),
                IdUser = userId.Value,
                IdContest = contestId
            };

            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Заявка успешно подана!";
            return RedirectToAction("Profile");
        }

        public async Task<IActionResult> Gallery(string search = "", string category = "", string sort = "new")
        {
            var query = _context.Contests
                .Include(c => c.Category)
                .Include(c => c.ApplicationPeriod)
                .Where(c => c.IdStage == 4)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.Title.Contains(search));

            if (!string.IsNullOrEmpty(category) && int.TryParse(category, out int catId))
                query = query.Where(c => c.IdCategory == catId);

            query = sort switch
            {
                "old" => query.OrderBy(c => c.Id),
                _ => query.OrderByDescending(c => c.Id)
            };

            var contests = await query.ToListAsync();
            var categories = await _context.ContestCategories.ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.Search = search;
            ViewBag.SelectedCategory = category;
            ViewBag.Sort = sort;

            return View(contests);
        }

        public async Task<IActionResult> Winners(int id)
        {
            var contest = await _context.Contests
                .Include(c => c.Category)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contest == null) return RedirectToAction("Gallery");

            var submissions = await _context.Submissions
                .Include(s => s.User)
                .Where(s => s.IdContest == id && s.TotalScore > 0)
                .OrderByDescending(s => s.TotalScore)
                .Take(10)
                .ToListAsync();

            ViewBag.Contest = contest;
            return View(submissions);
        }
    }

    public class UserProfileViewModel
    {
        public User User { get; set; } = null!;
        public List<Region> Regions { get; set; } = new();
        public List<Submission> Submissions { get; set; } = new();
    }
}
