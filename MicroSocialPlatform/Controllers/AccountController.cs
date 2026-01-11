using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;

namespace MicroSocialPlatform.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // login 
        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            // curat cookie urile initiale
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            var model = new LoginViewModel 
            {
                ReturnUrl = returnUrl,
                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // protecție CSRF
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            model.ReturnUrl ??= Url.Content("~/");
            model.ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // gasesc utilizatorul dupa email
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Email sau parolă incorectă.");
                    return View(model);
                }

                // foloseste username-ul pentru login
                var userName = user.UserName;

                // login cu username
                var result = await _signInManager.PasswordSignInAsync(
                    userName,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(model.ReturnUrl);
                }

                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction("LoginWith2fa", new { ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToAction("Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Email sau parolă incorectă.");
                    return View(model);
                }
            }

            // daca se ajunge aici, ceva a esuat
            return View(model);
        }

        // logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }

        // register
        [HttpGet]
        public async Task<IActionResult> Register(string? returnUrl = null)
        {
            var model = new RegisterViewModel
            {
                ReturnUrl = returnUrl,
                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            model.ReturnUrl ??= Url.Content("~/");
            model.ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // VALIDARI!!!!
                // daca emailul exista deja
                var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingUserByEmail != null)
                {
                    ModelState.AddModelError("Email", "Acest email este deja înregistrat. Te rugăm să folosești alt email sau să te autentifici.");
                    return View(model);
                }

                // verific username ul
                var existingUserByUsername = await _userManager.FindByNameAsync(model.Email);
                if (existingUserByUsername != null)
                {
                    ModelState.AddModelError("Email", "Acest email este deja folosit ca username în sistem.");
                    return View(model);
                }

                // validare personalizata dupa nume
                if (string.IsNullOrWhiteSpace(model.FullName))
                {
                    ModelState.AddModelError("FullName", "Numele complet este obligatoriu!");
                    return View(model);
                }

                // validare personalizata pentru parola 
                // minim 6 caractere
                if (model.Password.Length < 6)
                {
                    ModelState.AddModelError("Password", "Parola trebuie să aibă cel puțin 6 caractere!");
                    return View(model);
                }

                // cel putin o litera mare
                if (!model.Password.Any(char.IsUpper))
                {
                    ModelState.AddModelError("Password", "Parola trebuie să conțină cel puțin o literă mare!");
                    return View(model);
                }

                // cel putin o cifra
                if (!model.Password.Any(char.IsDigit))
                {
                    ModelState.AddModelError("Password", "Parola trebuie să conțină cel puțin o cifră!");
                    return View(model);
                }

                // creeaza utilizatorul
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    FullName = model.FullName,
                    Email = model.Email,
                    CreatedAt = DateTime.UtcNow,
                    IsPublic = true // cont public by default
                };

                // creeaza contul
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // adauga rolul "User" pentru utilizatorii noi
                    await _userManager.AddToRoleAsync(user, "RegisteredUser");

                     // autentificare automata dupa inregistrare
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(model.ReturnUrl);
                    
                }

                // adaug erorile îin ModelState
                foreach (var error in result.Errors)
                {
                    // traduce erorile Identity in romana
                    string errorMessage = error.Code switch
                    {
                        "DuplicateUserName" => "Acest email este deja folosit. Te rugăm să alegi altul.",
                        "DuplicateEmail" => "Acest email este deja înregistrat în sistem.",
                        "InvalidEmail" => "Adresa de email nu este validă.",
                        "PasswordTooShort" => "Parola trebuie să aibă cel puțin 6 caractere.",
                        "PasswordRequiresNonAlphanumeric" => "Parola trebuie să conțină cel puțin un caracter special (!@#$%^&*).",
                        "PasswordRequiresDigit" => "Parola trebuie să conțină cel puțin o cifră.",
                        "PasswordRequiresUpper" => "Parola trebuie să conțină cel puțin o literă mare.",
                        "PasswordRequiresLower" => "Parola trebuie să conțină cel puțin o literă mică.",
                        _ => error.Description // mesaj default daca nu e tradus
                    };

                    ModelState.AddModelError(string.Empty, errorMessage);
                }
            }

            // daca ajungem aici, ceva a esuat
            return View(model);
        }

        // LOCKOUT

        [HttpGet]
        public IActionResult Lockout()
        {
            return View();
        }

        // verificare instantanee pentru email ul existent
        [HttpGet]
        public async Task<IActionResult> CheckEmailAvailable(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { available = false });
            }

            var user = await _userManager.FindByEmailAsync(email);
            return Json(new { available = user == null });
        }
    }
}
