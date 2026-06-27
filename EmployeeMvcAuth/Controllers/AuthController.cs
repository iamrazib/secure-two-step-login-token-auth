using EmployeeMvcAuth.Data;
using EmployeeMvcAuth.Models;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeMvcAuth.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthRepository _authRepository;

        public AuthController(AuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginUserIdViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginUserIdViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authRepository.StartLoginAsync(model.UserId);

            if (!result.Success || result.ChallengeId == null)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            HttpContext.Session.SetString("LoginChallengeId", result.ChallengeId.Value.ToString());

            return RedirectToAction(nameof(Password));
        }

        [HttpGet]
        public IActionResult Password()
        {
            var challengeId = HttpContext.Session.GetString("LoginChallengeId");

            if (string.IsNullOrWhiteSpace(challengeId))
            {
                return RedirectToAction(nameof(Login));
            }

            return View(new PasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Password(PasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var challengeIdString = HttpContext.Session.GetString("LoginChallengeId");

            if (!Guid.TryParse(challengeIdString, out var challengeId))
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _authRepository.CheckPasswordAndCreateSessionAsync(
                challengeId,
                model.Password
            );

            if (!result.Success || result.SessionToken == null)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            HttpContext.Session.Remove("LoginChallengeId");
            HttpContext.Session.SetString("AuthToken", result.SessionToken.Value.ToString());

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var tokenString = HttpContext.Session.GetString("AuthToken");

            if (Guid.TryParse(tokenString, out var sessionToken))
            {
                await _authRepository.RevokeSessionAsync(sessionToken);
            }

            HttpContext.Session.Clear();

            return RedirectToAction(nameof(Login));
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
