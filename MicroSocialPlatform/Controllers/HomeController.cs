using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;

namespace MicroSocialPlatform.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            List<Post> posts = new List<Post>();

            // Verificam daca userul pare logat
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);

                // === AICI E REPARATIA CRITICA ===
                // Daca userul e null (adica baza s-a sters, dar browserul tine minte vechiul user)
                if (user == null)
                {
                    await _signInManager.SignOutAsync(); // Il delogam fortat
                    return RedirectToAction("Index"); // Dam refresh la pagina
                }
                // ================================

                var isAdmin = User.IsInRole("Administrator");

                if (isAdmin)
                {
                    // Adminul vede tot
                    posts = await _context.Posts
                        .Include(p => p.User)
                        .Include(p => p.Likes)
                        .Include(p => p.Comments)
                            .ThenInclude(c => c.User)
                        .Include(p => p.PostMedias)
                        .OrderByDescending(p => p.CreatedAt)
                        .ToListAsync();
                }
                else
                {
                    // Utilizator normal -> feed personalizat (doar urmăriți)
                    // Mai intai luam lista de ID-uri pe care le urmarim
                    var followingIds = await _context.Follows
                        .Where(f => f.FollowerId == user.Id && f.Status == FollowStatus.Accepted)
                        .Select(f => f.FollowingId)
                        .ToListAsync();

                    // Adaugam si ID-ul propriu ca sa ne vedem postarile noastre
                    followingIds.Add(user.Id);

                    // Luam postarile doar de la acesti useri
                    posts = await _context.Posts
                        .Include(p => p.User)
                        .Include(p => p.Likes)
                        .Include(p => p.Comments)
                            .ThenInclude(c => c.User)
                        .Include(p => p.PostMedias)
                        .Where(p => followingIds.Contains(p.UserId)) // <--- FILTRAREA
                        .OrderByDescending(p => p.CreatedAt)
                        .ToListAsync();
                }
            }
            else
            {
                // Vizitator neautentificat - vede doar postari publice
                posts = await _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Likes)
                    .Include(p => p.PostMedias)
                    .Where(p => p.User.IsPublic)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(10) // Limitam la 10 pt vizitatori
                    .ToListAsync();
            }

            return View(posts);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}