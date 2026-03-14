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

        public async Task<IActionResult> Online()
        {
            // Fetch all questions for the Online Survey (Type 2)
            var questions = await _context.Questions
                .Where(q => q.is_active && q.survey_types_id == 2)
                .OrderBy(q => q.survey_categories_id) // Order by category so steps make sense
                .ThenBy(q => q.id)
                .ToListAsync();

            // Send the list to the View
            ViewBag.Questions = questions;
            ViewBag.Technicians = await _context.Technicians.ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitOnline(Dictionary<int, string>? ratings, Dictionary<int, string>? techRatings)
        {
            ratings ??= new Dictionary<int, string>();
            techRatings ??= new Dictionary<int, string>();

            // 1. Create the main submission header (Survey Type 2 = Online)
            var submission = new SurveySubmission 
            { 
                survey_types_id = 2, 
                submitted_at = DateTime.Now 
            };
            _context.SurveySubmissions.Add(submission);
            await _context.SaveChangesAsync();

            // 2. Fetch all question metadata to get categories automatically
            var questionIds = ratings.Keys.ToList();
            var questions = await _context.Questions
                .Where(q => questionIds.Contains(q.id))
                .ToListAsync();

            var technicianQuestion = await _context.Questions
                .FirstOrDefaultAsync(q => q.question_body.Contains("Teknisi") || q.input_type.Contains("NPS"));

            // 3. Loop through the submitted data
            foreach (var rating in ratings)
            {
                var question = questions.FirstOrDefault(q => q.id == rating.Key);
                
                if (question != null)
                {
                    var response = new QuestionResponse
                    {
                        questions_id = rating.Key,
                        survey_submissions_id = submission.id,
                        survey_categories_id = question.survey_categories_id,
                        // Check if it's a numeric rating or text response
                        rating_score = int.TryParse(rating.Value, out int score) ? score : (int?)null,
                        text_response = !int.TryParse(rating.Value, out _) ? rating.Value : null
                    };
                    _context.QuestionResponses.Add(response);
                }
            }

            foreach (var techRating in techRatings)
            {
                if (!int.TryParse(techRating.Value, out var technicianScore))
                {
                    continue;
                }

                if (technicianQuestion == null)
                {
                    continue;
                }

                var response = new QuestionResponse
                {
                    questions_id = technicianQuestion.id,
                    survey_submissions_id = submission.id,
                    survey_categories_id = technicianQuestion.survey_categories_id,
                    technicians_id = techRating.Key,
                    rating_score = technicianScore
                };

                _context.QuestionResponses.Add(response);
            }

            await _context.SaveChangesAsync();
            
            // 4. Redirect to a Thank You page
            return RedirectToAction("OnlineThankYou");
        }

        public IActionResult OnlineThankYou()
        {
            return View();
        }
    }
}