using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SurveyApp.Data;
using SurveyApp.Models;
using System.IO;

namespace SurveyApp.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public DashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalSurvey = await _context.SurveySubmissions.CountAsync();
        ViewBag.TotalOnline = await _context.SurveySubmissions.CountAsync(s => s.survey_types_id == 2);
        ViewBag.TotalWalkIn = await _context.SurveySubmissions.CountAsync(s => s.survey_types_id == 1);
        ViewBag.ServiceMap = await _context.Services.ToDictionaryAsync(s => s.id, s => s.name);

        // Chart: count submissions per month for current year, split by type
        var currentYear = DateTime.Now.Year;
        var allSubmissions = await _context.SurveySubmissions
            .Where(s => s.submitted_at.Year == currentYear)
            .ToListAsync();

        var monthlyWalkIn = Enumerable.Range(1, 12)
            .Select(m => allSubmissions.Count(s => s.survey_types_id == 1 && s.submitted_at.Month == m))
            .ToList();

        var monthlyOnline = Enumerable.Range(1, 12)
            .Select(m => allSubmissions.Count(s => s.survey_types_id == 2 && s.submitted_at.Month == m))
            .ToList();

        ViewBag.MonthlyWalkIn = monthlyWalkIn;
        ViewBag.MonthlyOnline = monthlyOnline;

        // NPS scores
        var npsScores = await _context.QuestionResponses
            .Where(r => r.technicians_id != null && r.rating_score != null)
            .GroupBy(r => r.technicians_id!.Value)
            .ToDictionaryAsync(g => g.Key, g => g.Average(r => r.rating_score!.Value));

        var topTechIds = npsScores
            .OrderByDescending(kvp => kvp.Value)
            .Take(4)
            .Select(kvp => kvp.Key)
            .ToList();

        var topTechs = await _context.Technicians
            .Where(t => topTechIds.Contains(t.id))
            .ToListAsync();

        topTechs = topTechs
            .OrderByDescending(t => npsScores.ContainsKey(t.id) ? npsScores[t.id] : 0)
            .ToList();

        ViewBag.NpsScores = npsScores;

        return View(topTechs);
    }

    // ── Edit Survey Online ──────────────────────────────────────────

    public async Task<IActionResult> EditSurveyOnline()
    {
        var questions = await _context.Questions
            .Include(q => q.SurveyCategory)
            .Where(q => q.survey_types_id == 2)
            .ToListAsync();

        return View(questions);
    }

    [HttpPost]
    public async Task<IActionResult> SaveSurveyOnline(
        List<int> questionIds,
        List<string> questionBodies,
        List<int> questionCategoryIds,
        List<string> questionInputTypes,
        List<int>? deletedIds)
    {
        if (questionIds == null || questionIds.Count == 0)
            return RedirectToAction("EditSurveyOnline");

        if (deletedIds != null && deletedIds.Count > 0)
        {
            var toDelete = _context.Questions.Where(q => deletedIds.Contains(q.id));
            _context.Questions.RemoveRange(toDelete);
        }

        for (int i = 0; i < questionIds.Count; i++)
        {
            if (questionIds[i] == 0)
            {
                _context.Questions.Add(new Question
                {
                    question_body = questionBodies[i],
                    survey_categories_id = questionCategoryIds[i],
                    survey_types_id = 2,
                    input_type = questionInputTypes[i],
                    is_active = true
                });
            }
            else
            {
                var q = await _context.Questions.FindAsync(questionIds[i]);
                if (q != null)
                {
                    q.question_body = questionBodies[i];
                    q.input_type = questionInputTypes[i];
                }
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("EditSurveyOnline");
    }

    // ── Edit Survey Walk In ─────────────────────────────────────────

    public async Task<IActionResult> EditSurveyWalkIn()
    {
        var questions = await _context.Questions
            .Include(q => q.SurveyCategory)
            .Where(q => q.survey_types_id == 1)
            .ToListAsync();

        return View(questions);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveSurveyWalkIn(
        List<int> questionIds,
        List<string> questionBodies,
        List<int> questionCategoryIds,
        List<string> questionInputTypes,
        List<int>? deletedIds)
    {
        if (deletedIds != null && deletedIds.Count > 0)
        {
            var toDelete = _context.Questions.Where(q => deletedIds.Contains(q.id));
            _context.Questions.RemoveRange(toDelete);
        }

        for (int i = 0; i < questionIds.Count; i++)
        {
            if (questionIds[i] == 0)
            {
                _context.Questions.Add(new Question
                {
                    question_body = questionBodies[i],
                    survey_categories_id = questionCategoryIds[i],
                    survey_types_id = 1,
                    input_type = questionInputTypes[i],
                    is_active = true
                });
            }
            else
            {
                var q = await _context.Questions.FindAsync(questionIds[i]);
                if (q != null)
                {
                    q.question_body = questionBodies[i];
                    q.input_type = questionInputTypes[i];
                }
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("EditSurveyWalkIn");
    }

    // ── Edit Teknisi ────────────────────────────────────────────────

    public async Task<IActionResult> EditTeknisi()
    {
        var teknisi = await _context.Technicians.OrderBy(t => t.id).ToListAsync();
        return View(teknisi);
    }

    public async Task<IActionResult> EditTeknisiDetail(int id)
    {
        var tech = await _context.Technicians.FindAsync(id);
        if (tech == null) return RedirectToAction("EditTeknisi");

        ViewBag.Services = await _context.Services.OrderBy(s => s.id).ToListAsync();
        return View(tech);
    }

    public async Task<IActionResult> TambahTeknisi()
    {
        ViewBag.Services = await _context.Services.OrderBy(s => s.id).ToListAsync();
        return View();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveTeknisi(int id, string name, string? email, int? service_id, IFormFile? photo_file)
    {
        var existingTech = id == 0 ? null : await _context.Technicians.FindAsync(id);
        var storedPhotoPath = existingTech?.photo_path ?? string.Empty;

        if (photo_file != null && photo_file.Length > 0)
        {
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".webp", ".gif"
            };

            var extension = Path.GetExtension(photo_file.FileName);
            if (!allowedExtensions.Contains(extension))
            {
                TempData["Success"] = "Format foto harus jpg, jpeg, png, webp, atau gif.";
                return RedirectToAction(id == 0 ? nameof(TambahTeknisi) : nameof(EditTeknisiDetail), new { id });
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "assets", "foto_teknisi");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{BuildTechnicianFileName(name)}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await photo_file.CopyToAsync(stream);

            storedPhotoPath = fileName;
        }

        if (id == 0)
        {
            _context.Technicians.Add(new Technician
            {
                name = name,
                email = email,
                service_id = service_id > 0 ? service_id : null,
                photo_path = storedPhotoPath
            });
        }
        else
        {
            if (existingTech != null)
            {
                existingTech.name = name;
                if (!string.IsNullOrWhiteSpace(email))
                {
                    existingTech.email = email;
                }
                if (service_id.HasValue && service_id.Value > 0)
                {
                    existingTech.service_id = service_id;
                }
                existingTech.photo_path = storedPhotoPath;
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = id == 0 ? "Teknisi berhasil ditambahkan." : "Teknisi berhasil diperbarui.";
        return RedirectToAction("EditTeknisi");
    }

    private static string BuildTechnicianFileName(string name)
    {
        var baseName = string.IsNullOrWhiteSpace(name) ? "teknisi" : name.Trim();
        var invalidChars = Path.GetInvalidFileNameChars();
        var cleaned = new string(baseName.Where(character => !invalidChars.Contains(character)).ToArray());
        cleaned = string.Join(" ", cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(cleaned) ? "teknisi" : cleaned;
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DeleteTeknisi(int id)
    {
        var tech = await _context.Technicians.FindAsync(id);
        if (tech != null)
        {
            _context.Technicians.Remove(tech);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Teknisi berhasil dihapus.";
        }
        return RedirectToAction("EditTeknisi");
    }

    // ── Responses ───────────────────────────────────────────────────

    public async Task<IActionResult> OnlineResponses(int month = 0, int year = 0)
    {
        return await BuildResponsesView("ResponsesOnline", 2, "online", month, year);
    }

    public async Task<IActionResult> WalkInResponses(int month = 0, int year = 0)
    {
        return await BuildResponsesView("ResponsesWalkIn", 1, "walkin", month, year);
    }

    public IActionResult Responses(string tab = "online", int month = 0, int year = 0)
    {
        if (tab == "walkin")
            return RedirectToAction(nameof(WalkInResponses), new { month, year });

        return RedirectToAction(nameof(OnlineResponses), new { month, year });
    }

    private async Task<IActionResult> BuildResponsesView(string viewName, int surveyTypeId, string tab, int month, int year)
    {

        var submissionsQuery = _context.SurveySubmissions
            .Where(s => s.survey_types_id == surveyTypeId);

        if (month > 0)
            submissionsQuery = submissionsQuery.Where(s => s.submitted_at.Month == month);
        if (year > 0)
            submissionsQuery = submissionsQuery.Where(s => s.submitted_at.Year == year);

        var submissionIds = await submissionsQuery.Select(s => s.id).ToListAsync();

        var questions = await _context.Questions
            .Include(q => q.SurveyCategory)
            .Where(q => q.survey_types_id == surveyTypeId && q.is_active)
            .OrderBy(q => q.survey_categories_id).ThenBy(q => q.id)
            .ToListAsync();

        var categoryIds = questions.Select(q => q.survey_categories_id).Distinct().ToList();
        var categories = await _context.SurveyCategories
            .Where(c => categoryIds.Contains(c.id))
            .OrderBy(c => c.id)
            .ToListAsync();

        var ratingQuestionIds = questions
            .Where(q => q.input_type == "likert_5" || q.input_type == "agreement_5" || q.input_type == "nps_10")
            .Select(q => q.id).ToList();

        var ratingResponses = await _context.QuestionResponses
            .Where(r => submissionIds.Contains(r.survey_submissions_id)
                    && ratingQuestionIds.Contains(r.questions_id)
                    && r.rating_score != null)
            .ToListAsync();

        var chartData = new Dictionary<int, Dictionary<int, int>>();
        foreach (var qid in ratingQuestionIds)
        {
            chartData[qid] = ratingResponses
                .Where(r => r.questions_id == qid)
                .GroupBy(r => r.rating_score!.Value)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        if (surveyTypeId == 1)
        {
            var satisfactionQuestionIds = questions
                .Where(q => q.input_type == "likert_5" || q.input_type == "agreement_5")
                .Select(q => q.id)
                .ToList();

            var walkInSatisfactionResponses = await _context.QuestionResponses
                .Where(r => submissionIds.Contains(r.survey_submissions_id)
                    && r.service_id != null
                    && r.rating_score != null
                    && satisfactionQuestionIds.Contains(r.questions_id))
                .ToListAsync();

            var submissionServicePairs = await _context.QuestionResponses
                .Where(r => submissionIds.Contains(r.survey_submissions_id) && r.service_id != null)
                .Select(r => new { r.survey_submissions_id, ServiceId = r.service_id!.Value })
                .Distinct()
                .ToListAsync();

            var responseCountByServiceId = submissionServicePairs
                .GroupBy(x => x.ServiceId)
                .ToDictionary(g => g.Key, g => g.Count());

            var satisfactionByServiceId = walkInSatisfactionResponses
                .GroupBy(r => r.service_id!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => Enumerable.Range(1, 5)
                        .Select(score => g.Count(r => r.rating_score == score))
                        .ToList());

            var serviceIds = responseCountByServiceId.Keys
                .Union(satisfactionByServiceId.Keys)
                .Distinct()
                .ToList();

            var walkInServices = await _context.Services
                .Where(s => serviceIds.Contains(s.id))
                .OrderBy(s => s.name)
                .ToListAsync();

            var serviceLabels = walkInServices.Select(s => s.name).ToList();
            var serviceCounts = walkInServices
                .Select(s => responseCountByServiceId.TryGetValue(s.id, out var count) ? count : 0)
                .ToList();

            var overallSatisfactionCounts = Enumerable.Range(1, 5)
                .Select(score => walkInSatisfactionResponses.Count(r => r.rating_score == score))
                .ToList();

            var perServiceSatisfaction = walkInServices
                .ToDictionary(
                    s => s.id,
                    s => satisfactionByServiceId.TryGetValue(s.id, out var counts)
                        ? counts
                        : Enumerable.Repeat(0, 5).ToList());

            ViewBag.ServiceOptions = walkInServices;
            ViewBag.ServiceLabels = serviceLabels;
            ViewBag.ServiceCounts = serviceCounts;
            ViewBag.OverallSatisfactionCounts = overallSatisfactionCounts;
            ViewBag.PerServiceSatisfaction = perServiceSatisfaction;
        }

        List<QuestionResponse> techResponses;

        if (surveyTypeId == 2)
        {
            techResponses = await _context.QuestionResponses
                .Where(r => submissionIds.Contains(r.survey_submissions_id)
                        && r.technicians_id != null
                        && r.rating_score != null)
                .ToListAsync();
        }
        else
        {
            var ratingQIds = questions
                .Where(q => q.input_type == "likert_5"
                        || q.input_type == "agreement_5"
                        || q.input_type == "nps_10")
                .Select(q => q.id).ToList();

            techResponses = await _context.QuestionResponses
                .Where(r => submissionIds.Contains(r.survey_submissions_id)
                        && r.technicians_id != null
                        && r.rating_score != null
                        && ratingQIds.Contains(r.questions_id))
                .ToListAsync();
        }

        var npsScores = techResponses
            .GroupBy(r => r.technicians_id!.Value)
            .ToDictionary(g => g.Key, g => g.Average(r => r.rating_score!.Value));

        var techIds = npsScores.Keys.ToList();
        var technicians = await _context.Technicians
            .Where(t => techIds.Contains(t.id))
            .ToListAsync();

        var feedbackQuestionIds = questions
            .Where(q => q.input_type == "text")
            .Select(q => q.id).ToList();

        var feedbacks = await _context.QuestionResponses
            .Where(r => submissionIds.Contains(r.survey_submissions_id)
                    && feedbackQuestionIds.Contains(r.questions_id)
                    && r.text_response != null && r.text_response != "")
            .Select(r => r.text_response!)
            .ToListAsync();

        var categoryColors = new Dictionary<int, string>();
        var colorList = new[] { "#2a408e", "#e74c3c", "#27ae60", "#f5a623" };
        for (int i = 0; i < categories.Count; i++)
            categoryColors[categories[i].id] = colorList[i % colorList.Length];

        ViewBag.ActiveTab = tab;
        ViewBag.SelectedMonth = month;
        ViewBag.SelectedYear = year;
        ViewBag.Categories = categories;
        ViewBag.Questions = questions;
        ViewBag.ChartData = chartData;
        ViewBag.Technicians = technicians;
        ViewBag.NpsScores = npsScores;
        ViewBag.Feedbacks = feedbacks;
        ViewBag.CategoryColors = categoryColors;

        return View(viewName);
    }

    // ── User ────────────────────────────────────────────────────────

    public IActionResult UserList()
    {
        var users = _userManager.Users.OrderBy(u => u.Email).ToList();
        return View(users);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            await _userManager.DeleteAsync(user);
            TempData["Success"] = "User berhasil dihapus.";
        }
        return RedirectToAction("UserList");
    }
}
