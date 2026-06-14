using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArtContest.Data;
using ArtContest.Models;

namespace ArtContest.Controllers
{
    public class ModeratorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ModeratorController(ApplicationDbContext context)
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

        public async Task<IActionResult> Home(string search = "", string category = "")
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var user = await _context.Users.FindAsync(userId);
            ViewBag.ModeratorLogin = user?.Login ?? "Модератор";

            var query = _context.Contests
                .Include(c => c.Category)
                .Include(c => c.ApplicationPeriod)
                .Where(c => c.IdStage == 1 || c.IdStage == 2)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.Title.Contains(search));

            if (!string.IsNullOrEmpty(category) && int.TryParse(category, out int catId))
                query = query.Where(c => c.IdCategory == catId);

            var contests = await query.OrderByDescending(c => c.Id).ToListAsync();
            var categories = await _context.ContestCategories.ToListAsync();

            foreach (var contest in contests)
            {
                contest.NewSubmissionsCount = await _context.Submissions
                    .CountAsync(s => s.IdContest == contest.Id && s.IdModLog == null);
            }

            ViewBag.Categories = categories;
            ViewBag.Search = search;
            ViewBag.SelectedCategory = category;

            return View(contests);
        }

        public async Task<IActionResult> ContestItems(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var contest = await _context.Contests
                .Include(c => c.Category)
                .Include(c => c.ApplicationPeriod)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contest == null) return RedirectToAction("Home");

            var submissions = await _context.Submissions
                .Include(s => s.User)
                .Include(s => s.ModeratorLog)
                .Where(s => s.IdContest == id && s.IdModLog == null)
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            ViewBag.Contest = contest;
            return View(submissions);
        }

        public async Task<IActionResult> Review(int submissionId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var submission = await _context.Submissions
                .Include(s => s.User)
                .Include(s => s.Contest)
                .Include(s => s.Contest.Category)
                .Include(s => s.ModeratorLog)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null) return RedirectToAction("Home");

            ViewBag.Submission = submission;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Review(int submissionId, string status, string comment)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var submission = await _context.Submissions
                .Include(s => s.ModeratorLog)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null) return RedirectToAction("Home");

            if (string.IsNullOrWhiteSpace(status))
            {
                ViewBag.Error = "Выберите решение";
                ViewBag.Submission = submission;
                return View();
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                ViewBag.Error = "Добавьте комментарий для участника";
                ViewBag.Submission = submission;
                return View();
            }

            var modLog = new ModeratorLog
            {
                Status = status,
                ModComment = comment.Trim(),
                IdUser = userId.Value,
                ResponseDate = DateTime.Now.ToString("yyyy-MM-dd")
            };

            _context.ModeratorLogs.Add(modLog);
            await _context.SaveChangesAsync();

            submission.IdModLog = modLog.Id;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Решение сохранено";
            return RedirectToAction("ContestItems", new { id = submission.IdContest });
        }

        public async Task<IActionResult> Log(string search = "", string contest = "", string status = "")
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var user = await _context.Users.FindAsync(userId);
            ViewBag.ModeratorLogin = user?.Login ?? "Модератор";

            var query = _context.Submissions
                .Include(s => s.User)
                .Include(s => s.Contest)
                .Include(s => s.ModeratorLog)
                .Where(s => s.IdModLog != null)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s => s.Title.Contains(search) || (s.User != null && s.User.Login.Contains(search)));

            if (!string.IsNullOrEmpty(contest) && int.TryParse(contest, out int contestId))
                query = query.Where(s => s.IdContest == contestId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.ModeratorLog != null && s.ModeratorLog.Status == status);

            var submissions = await query.OrderByDescending(s => s.Id).ToListAsync();

            var contests = await _context.Contests
                .Where(c => c.IdStage == 1 || c.IdStage == 2 || c.IdStage == 3 || c.IdStage == 4)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            ViewBag.Contests = contests;
            ViewBag.Search = search;
            ViewBag.SelectedContest = contest;
            ViewBag.SelectedStatus = status;

            return View(submissions);
        }
    }
}
