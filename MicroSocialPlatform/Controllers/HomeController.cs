using System.Diagnostics;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using MicroSocialPlatform.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

        public async Task<IActionResult> Index()
        {
            List<Post> posts;

            // verific tipul de utilizator
            if (!_signInManager.IsSignedIn(User))
            {
                // VIZITATOR NEINREGISTRAT - doar postarile cu profil public
                posts = await _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Likes)
                    .Include(p => p.Comments)
                        .ThenInclude(c => c.User)
                    .Include(p => p.PostMedias)
                    .Where(p => p.User.IsPublic) // doar utilizatorii cu profil public
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(50)
                    .ToListAsync();
            }
            else
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    // daca nu gasim utilizatorul, tratam ca vizitator
                    posts = await _context.Posts
                        .Include(p => p.User)
                        .Include(p => p.Likes)
                        .Include(p => p.Comments)
                            .ThenInclude(c => c.User)
                        .Include(p => p.PostMedias)
                        .Where(p => p.User.IsPublic)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(50)
                        .ToListAsync();
                }
                else
                {
                    var isAdministrator = await UserHelper.IsAdministratorAsync(_userManager, currentUser);

                    if (isAdministrator)
                    {
                        // ADMINISTRATOR - toate postarile (public si privat)
                        posts = await _context.Posts
                            .Include(p => p.User)
                            .Include(p => p.Likes)
                            .Include(p => p.Comments)
                                .ThenInclude(c => c.User)
                            .Include(p => p.PostMedias)
                            .OrderByDescending(p => p.CreatedAt)
                            .Take(50)
                            .ToListAsync();
                    }
                    else
                    {
                        // USER INREGISTRAT - logica
                        // 1. postarile cu profil public
                        // 2. postarile de la utilizatorii pe care ii urmareste (public sau privat)
                        // 3. postarile proprii

                        // obtin lista de ID-uri ale utilizatorilor pe care ii urmareste
                        var followingIds = await _context.Follows
                            .Where(f => f.FollowerId == currentUser.Id)
                            .Select(f => f.FollowingId)
                            .ToListAsync();

                        // adaug si ID-ul utilizatorului curent pentru postarile proprii
                        followingIds.Add(currentUser.Id);

                        posts = await _context.Posts
                            .Include(p => p.User)
                            .Include(p => p.Likes)
                            .Include(p => p.Comments)
                                .ThenInclude(c => c.User)
                            .Include(p => p.PostMedias)
                            .Where(p => 
                                // postarile cu profil public
                                p.User.IsPublic ||
                                // postarile de la utilizatorii pe care ii urmareste (inclusiv propriile)
                                followingIds.Contains(p.UserId)
                            )
                            .OrderByDescending(p => p.CreatedAt)
                            .Take(50)
                            .ToListAsync();
                    }
                }
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
