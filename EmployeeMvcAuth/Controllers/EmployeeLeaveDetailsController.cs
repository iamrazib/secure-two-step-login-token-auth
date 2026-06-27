using EmployeeMvcAuth.Data;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeMvcAuth.Controllers
{
    public class EmployeeLeaveDetailsController : Controller
    {
        private readonly AuthRepository _authRepository;

        public EmployeeLeaveDetailsController(AuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var tokenString = HttpContext.Session.GetString("AuthToken");

            if (!Guid.TryParse(tokenString, out var sessionToken))
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }

            var leaveDetails = await _authRepository.GetEmployeeLeaveDetailsAsync(sessionToken);

            if (leaveDetails == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }
            if (!leaveDetails.IsSessionValid)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }

            return View(leaveDetails);
        }
    }
}
