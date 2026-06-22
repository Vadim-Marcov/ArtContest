using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArtContest.Data;
using ArtContest.Models;

namespace ArtContest.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
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

        private bool IsAdmin()
        {
            var roleId = HttpContext.Session.GetString("UserRoleId");
            return roleId == "1";
        }

        private IActionResult RequireAdmin()
        {
            if (GetCurrentUserId() == null || !IsAdmin())
                return RedirectToAction("Login", "Auth");
            return null;
        }

        private void SetAdminLogin()
        {
            var login = HttpContext.Session.GetString("UserLogin");
            if (string.IsNullOrEmpty(login))
            {
                login = "—";
            }
            ViewBag.AdminLogin = login;
            ViewBag.CurrentAdminLogin = login;
        }

        public async Task<IActionResult> Analytics()
        {
            var auth = RequireAdmin();
            if (auth != null) return auth;
            SetAdminLogin();

            var totalUsers = await _context.Users.CountAsync();
            var activeContests = await _context.Contests.CountAsync(c => c.IdStage != 4);
            var submissionsOnModeration = await _context.Submissions.CountAsync(s => s.IdModLog == null);

            var contests = await _context.Contests
                .Include(c => c.Category)
                .Include(c => c.ApplicationPeriod)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            var submissions = await _context.Submissions
                .Include(s => s.Contest)
                .Include(s => s.Contest.Category)
                .Include(s => s.User)
                .Include(s => s.JuryAssessments)
                .Where(s => s.IdModLog != null && s.ModeratorLog != null && s.ModeratorLog.Status == "Одобрено")
                .ToListAsync();

            var users = await _context.Users
                .Include(u => u.Region)
                .ToListAsync();

            var juryScoreReport = submissions
                .Select(s => new JuryScoreReportItem
                {
                    ContestTitle = s.Contest?.Title ?? "",
                    CategoryName = s.Contest?.Category?.CategoryName ?? "",
                    SubmissionTitle = s.Title,
                    AuthorLogin = s.User?.Login ?? "",
                    TotalScore = s.TotalScore,
                    JuryCount = s.JuryAssessments?.Count ?? 0
                })
                .OrderByDescending(r => r.ContestTitle)
                .ThenByDescending(r => r.TotalScore)
                .ToList();

            var winnersReport = submissions
                .Where(s => s.TotalScore.HasValue)
                .GroupBy(s => s.IdContest)
                .SelectMany(g => g.OrderByDescending(s => s.TotalScore).Take(3).Select((s, i) => new WinnerReportItem
                {
                    ContestTitle = s.Contest?.Title ?? "",
                    CategoryName = s.Contest?.Category?.CategoryName ?? "",
                    SubmissionTitle = s.Title,
                    AuthorLogin = s.User?.Login ?? "",
                    TotalScore = s.TotalScore,
                    Place = i + 1
                }))
                .OrderBy(r => r.ContestTitle)
                .ThenBy(r => r.Place)
                .ToList();

            var activityByRegion = users
                .Where(u => u.IdRole == 4)
                .GroupBy(u => u.Region?.RegionName ?? "Не указан")
                .Select(g => new ActivityByRegionItem
                {
                    RegionName = g.Key,
                    SubmissionCount = submissions.Count(s => s.User != null && s.User.IdRegion == g.First().IdRegion)
                })
                .OrderByDescending(r => r.SubmissionCount)
                .ToList();

            var activityByAge = users
                .Where(u => u.IdRole == 4 && !string.IsNullOrEmpty(u.BirthDate))
                .Select(u =>
                {
                    int age = 0;
                    if (DateTime.TryParse(u.BirthDate, out var birth))
                    {
                        age = DateTime.Now.Year - birth.Year;
                        if (DateTime.Now.DayOfYear < birth.DayOfYear) age--;
                    }
                    return new { User = u, Age = age };
                })
                .GroupBy(x =>
                {
                    if (x.Age < 14) return "до 14 лет";
                    if (x.Age < 18) return "14-17 лет";
                    if (x.Age < 25) return "18-24 лет";
                    if (x.Age < 35) return "25-34 лет";
                    if (x.Age < 50) return "35-49 лет";
                    return "50+ лет";
                })
                .Select(g => new ActivityByAgeItem
                {
                    AgeGroup = g.Key,
                    SubmissionCount = submissions.Count(s => g.Any(x => x.User.Id == s.IdUser))
                })
                .OrderBy(a => a.AgeGroup)
                .ToList();

            var allSubmissions = await _context.Submissions
                .Where(s => !string.IsNullOrEmpty(s.SubmissionDate))
                .ToListAsync();

            var submissionsByDay = allSubmissions
                .GroupBy(s => s.SubmissionDate)
                .OrderBy(g => g.Key)
                .Select(g => new SubmissionsByDayItem
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var contestCategoryMap = contests.ToDictionary(c => c.Id, c => c.IdCategory);
            var allCategories = await _context.ContestCategories.ToListAsync();
            var categoryPopularity = allCategories
                .Select(c => new CategoryPopularityItem
                {
                    CategoryName = c.CategoryName,
                    SubmissionCount = submissions.Count(s => contestCategoryMap.ContainsKey(s.IdContest) && contestCategoryMap[s.IdContest] == c.Id),
                    ContestCount = contests.Count(ct => ct.IdCategory == c.Id)
                })
                .OrderByDescending(c => c.SubmissionCount)
                .ToList();

            var viewModel = new AdminAnalyticsViewModel
            {
                TotalUsers = totalUsers,
                ActiveContests = activeContests,
                SubmissionsOnModeration = submissionsOnModeration,
                JuryScoreReport = juryScoreReport,
                WinnersReport = winnersReport,
                ActivityByRegion = activityByRegion,
                ActivityByAge = activityByAge,
                SubmissionsByDay = submissionsByDay,
                CategoryPopularity = categoryPopularity,
                Contests = contests
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetAnalyticsData()
        {
            var auth = RequireAdmin();
            if (auth != null) return Unauthorized();

            var allSubs = await _context.Submissions
                .Where(s => !string.IsNullOrEmpty(s.SubmissionDate))
                .Select(s => s.SubmissionDate)
                .ToListAsync();

            var today = DateTime.Now.Date;
            var startDate = today.AddDays(-13);

            if (allSubs.Any())
            {
                var minDate = allSubs.Min();
                if (DateTime.TryParse(minDate, out var parsedMin))
                {
                    startDate = parsedMin.Date;
                    if ((today - startDate).TotalDays > 13)
                    {
                        startDate = today.AddDays(-13);
                    }
                }
            }

            var submissionsByDay = new List<object>();
            for (var date = startDate; date <= today; date = date.AddDays(1))
            {
                var dateStr = date.ToString("yyyy-MM-dd");
                var count = allSubs.Count(d => d == dateStr);
                submissionsByDay.Add(new { date = dateStr, count });
            }

            var totalUsers = await _context.Users.CountAsync();
            var activeContests = await _context.Contests.CountAsync(c => c.IdStage != 4);
            var submissionsOnModeration = await _context.Submissions.CountAsync(s => s.IdModLog == null);

            return Json(new
            {
                totalUsers,
                activeContests,
                submissionsOnModeration,
                submissionsByDay
            });
        }

        [HttpGet]
        public async Task<IActionResult> ExportReport(string reportType)
        {
            var auth = RequireAdmin();
            if (auth != null) return Unauthorized();

            byte[] bytes;
            string fileName;

            if (reportType == "excel")
            {
                var submissions = await _context.Submissions
                    .Include(s => s.Contest).ThenInclude(c => c.Category)
                    .Include(s => s.User)
                    .Include(s => s.JuryAssessments)
                    .Where(s => s.IdModLog != null && s.ModeratorLog != null && s.ModeratorLog.Status == "Одобрено")
                    .ToListAsync();

                submissions = submissions
                    .OrderByDescending(s => s.Contest?.Title)
                    .ThenByDescending(s => s.TotalScore)
                    .ToList();

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Конкурс;Категория;Работа;Автор;Средний балл;Кол-во оценок");
                foreach (var s in submissions)
                {
                    var contest = EscapeCsvField(s.Contest?.Title);
                    var category = EscapeCsvField(s.Contest?.Category?.CategoryName);
                    var title = EscapeCsvField(s.Title);
                    var author = EscapeCsvField(s.User?.Login);
                    var scoreVal = s.TotalScore.HasValue ? Math.Round(s.TotalScore.Value, 2) : (double?)null;
                    var score = scoreVal?.ToString("F2", new System.Globalization.CultureInfo("ru-RU")) ?? "—";
                    var count = s.JuryAssessments?.Count ?? 0;
                    sb.AppendLine($"{contest};{category};{title};{author};{score};{count}");
                }

                bytes = GetCsvBytesWithBom(sb.ToString());
                fileName = "svodnaya_vedomost.csv";
            }
            else if (reportType == "winners")
            {
                var submissions = await _context.Submissions
                    .Include(s => s.Contest).ThenInclude(c => c.Category)
                    .Include(s => s.User)
                    .Where(s => s.TotalScore.HasValue)
                    .ToListAsync();

                var winners = submissions
                    .GroupBy(s => new { s.IdContest, s.Contest.Title, s.Contest.Category.CategoryName })
                    .SelectMany(g => g.OrderByDescending(s => s.TotalScore).Take(3).Select((s, i) => new
                    {
                        Contest = g.Key.Title ?? "",
                        Category = g.Key.CategoryName ?? "",
                        Submission = s.Title,
                        Author = s.User?.Login ?? "",
                        Score = Math.Round(s.TotalScore ?? 0, 2),
                        Place = i + 1
                    }))
                    .OrderBy(r => r.Contest).ThenBy(r => r.Category).ThenBy(r => r.Place)
                    .ToList();

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Конкурс;Категория;Место;Работа;Автор;Балл");
                foreach (var w in winners)
                {
                    var contest = EscapeCsvField(w.Contest);
                    var category = EscapeCsvField(w.Category);
                    var submission = EscapeCsvField(w.Submission);
                    var author = EscapeCsvField(w.Author);
                    var score = w.Score.ToString("F2", new System.Globalization.CultureInfo("ru-RU"));
                    sb.AppendLine($"{contest};{category};{w.Place};{submission};{author};{score}");
                }

                bytes = GetCsvBytesWithBom(sb.ToString());
                fileName = "reestr_pobediteley.csv";
            }
            else
            {
                return BadRequest();
            }

            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        private static string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field))
                return "\"\"";
            if (field.Contains(';') || field.Contains('"') || field.Contains('\n'))
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            return "\"" + field + "\"";
        }

        private static byte[] GetCsvBytesWithBom(string content)
        {
            var preamble = System.Text.Encoding.UTF8.GetPreamble();
            var body = System.Text.Encoding.UTF8.GetBytes(content);
            var result = new byte[preamble.Length + body.Length];
            Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
            Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
            return result;
        }

        public async Task<IActionResult> Users(string search = "", string role = "", string sort = "new")
        {
            var auth = RequireAdmin();
            if (auth != null) return auth;
            SetAdminLogin();
            var query = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Region)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Login.Contains(search) || u.Email.Contains(search));
            }

            if (!string.IsNullOrEmpty(role) && int.TryParse(role, out int roleId))
            {
                query = query.Where(u => u.IdRole == roleId);
            }

            query = sort switch
            {
                "old" => query.OrderBy(u => u.Id),
                _ => query.OrderByDescending(u => u.Id)
            };

            var users = await query.ToListAsync();
            var roles = await _context.Roles.ToListAsync();

            var viewModel = new AdminUsersViewModel
            {
                Users = users,
                Roles = roles,
                Search = search,
                SelectedRole = role,
                Sort = sort
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeUserRole(int userId, int newRoleId)
        {
            if (GetCurrentUserId() == null || !IsAdmin())
                return Unauthorized();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return BadRequest();
            }

            var currentAdminId = HttpContext.Session.GetString("UserLogin");
            if (user.Login == currentAdminId)
            {
                return BadRequest();
            }

            if (newRoleId < 1 || newRoleId > 4)
            {
                return BadRequest();
            }

            user.IdRole = newRoleId;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            if (GetCurrentUserId() == null || !IsAdmin())
                return Unauthorized();
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.IdRole == 1)
            {
                return BadRequest();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok();
        }

        public async Task<IActionResult> Contests()
        {
            var auth = RequireAdmin();
            if (auth != null) return auth;
            SetAdminLogin();
            var contests = await _context.Contests
                .Include(c => c.Category)
                .Include(c => c.Stage)
                .Include(c => c.ApplicationPeriod)
                .Include(c => c.JudgingPeriod)
                .Where(c => c.IdStage != 4)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            var stages = await _context.Stages.ToListAsync();

            var viewModel = new AdminContestsViewModel
            {
                Contests = contests,
                Stages = stages
            };

            return View(viewModel);
        }

        public async Task<IActionResult> CreateContest()
        {
            var auth = RequireAdmin();
            if (auth != null) return auth;
            SetAdminLogin();
            var viewModel = new AdminCreateContestViewModel
            {
                Categories = await _context.ContestCategories.ToListAsync(),
                Criteria1List = await _context.Criteria1.ToListAsync(),
                Criteria2List = await _context.Criteria2.ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateContest(
            string title, string rules, int category, int criteria1, int criteria2,
            string appStartDate, string appEndDate, string judStartDate, string judEndDate,
            IFormFile contestImage)
        {
            var auth = RequireAdmin();
            if (auth != null) return auth;
            SetAdminLogin();

            if (category <= 0 || !await _context.ContestCategories.AnyAsync(c => c.Id == category))
            {
                ViewBag.Error = "Выберите корректную категорию";
                return await CreateContestGet();
            }

            if (criteria1 <= 0 || !await _context.Criteria1.AnyAsync(c => c.Id == criteria1))
            {
                ViewBag.Error = "Выберите корректный критерий оценки 1";
                return await CreateContestGet();
            }

            if (criteria2 <= 0 || !await _context.Criteria2.AnyAsync(c => c.Id == criteria2))
            {
                ViewBag.Error = "Выберите корректный критерий оценки 2";
                return await CreateContestGet();
            }

            if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 3)
            {
                ViewBag.Error = "Название конкурса должно содержать минимум 3 символа";
                return await CreateContestGet();
            }

            if (title.Trim().Length > 200)
            {
                ViewBag.Error = "Название конкурса не может превышать 200 символов";
                return await CreateContestGet();
            }

            if (string.IsNullOrWhiteSpace(rules) || rules.Trim().Length < 10)
            {
                ViewBag.Error = "Правила и описание должны содержать минимум 10 символов";
                return await CreateContestGet();
            }

            if (rules.Trim().Length > 5000)
            {
                ViewBag.Error = "Правила и описание не могут превышать 5000 символов";
                return await CreateContestGet();
            }

            if (!DateTime.TryParse(appStartDate, out var appStart) ||
                !DateTime.TryParse(appEndDate, out var appEnd) ||
                !DateTime.TryParse(judStartDate, out var judStart) ||
                !DateTime.TryParse(judEndDate, out var judEnd))
            {
                ViewBag.Error = "Укажите корректные даты";
                return await CreateContestGet();
            }

            var today = DateTime.Now.Date;
            if (appStart < today)
            {
                ViewBag.Error = "Дата начала приёма заявок не может быть в прошлом";
                return await CreateContestGet();
            }

            if (appEnd <= appStart)
            {
                ViewBag.Error = "Дата окончания приёма заявок должна быть позже даты начала";
                return await CreateContestGet();
            }

            var appDays = (appEnd - appStart).TotalDays;
            if (appDays < 1)
            {
                ViewBag.Error = "Период приёма заявок должен быть минимум 1 день";
                return await CreateContestGet();
            }
            if (appDays > 365)
            {
                ViewBag.Error = "Период приёма заявок не может превышать 365 дней";
                return await CreateContestGet();
            }

            if (judStart <= appEnd)
            {
                ViewBag.Error = "Этап судейства должен начинаться после окончания приёма заявок";
                return await CreateContestGet();
            }

            var gapDays = (judStart - appEnd).TotalDays;
            if (gapDays > 1)
            {
                ViewBag.Error = "Между окончанием приёма заявок и началом судейства должен быть максимум 1 день";
                return await CreateContestGet();
            }

            if (judEnd <= judStart)
            {
                ViewBag.Error = "Дата окончания судейства должна быть позже даты начала";
                return await CreateContestGet();
            }

            var judDays = (judEnd - judStart).TotalDays;
            if (judDays < 1)
            {
                ViewBag.Error = "Период судейства должен быть минимум 1 день";
                return await CreateContestGet();
            }
            if (judDays > 30)
            {
                ViewBag.Error = "Период судейства не может превышать 30 дней";
                return await CreateContestGet();
            }

            if (contestImage == null || contestImage.Length == 0)
            {
                ViewBag.Error = "Загрузите изображение конкурса";
                return await CreateContestGet();
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(contestImage.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
            {
                ViewBag.Error = "Допустимые форматы изображения: JPG, JPEG, PNG";
                return await CreateContestGet();
            }

            if (contestImage.Length > 10 * 1024 * 1024)
            {
                ViewBag.Error = "Размер изображения не может превышать 10 МБ";
                return await CreateContestGet();
            }

            string imagePath;
            var fileName = Guid.NewGuid().ToString() + ext;
            var filePath = Path.Combine("wwwroot", "images", "contests", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await contestImage.CopyToAsync(stream);
            }
            imagePath = "contests/" + fileName;

            var appPeriod = new ApplicationPeriod
            {
                AppStartDate = appStartDate,
                AppEndDate = appEndDate
            };
            _context.ApplicationPeriods.Add(appPeriod);
            await _context.SaveChangesAsync();

            var judPeriod = new JudgingPeriod
            {
                JudStartDate = judStartDate,
                JudEndDate = judEndDate
            };
            _context.JudgingPeriods.Add(judPeriod);
            await _context.SaveChangesAsync();

            var contest = new Contest
            {
                Title = title.Trim(),
                Rules = rules.Trim(),
                ContestImage = imagePath,
                IdCategory = category,
                IdStage = 1,
                IdAppPeriod = appPeriod.Id,
                IdJudPeriod = judPeriod.Id,
                IdCriteria1 = criteria1,
                IdCriteria2 = criteria2
            };

            _context.Contests.Add(contest);
            await _context.SaveChangesAsync();

            return RedirectToAction("Contests");
        }

        private async Task<IActionResult> CreateContestGet()
        {
            SetAdminLogin();
            var viewModel = new AdminCreateContestViewModel
            {
                Categories = await _context.ContestCategories.ToListAsync(),
                Criteria1List = await _context.Criteria1.ToListAsync(),
                Criteria2List = await _context.Criteria2.ToListAsync()
            };
            return View(viewModel);
        }

        public async Task<IActionResult> EditContest(int id)
        {
            var auth = RequireAdmin();
            if (auth != null) return auth;
            SetAdminLogin();
            var contest = await _context.Contests
                .Include(c => c.ApplicationPeriod)
                .Include(c => c.JudgingPeriod)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contest == null)
            {
                return RedirectToAction("Contests");
            }

            var now = DateTime.Now.Date;
            var appStartPassed = contest.ApplicationPeriod != null && DateTime.Parse(contest.ApplicationPeriod.AppStartDate) < now;
            var appEndPassed = contest.ApplicationPeriod != null && DateTime.Parse(contest.ApplicationPeriod.AppEndDate) < now;
            var judStartPassed = contest.JudgingPeriod != null && DateTime.Parse(contest.JudgingPeriod.JudStartDate) < now;

            var viewModel = new AdminEditContestViewModel
            {
                Contest = contest,
                Categories = await _context.ContestCategories.ToListAsync(),
                Criteria1List = await _context.Criteria1.ToListAsync(),
                Criteria2List = await _context.Criteria2.ToListAsync(),
                AppStartPassed = appStartPassed,
                AppEndPassed = appEndPassed,
                JudStartPassed = judStartPassed
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditContest(
            int id, string title, string rules, int category, int criteria1, int criteria2,
            string appStartDate, string appEndDate, string judStartDate, string judEndDate,
            IFormFile contestImage)
        {
            var auth = RequireAdmin();
            if (auth != null) return auth;
            SetAdminLogin();

            var contest = await _context.Contests
                .Include(c => c.ApplicationPeriod)
                .Include(c => c.JudgingPeriod)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contest == null)
            {
                return RedirectToAction("Contests");
            }

            if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 3)
            {
                ViewBag.Error = "Название конкурса должно содержать минимум 3 символа";
                return await EditContestGet(id);
            }

            if (title.Trim().Length > 200)
            {
                ViewBag.Error = "Название конкурса не может превышать 200 символов";
                return await EditContestGet(id);
            }

            if (string.IsNullOrWhiteSpace(rules) || rules.Trim().Length < 10)
            {
                ViewBag.Error = "Правила и описание должны содержать минимум 10 символов";
                return await EditContestGet(id);
            }

            if (rules.Trim().Length > 5000)
            {
                ViewBag.Error = "Правила и описание не могут превышать 5000 символов";
                return await EditContestGet(id);
            }

            bool appPeriodEditable = contest.IdStage == 1;
            bool judPeriodEditable = contest.IdStage <= 2;

            DateTime appStart, appEnd, judStart, judEnd;

            if (appPeriodEditable)
            {
                if (!DateTime.TryParse(appStartDate, out appStart) ||
                    !DateTime.TryParse(appEndDate, out appEnd))
                {
                    ViewBag.Error = "Укажите корректные даты приёма заявок";
                    return await EditContestGet(id);
                }

                var today = DateTime.Now.Date;
                if (appStart < today)
                {
                    ViewBag.Error = "Дата начала приёма заявок не может быть в прошлом";
                    return await EditContestGet(id);
                }

                if (appEnd <= appStart)
                {
                    ViewBag.Error = "Дата окончания приёма заявок должна быть позже даты начала";
                    return await EditContestGet(id);
                }

                var appDays = (appEnd - appStart).TotalDays;
                if (appDays < 1 || appDays > 365)
                {
                    ViewBag.Error = "Период приёма заявок должен быть от 1 до 365 дней";
                    return await EditContestGet(id);
                }
            }
            else
            {
                appStart = DateTime.Parse(contest.ApplicationPeriod!.AppStartDate);
                appEnd = DateTime.Parse(contest.ApplicationPeriod.AppEndDate);
            }

            if (judPeriodEditable)
            {
                if (!DateTime.TryParse(judStartDate, out judStart) ||
                    !DateTime.TryParse(judEndDate, out judEnd))
                {
                    ViewBag.Error = "Укажите корректные даты судейства";
                    return await EditContestGet(id);
                }

                if (judStart <= appEnd)
                {
                    ViewBag.Error = "Этап судейства должен начинаться после окончания приёма заявок";
                    return await EditContestGet(id);
                }

                if (judEnd <= judStart)
                {
                    ViewBag.Error = "Дата окончания судейства должна быть позже даты начала";
                    return await EditContestGet(id);
                }

                var judDays = (judEnd - judStart).TotalDays;
                if (judDays < 1 || judDays > 30)
                {
                    ViewBag.Error = "Период судейства должен быть от 1 до 30 дней";
                    return await EditContestGet(id);
                }
            }
            else
            {
                judStart = DateTime.Parse(contest.JudgingPeriod!.JudStartDate);
                judEnd = DateTime.Parse(contest.JudgingPeriod.JudEndDate);
            }

            if (category <= 0 || !await _context.ContestCategories.AnyAsync(c => c.Id == category))
            {
                ViewBag.Error = "Выберите корректную категорию";
                return await EditContestGet(id);
            }

            if (criteria1 <= 0 || !await _context.Criteria1.AnyAsync(c => c.Id == criteria1))
            {
                ViewBag.Error = "Выберите корректный критерий оценки 1";
                return await EditContestGet(id);
            }

            if (criteria2 <= 0 || !await _context.Criteria2.AnyAsync(c => c.Id == criteria2))
            {
                ViewBag.Error = "Выберите корректный критерий оценки 2";
                return await EditContestGet(id);
            }

            if (appPeriodEditable && judPeriodEditable)
            {
                var gapDays = (judStart - appEnd).TotalDays;
                if (gapDays > 1)
                {
                    ViewBag.Error = "Между окончанием приёма заявок и началом судейства должен быть максимум 1 день";
                    return await EditContestGet(id);
                }
            }

            contest.Title = title.Trim();
            contest.Rules = rules.Trim();
            contest.IdCategory = category;
            contest.IdCriteria1 = criteria1;
            contest.IdCriteria2 = criteria2;

            if (appPeriodEditable)
            {
                contest.ApplicationPeriod!.AppStartDate = appStartDate;
                contest.ApplicationPeriod.AppEndDate = appEndDate;
            }

            if (judPeriodEditable)
            {
                contest.JudgingPeriod!.JudStartDate = judStartDate;
                contest.JudgingPeriod.JudEndDate = judEndDate;
            }

            if (contestImage != null && contestImage.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(contestImage.FileName).ToLower();
                if (!allowedExtensions.Contains(ext))
                {
                    ViewBag.Error = "Допустимые форматы изображения: JPG, JPEG, PNG";
                    return await EditContestGet(id);
                }

                if (contestImage.Length > 10 * 1024 * 1024)
                {
                    ViewBag.Error = "Размер изображения не может превышать 10 МБ";
                    return await EditContestGet(id);
                }

                var fileName = Guid.NewGuid().ToString() + ext;
                var filePath = Path.Combine("wwwroot", "images", "contests", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await contestImage.CopyToAsync(stream);
                }
                contest.ContestImage = "contests/" + fileName;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Contests");
        }

        private async Task<IActionResult> EditContestGet(int id)
        {
            SetAdminLogin();
            var contest = await _context.Contests
                .Include(c => c.ApplicationPeriod)
                .Include(c => c.JudgingPeriod)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contest == null)
            {
                return RedirectToAction("Contests");
            }

            var viewModel = new AdminEditContestViewModel
            {
                Contest = contest,
                Categories = await _context.ContestCategories.ToListAsync(),
                Criteria1List = await _context.Criteria1.ToListAsync(),
                Criteria2List = await _context.Criteria2.ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteContest(int id)
        {
            if (GetCurrentUserId() == null || !IsAdmin())
                return Unauthorized();
            var contest = await _context.Contests.FindAsync(id);
            if (contest != null)
            {
                _context.Contests.Remove(contest);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Contests");
        }
    }

    public class AdminAnalyticsViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveContests { get; set; }
        public int SubmissionsOnModeration { get; set; }
        public List<JuryScoreReportItem> JuryScoreReport { get; set; } = new();
        public List<WinnerReportItem> WinnersReport { get; set; } = new();
        public List<ActivityByRegionItem> ActivityByRegion { get; set; } = new();
        public List<ActivityByAgeItem> ActivityByAge { get; set; } = new();
        public List<SubmissionsByDayItem> SubmissionsByDay { get; set; } = new();
        public List<CategoryPopularityItem> CategoryPopularity { get; set; } = new();
        public List<Contest> Contests { get; set; } = new();
    }

    public class JuryScoreReportItem
    {
        public string ContestTitle { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string SubmissionTitle { get; set; } = "";
        public string AuthorLogin { get; set; } = "";
        public double? TotalScore { get; set; }
        public int JuryCount { get; set; }
    }

    public class WinnerReportItem
    {
        public string ContestTitle { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string SubmissionTitle { get; set; } = "";
        public string AuthorLogin { get; set; } = "";
        public double? TotalScore { get; set; }
        public int Place { get; set; }
    }

    public class ActivityByRegionItem
    {
        public string RegionName { get; set; } = "";
        public int SubmissionCount { get; set; }
    }

    public class ActivityByAgeItem
    {
        public string AgeGroup { get; set; } = "";
        public int SubmissionCount { get; set; }
    }

    public class SubmissionsByDayItem
    {
        public string Date { get; set; } = "";
        public int Count { get; set; }
    }

    public class CategoryPopularityItem
    {
        public string CategoryName { get; set; } = "";
        public int SubmissionCount { get; set; }
        public int ContestCount { get; set; }
    }

    public class AdminUsersViewModel
    {
        public List<User> Users { get; set; } = new();
        public List<Role> Roles { get; set; } = new();
        public string Search { get; set; } = "";
        public string SelectedRole { get; set; } = "";
        public string Sort { get; set; } = "new";
    }

    public class AdminContestsViewModel
    {
        public List<Contest> Contests { get; set; } = new();
        public List<Stage> Stages { get; set; } = new();
    }

    public class AdminCreateContestViewModel
    {
        public List<ContestCategory> Categories { get; set; } = new();
        public List<Criteria1> Criteria1List { get; set; } = new();
        public List<Criteria2> Criteria2List { get; set; } = new();
    }

    public class AdminEditContestViewModel
    {
        public Contest Contest { get; set; } = null!;
        public List<ContestCategory> Categories { get; set; } = new();
        public List<Criteria1> Criteria1List { get; set; } = new();
        public List<Criteria2> Criteria2List { get; set; } = new();
        public bool AppStartPassed { get; set; }
        public bool AppEndPassed { get; set; }
        public bool JudStartPassed { get; set; }
    }
}
