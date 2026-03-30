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

        // GET: Survey/Identitas
        public IActionResult Identitas(string? returnTo)
        {
            var normalizedReturnTo = (returnTo ?? string.Empty).Trim().ToLowerInvariant();
            ViewBag.ReturnTo = normalizedReturnTo == "walkin" ? "walkin" : "online";
            return View();
        }

        // POST: Survey/StartSurvey
        [HttpPost]
        public async Task<IActionResult> StartSurvey(string respondent_name, string respondent_email, string respondent_function, string returnTo)
        {
            if (string.IsNullOrWhiteSpace(respondent_name) ||
                string.IsNullOrWhiteSpace(respondent_email) ||
                string.IsNullOrWhiteSpace(respondent_function))
            {
                TempData["FormError"] = "Harap lengkapi semua pertanyaan sebelum lanjut.";
                return RedirectToAction("Identitas", new { returnTo });
            }

            var normalizedReturnTo = (returnTo ?? string.Empty).Trim().ToLowerInvariant();
            if (normalizedReturnTo != "walkin" && normalizedReturnTo != "online")
            {
                TempData["FormError"] = "Tujuan survey tidak valid.";
                return RedirectToAction("Identitas");
            }

            var selectedSurveyTypeId = normalizedReturnTo == "walkin" ? 1 : 2;

            // Create the submission record early to store the identity
            var submission = new SurveySubmission
            {
                respondent_name = respondent_name,
                respondent_email = respondent_email,
                respondent_function = respondent_function,
                survey_types_id = selectedSurveyTypeId,
                submitted_at = DateTime.Now
            };

            _context.SurveySubmissions.Add(submission);
            await _context.SaveChangesAsync();

            // Pass the submission ID to selected survey page
            return selectedSurveyTypeId == 1
                ? RedirectToAction("Walkin", new { submissionId = submission.id })
                : RedirectToAction("Online", new { submissionId = submission.id });
        }

        // GET: Survey/Walkin
        public async Task<IActionResult> Walkin(int? submissionId)
        {
            if (!submissionId.HasValue ||
                !await _context.SurveySubmissions.AnyAsync(s => s.id == submissionId.Value && s.survey_types_id == 1))
            {
                TempData["FormError"] = "Isi data identitas terlebih dahulu untuk memulai survey Walk-in.";
                return RedirectToAction("Identitas", new { returnTo = "walkin" });
            }

            ViewBag.Services = await _context.Services.ToListAsync();
            ViewBag.Technicians = await _context.Technicians.ToListAsync();
            ViewBag.SubmissionId = submissionId.Value;
            
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
        public async Task<IActionResult> SubmitWalkin(int submissionId, int serviceId, int technicianId, Dictionary<int, int> ratings)
        {
            var submission = await _context.SurveySubmissions
                .FirstOrDefaultAsync(s => s.id == submissionId && s.survey_types_id == 1);

            if (submission == null)
            {
                TempData["FormError"] = "Isi data identitas terlebih dahulu untuk memulai survey Walk-in.";
                return RedirectToAction("Identitas", new { returnTo = "walkin" });
            }

            if (serviceId <= 0 || technicianId <= 0 || ratings == null || ratings.Count == 0 || ratings.Any(r => r.Value <= 0))
            {
                TempData["FormError"] = "Harap lengkapi semua pertanyaan sebelum kirim.";
                return RedirectToAction("Walkin", new { submissionId });
            }

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

        public async Task<IActionResult> Online(int? submissionId)
        {
            if (!submissionId.HasValue ||
                !await _context.SurveySubmissions.AnyAsync(s => s.id == submissionId.Value && s.survey_types_id == 2))
            {
                TempData["FormError"] = "Isi data identitas terlebih dahulu untuk memulai survey Online.";
                return RedirectToAction("Identitas", new { returnTo = "online" });
            }

            // Fetch all questions for the Online Survey (Type 2)
            var questions = await _context.Questions
                .Where(q => q.is_active && q.survey_types_id == 2)
                .OrderBy(q => q.survey_categories_id) // Order by category so steps make sense
                .ThenBy(q => q.id)
                .ToListAsync();

            // Send the list to the View
            ViewBag.Questions = questions;
            ViewBag.Technicians = await _context.Technicians.ToListAsync();
            ViewBag.SubmissionId = submissionId.Value;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitOnline(int submissionId, Dictionary<int, string>? ratings, Dictionary<int, string>? techRatings)
        {
            var submission = await _context.SurveySubmissions
                .FirstOrDefaultAsync(s => s.id == submissionId && s.survey_types_id == 2);

            if (submission == null)
            {
                TempData["FormError"] = "Isi data identitas terlebih dahulu untuk memulai survey Online.";
                return RedirectToAction("Identitas", new { returnTo = "online" });
            }

            ratings ??= new Dictionary<int, string>();
            techRatings ??= new Dictionary<int, string>();

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