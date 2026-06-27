using EmployeeMvcAuth.Data;
using EmployeeMvcAuth.Models;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeMvcAuth.Controllers
{
    public class EmployeeUserController : Controller
    {
        private readonly AuthRepository _authRepository;

        public EmployeeUserController(AuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateEmployeeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authRepository.CreateEmployeeAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction("Login", "Auth");
        }
    }
}
