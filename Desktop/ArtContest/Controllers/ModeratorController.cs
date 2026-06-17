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

        private bool IsModerator()
        {
            var roleId = HttpContext.Session.GetString("UserRoleId");
            return roleId == "2";
        }

        private IActionResult RequireModerator()
        {
            if (GetCurrentUserId() == null || !IsModerator())
                return RedirectToAction("Login", "Auth");
            return null;
        }

        private async Task UpdateContestStages()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var allContests = await _context.Contests
                .Include(c => c.ApplicationPeriod)
                .Include(c => c.JudgingPeriod)
                .ToListAsync();

            foreach (var contest in allContests)
            {
                if (contest.ApplicationPeriod == null || contest.JudgingPeriod == null)
                    continue;

                var appEndDate = contest.ApplicationPeriod.AppEndDate;
                var judStartDate = contest.JudgingPeriod.JudStartDate;
                var judEndDate = contest.JudgingPeriod.JudEndDate;
                var appStartDate = contest.ApplicationPeriod.AppStartDate;

                int newStage;
                if (judEndDate.CompareTo(today) < 0)
                    newStage = 4;
                else if (appEndDate.CompareTo(today) < 0)
                    newStage = 3;
                else if (appStartDate.CompareTo(today) <= 0)
                    newStage = 2;
                else
                    newStage = 1;

                if (contest.IdStage != newStage)
                    contest.IdStage = newStage;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> Home(string search = "", string category = "", string sort = "new")
        {
            var auth = RequireModerator();
            if (auth != null) return auth;

            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            ViewBag.ModeratorLogin = user?.Login ?? "Модератор";

            await UpdateContestStages();

            var query = _context.Contests
                .Include(c => c.Category)
                .Include(c => c.ApplicationPeriod)
                .Where(c => c.IdStage == 1 || c.IdStage == 2)
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

            foreach (var contest in contests)
            {
                contest.NewSubmissionsCount = await _context.Submissions
                    .CountAsync(s => s.IdContest == contest.Id && s.IdModLog == null);
            }

            ViewBag.Categories = categories;
            ViewBag.Search = search;
            ViewBag.SelectedCategory = category;
            ViewBag.Sort = sort;

            return View(contests);
        }

        public async Task<IActionResult> ContestItems(int id)
        {
            var auth = RequireModerator();
            if (auth != null) return auth;

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
            var auth = RequireModerator();
            if (auth != null) return auth;

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
            var auth = RequireModerator();
            if (auth != null) return auth;

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

            var userId = GetCurrentUserId();
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
            var auth = RequireModerator();
            if (auth != null) return auth;

            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            ViewBag.ModeratorLogin = user?.Login ?? "Модератор";

            await UpdateContestStages();

            var query = _context.Submissions
                .Include(s => s.User)
                .Include(s => s.Contest)
                .Include(s => s.ModeratorLog)
                .Where(s => s.IdModLog != null && s.ModeratorLog != null && s.ModeratorLog.IdUser == userId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s => s.Title.Contains(search) || (s.User != null && s.User.Login.Contains(search)));

            if (!string.IsNullOrEmpty(contest) && int.TryParse(contest, out int contestId))
                query = query.Where(s => s.IdContest == contestId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.ModeratorLog != null && s.ModeratorLog.Status == status);

            var submissions = await query.OrderByDescending(s => s.Id).ToListAsync();

            var contests = await _context.Contests
                .Where(c => c.IdStage == 2)
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
