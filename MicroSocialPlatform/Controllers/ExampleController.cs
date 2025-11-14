using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroSocialPlatform.Controllers
{
    public class ExampleController : Controller
    {
        // Această acțiune este accesibilă tuturor (vizitatori, utilizatori, administratori)
        public IActionResult PublicContent()
        {
            return View();
        }

        // Această acțiune este accesibilă doar utilizatorilor înregistrați (User sau Administrator)
        [Authorize(Policy = "RequireRegisteredUser")]
        public IActionResult RegisteredUserContent()
        {
            return View();
        }

        // Această acțiune este accesibilă doar administratorilor
        [Authorize(Policy = "RequireAdministrator")]
        public IActionResult AdminContent()
        {
            return View();
        }

        // Alternativ, poți folosi direct atributul [Authorize] fără politică
        // pentru a permite doar utilizatorilor autentificați
        [Authorize]
        public IActionResult AuthenticatedContent()
        {
            return View();
        }
    }
}

