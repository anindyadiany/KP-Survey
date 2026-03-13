using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyApp.Data;
using SurveyApp.Models;
namespace SurveyApp.Controllers
{
    public class SurveyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SurveyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Survey/Walkin
        public async Task<IActionResult> Walkin()
        {
            ViewBag.Services = await _context.Services.ToListAsync();
            ViewBag.Technicians = await _context.Technicians.ToListAsync();
            
            // Fetch only active questions for Walk-in (Survey Type ID 1)
            ViewBag.Questions = await _context.Questions
                .Where(q => q.is_active && q.survey_types_id == 1)
                .OrderBy(q => q.id)
                .ToListAsync();
            
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetTechniciansByService(int serviceId)
        {
            var techs = await _context.Technicians
                .Where(t => t.service_id == serviceId)
                .Select(t => new { t.id, t.name })
                .ToListAsync();
                
            return Json(techs);
        }

        // POST: Survey/SubmitWalkin
        [HttpPost]
        public async Task<IActionResult> SubmitWalkin(int serviceId, int technicianId, Dictionary<int, int> ratings)
        {
            if (serviceId <= 0 || technicianId <= 0 || ratings == null || ratings.Count == 0 || ratings.Any(r => r.Value <= 0))
            {
                TempData["FormError"] = "Harap lengkapi semua field wajib sebelum kirim.";
                return RedirectToAction("Walkin");
            }

            var submission = new SurveySubmission { survey_types_id = 1, submitted_at = DateTime.Now };
            _context.SurveySubmissions.Add(submission);
            await _context.SaveChangesAsync();

            var questionIds = ratings.Keys.ToList();
                var questions = await _context.Questions
                    .Where(q => questionIds.Contains(q.id))
                    .ToListAsync();

                foreach (var rating in ratings)
                {
                    var question = questions.FirstOrDefault(q => q.id == rating.Key);
                    
                    if (question != null)
                    {
                        var response = new QuestionResponse
                        {
                            rating_score = rating.Value,
                            questions_id = rating.Key,
                            survey_submissions_id = submission.id,
                            technicians_id = technicianId,
                            service_id = serviceId,
                            survey_categories_id = question.survey_categories_id 
                        };
                        _context.QuestionResponses.Add(response);
                    }
                }
            
            await _context.SaveChangesAsync();
            return RedirectToAction("WalkinThankYou");
        }

        public IActionResult WalkinThankYou()
        {
            return View();
        }

        // This method handles the "Online" URL
        public IActionResult Online()
        {
            return View();
        }

        // This method handles the next step of the "Online" survey
        public IActionResult OnlineNext()
        {
            return View();
        }

        // This method handles technician selection step of the "Online" survey
        public IActionResult OnlineTechnician()
        {
            return View();
        }

        // This method handles final feedback step of the "Online" survey
        public IActionResult OnlineFinal()
        {
            return View();
        }

        public IActionResult OnlineThankYou()
        {
            return View();
        }
    }
}