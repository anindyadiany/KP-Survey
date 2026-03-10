using Microsoft.AspNetCore.Mvc;

namespace SurveyApp.Controllers
{
    public class SurveyController : Controller
    {
        // This method handles the "Walkin" URL
        public IActionResult Walkin()
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

        public IActionResult WalkinThankYou()
        {
            return View();
        }
    }
}