using EmployeeMvcAuth.Data;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeMvcAuth.Controllers
{
    public class EmployeeSalaryController : Controller
    {
        private readonly AuthRepository _authRepository;

        public EmployeeSalaryController(AuthRepository authRepository)
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

            var salary = await _authRepository.GetEmployeeSalaryAsync(sessionToken);

            if (salary == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }

            return View(salary);
        }
    }
}
