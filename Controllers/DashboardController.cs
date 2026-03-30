using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyApp.Data;
using SurveyApp.Models;

namespace SurveyApp.Controllers;

public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalSurvey = await _context.SurveySubmissions.CountAsync();
        ViewBag.TotalOnline = await _context.SurveySubmissions.CountAsync(s => s.survey_types_id == 2);
        ViewBag.TotalWalkIn = await _context.SurveySubmissions.CountAsync(s => s.survey_types_id == 1);
        ViewBag.ServiceMap = await _context.Services.ToDictionaryAsync(s => s.id, s => s.name);

        var topTechs = await _context.Technicians
            .OrderBy(t => t.id)
            .Take(4)
            .ToListAsync();

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
        ViewBag.Services = await _context.Services.ToDictionaryAsync(s => s.id, s => s.name);
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
    public async Task<IActionResult> SaveTeknisi(int id, string name, string email, int service_id, string photo_path)
    {
        if (id == 0)
        {
            _context.Technicians.Add(new Technician
            {
                name = name,
                email = email,
                service_id = service_id,
                photo_path = photo_path ?? ""
            });
        }
        else
        {
            var tech = await _context.Technicians.FindAsync(id);
            if (tech != null)
            {
                tech.name = name;
                tech.email = email;
                tech.service_id = service_id;
                tech.photo_path = photo_path ?? "";
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = id == 0 ? "Teknisi berhasil ditambahkan." : "Teknisi berhasil diperbarui.";
        return RedirectToAction("EditTeknisi");
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



    public async Task<IActionResult> OnlineResponses(string tab = "online", int month = 0, int year = 0)
    {
        return await Responses(tab, month, year);
    }

    public async Task<IActionResult> Responses(string tab = "online", int month = 0, int year = 0)
    {
        int surveyTypeId = tab == "walkin" ? 1 : 2;

        // Base query for submissions filtered by type + optional month/year
        var submissionsQuery = _context.SurveySubmissions
            .Where(s => s.survey_types_id == surveyTypeId);

        if (month > 0)
            submissionsQuery = submissionsQuery.Where(s => s.submitted_at.Month == month);
        if (year > 0)
            submissionsQuery = submissionsQuery.Where(s => s.submitted_at.Year == year);

        var submissionIds = await submissionsQuery.Select(s => s.id).ToListAsync();

        // Questions for this survey type
        var questions = await _context.Questions
            .Include(q => q.SurveyCategory)
            .Where(q => q.survey_types_id == surveyTypeId && q.is_active)
            .OrderBy(q => q.survey_categories_id).ThenBy(q => q.id)
            .ToListAsync();

        // Categories
        var categoryIds = questions.Select(q => q.survey_categories_id).Distinct().ToList();
        var categories = await _context.SurveyCategories
            .Where(c => categoryIds.Contains(c.id))
            .OrderBy(c => c.id)
            .ToListAsync();

        // Chart data: for each likert/nps question, count responses per rating value
        var ratingQuestionIds = questions
            .Where(q => q.input_type == "likert_5" || q.input_type == "agreement_5" || q.input_type == "nps_10")
            .Select(q => q.id).ToList();

        var ratingResponses = await _context.QuestionResponses
            .Where(r => submissionIds.Contains(r.survey_submissions_id)
                    && ratingQuestionIds.Contains(r.questions_id)
                    && r.rating_score != null)
            .ToListAsync();

        // chartData[questionId][score] = count
        var chartData = new Dictionary<int, Dictionary<int, int>>();
        foreach (var qid in ratingQuestionIds)
        {
            chartData[qid] = ratingResponses
                .Where(r => r.questions_id == qid)
                .GroupBy(r => r.rating_score!.Value)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        // NPS scores per technician (average of techRating responses)
        var techResponses = await _context.QuestionResponses
            .Where(r => submissionIds.Contains(r.survey_submissions_id)
                    && r.technicians_id != null
                    && r.rating_score != null)
            .ToListAsync();

        var npsScores = techResponses
            .GroupBy(r => r.technicians_id!.Value)
            .ToDictionary(g => g.Key, g => g.Average(r => r.rating_score!.Value));

        // Technicians
        var techIds = npsScores.Keys.ToList();
        var technicians = await _context.Technicians
            .Where(t => techIds.Contains(t.id))
            .ToListAsync();

        // Text feedbacks
        var feedbackQuestionIds = questions
            .Where(q => q.input_type == "text")
            .Select(q => q.id).ToList();

        var feedbacks = await _context.QuestionResponses
            .Where(r => submissionIds.Contains(r.survey_submissions_id)
                    && feedbackQuestionIds.Contains(r.questions_id)
                    && r.text_response != null && r.text_response != "")
            .Select(r => r.text_response!)
            .ToListAsync();

        // Category colors: first category = blue, second = red, rest = default blue
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

        return View("Responses");
    }

}
