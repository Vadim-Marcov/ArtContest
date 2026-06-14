using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArtContest.Data;
using ArtContest.Models;

namespace ArtContest.Controllers
{
    public class JuryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JuryController(ApplicationDbContext context)
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

        public async Task<IActionResult> Home(string search = "", string category = "", string sort = "new")
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var user = await _context.Users.FindAsync(userId);
            ViewBag.JuryLogin = user?.Login ?? "Член жюри";

            var query = _context.Contests
                .Include(c => c.Category)
                .Include(c => c.ApplicationPeriod)
                .Include(c => c.JudgingPeriod)
                .Where(c => c.IdStage == 3)
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

            ViewBag.Categories = categories;
            ViewBag.Search = search;
            ViewBag.SelectedCategory = category;
            ViewBag.Sort = sort;

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
                .Where(s => s.IdContest == id && s.ModeratorLog != null && s.ModeratorLog.Status == "Одобрено")
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            var assessedSubmissionIds = await _context.JuryAssessments
                .Where(a => a.IdUser == userId)
                .Select(a => a.IdSubmission)
                .ToListAsync();

            ViewBag.Contest = contest;
            ViewBag.AssessedSubmissionIds = assessedSubmissionIds;

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
                .Include(s => s.Contest.Criteria1)
                .Include(s => s.Contest.Criteria2)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null) return RedirectToAction("Home");

            var existingAssessment = await _context.JuryAssessments
                .FirstOrDefaultAsync(a => a.IdSubmission == submissionId && a.IdUser == userId);

            ViewBag.Submission = submission;
            ViewBag.ExistingAssessment = existingAssessment;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Review(int submissionId, int score1, int score2, string juryComment)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var submission = await _context.Submissions
                .Include(s => s.Contest)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null) return RedirectToAction("Home");

            if (score1 < 0 || score1 > 50)
            {
                ViewBag.Error = "Оценка 1 должна быть от 0 до 50";
                ViewBag.Submission = submission;
                return View();
            }

            if (score2 < 0 || score2 > 50)
            {
                ViewBag.Error = "Оценка 2 должна быть от 0 до 50";
                ViewBag.Submission = submission;
                return View();
            }

            if (string.IsNullOrWhiteSpace(juryComment) || juryComment.Trim().Length < 5)
            {
                ViewBag.Error = "Комментарий должен содержать минимум 5 символов";
                ViewBag.Submission = submission;
                return View();
            }

            var existingAssessment = await _context.JuryAssessments
                .FirstOrDefaultAsync(a => a.IdSubmission == submissionId && a.IdUser == userId);

            if (existingAssessment != null)
            {
                existingAssessment.Score1 = score1;
                existingAssessment.Score2 = score2;
                existingAssessment.JuryComment = juryComment.Trim();
            }
            else
            {
                var assessment = new JuryAssessment
                {
                    Score1 = score1,
                    Score2 = score2,
                    JuryComment = juryComment.Trim(),
                    IdUser = userId.Value,
                    IdSubmission = submissionId
                };
                _context.JuryAssessments.Add(assessment);
            }

            await _context.SaveChangesAsync();

            var allAssessments = await _context.JuryAssessments
                .Where(a => a.IdSubmission == submissionId)
                .ToListAsync();

            if (allAssessments.Any())
            {
                submission.TotalScore = allAssessments.Sum(a => (double)(a.Score1 + a.Score2));
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Оценка сохранена";
            return RedirectToAction("ContestItems", new { id = submission.IdContest });
        }

        public async Task<IActionResult> History(string search = "", string contest = "", string category = "")
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var user = await _context.Users.FindAsync(userId);
            ViewBag.JuryLogin = user?.Login ?? "Член жюри";

            var query = _context.JuryAssessments
                .Include(a => a.Submission)
                .Include(a => a.Submission.Contest)
                .Include(a => a.Submission.Contest.Category)
                .Include(a => a.Submission.User)
                .Where(a => a.IdUser == userId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(a => a.Submission != null && a.Submission.Title.Contains(search));

            if (!string.IsNullOrEmpty(contest) && int.TryParse(contest, out int contestId))
                query = query.Where(a => a.Submission != null && a.Submission.IdContest == contestId);

            if (!string.IsNullOrEmpty(category) && int.TryParse(category, out int catId))
                query = query.Where(a => a.Submission != null && a.Submission.Contest != null && a.Submission.Contest.IdCategory == catId);

            var assessments = await query.OrderByDescending(a => a.Id).ToListAsync();

            var contests = await _context.Contests
                .Where(c => c.IdStage == 3 || c.IdStage == 4)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            var categories = await _context.ContestCategories.ToListAsync();

            ViewBag.Contests = contests;
            ViewBag.Categories = categories;
            ViewBag.Search = search;
            ViewBag.SelectedContest = contest;
            ViewBag.SelectedCategory = category;

            return View(assessments);
        }
    }
}
