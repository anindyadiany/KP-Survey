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
        // 1. Fetch counts for the stat cards
        ViewBag.TotalSurvey = await _context.SurveySubmissions.CountAsync();
        ViewBag.TotalOnline = await _context.SurveySubmissions.CountAsync(s => s.survey_types_id == 2);
        ViewBag.TotalWalkIn = await _context.SurveySubmissions.CountAsync(s => s.survey_types_id == 1);

        // 2. Fetch top 4 technicians ordered by ID for consistency
        var topTechs = await _context.Technicians
            .OrderBy(t => t.id) 
            .Take(4)
            .ToListAsync();

        return View(topTechs);
    }
    // For Online (Type 2)
    public async Task<IActionResult> EditSurveyOnline()
    {
        var questions = await _context.Questions
            .Include(q => q.SurveyCategory)
            .Where(q => q.survey_types_id == 2)
            .ToListAsync();
        
        return View(questions);
    }
 
}