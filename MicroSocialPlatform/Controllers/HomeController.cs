using System.Diagnostics;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using MicroSocialPlatform.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using NuGet.Protocol;

namespace MicroSocialPlatform.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        // private readonly SignInManager<ApplicationUser> _signInManager;

        public HomeController(
            ILogger<HomeController> logger, 
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
            // SignInManager<ApplicationUser> signInManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            // _signInManager = signInManager;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            List<Post> posts;

            // verific tipul de utilizator
            if (User.Identity.IsAuthenticated)
            {
                // utilizator locat
                var user = await _userManager.GetUserAsync(User);
                var isAdmin = User.IsInRole("Administrator");

                // ✅ ADMIN vede TOATE postările (publice + private)
                if (isAdmin)
                {
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
                    var followingIds = await _context.Follows
                        .Where(f => f.FollowerId == user.Id && f.Status == FollowStatus.Accepted)
                        .Select(f => f.FollowingId)
                        .ToListAsync();

                    followingIds.Add(user.Id);

                    posts = await _context.Posts
                        .Include(p => p.User)
                        .Include(p => p.Likes)
                        .Include(p => p.Comments)
                            .ThenInclude(c => c.User)
                        .Include(p => p.PostMedias)
                        .Where(p => followingIds.Contains(p.UserId))
                        .OrderByDescending(p => p.CreatedAt)
                        .ToListAsync();
                }

                ViewBag.IsGuest = false;
            }
            else
            {
                // vizitator -> doar postarile publice
                posts = await _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Likes)
                    .Include(p => p.Comments)
                        .ThenInclude(c => c.User)
                    .Include(p => p.PostMedias)
                    .Where(p => p.User.IsPublic == true)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                ViewBag.IsGuest = true;
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
