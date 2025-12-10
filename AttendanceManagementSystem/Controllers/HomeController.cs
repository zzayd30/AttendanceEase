using System.Diagnostics;
using AttendanceManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace AttendanceManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<IdentityUser> _signInManager;

        // ← ONLY THIS ONE CONSTRUCTOR SHOULD EXIST
        public HomeController(ILogger<HomeController> logger, SignInManager<IdentityUser> signInManager)
        {
            _logger = logger;
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Dashboard");

            //return RedirectToPage("/Identity/Account/Login");
            return Redirect("/Identity/Account/Login");
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
