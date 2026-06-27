using EmployeeMvcAuth.Data;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeMvcAuth.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AuthRepository _authRepository;

        public DashboardController(AuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var tokenString = HttpContext.Session.GetString("AuthToken");

            if (!Guid.TryParse(tokenString, out var sessionToken))
            {
                return RedirectToAction("Login", "Auth");
            }

            var profile = await _authRepository.GetMyEmployeeProfileAsync(sessionToken);

            if (profile == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }

            return View(profile);
        }
    }
}
