using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Models;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Helpers;
using Microsoft.AspNetCore.Authorization;
using MicroSocialPlatform.Models.ViewModels;

namespace MicroSocialPlatform.Controllers
{
    [AllowAnonymous]
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public SearchController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET -->> Search
        public async Task<IActionResult> Index(string? q, string? type)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                ViewBag.Query = "";
                ViewBag.Profiles = new List<ApplicationUser>();
                ViewBag.Posts = new List<Post>();
                ViewBag.Type = "all";
                return View();
            }

            ViewBag.Query = q.Trim();
            ViewBag.Type = type ?? "all";

            var query = q.Trim().ToLower();
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;

            // cautare profil
            List<SearchResultViewModel> profiles = new List<SearchResultViewModel>();
            if (type == null || type == "all" || type == "profiles")
            {
                var profilesQuery = _context.Users.AsQueryable();

                // cautare dupa FullName, CustomUsername, UserName (email), Bio
                profilesQuery = profilesQuery.Where(u =>
                    (u.FullName != null && u.FullName.ToLower().Contains(query)) ||
                    (u.CustomUsername != null && u.CustomUsername.ToLower().Contains(query)) ||
                    (u.UserName != null && u.UserName.ToLower().Contains(query)) ||
                    (u.Bio != null && u.Bio.ToLower().Contains(query))
                );

                profiles = await profilesQuery
                    .OrderByDescending(u => u.CreatedAt)
                    .Select(u => new SearchResultViewModel
                    {
                        UserId = u.Id,
                        FullName = u.FullName ?? "Utilizator",
                        Username = u.CustomUsername ?? u.UserName,
                        Bio = u.Bio,
                        ProfilePicture = u.ProfilePicture,
                        IsPublic = u.IsPublic,
                        IsCurrentUser = (currentUserId != null && u.Id == currentUserId),
                        IsFollowing = _context.Follows.Any(f => f.FollowerId == currentUserId && f.FollowingId == u.Id && f.Status == FollowStatus.Accepted),
                        IsPending = _context.Follows.Any(f => f.FollowerId == currentUserId && f.FollowingId == u.Id && f.Status == FollowStatus.Pending),
                    })
                    .Take(20)
                    .ToListAsync();
            }

            // cautare postari
            List<Post> posts = new List<Post>();
            if (type == null || type == "all" || type == "posts")
            {
                var postsQuery = _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Likes)
                    .Include(p => p.Comments)
                    .Include(p => p.PostMedias)
                    .AsQueryable();

                // trebuie sa caut si dupa continutul postarii
                postsQuery = postsQuery.Where(p =>
                    (p.Content != null && p.Content.ToLower().Contains(query))
                );

                // filtrez dupa tipul de utilizator
                if (!_signInManager.IsSignedIn(User))
                {
                    // VIZITATOR - doar postarile de la profiluri publice
                    postsQuery = postsQuery.Where(p => p.User != null && p.User.IsPublic);
                }
                else if (currentUser != null)
                {
                    var isAdministrator = await UserHelper.IsAdministratorAsync(_userManager, currentUser);

                    if (!isAdministrator)
                    {
                        // USER INREGISTRAT - doar postarile de la profiluri publice
                        // SAU de la utilizatorii pe care ii urmareste
                        // SAU propriile postari
                        var followingIds = await _context.Follows
                            .Where(f => f.FollowerId == currentUser.Id)
                            .Select(f => f.FollowingId)
                            .ToListAsync();

                        followingIds.Add(currentUser.Id);

                        postsQuery = postsQuery.Where(p =>
                            (p.User != null && p.User.IsPublic) ||
                            (p.UserId != null && followingIds.Contains(p.UserId))
                        );
                    }
                    // ADMINISTRATOR - vede toate postarile (nu mai filtrez)
                }

                posts = await postsQuery
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(20)
                    .ToListAsync();
            }

            // cautare grupuri
            List<Group> groups = new List<Group>();

            // cautam doar daca filtrul este "all" sau "groups"
            if (type == null || type == "all" || type == "groups")
            {
                var groupsQuery = _context.Groups
                    .Include(g => g.Members)
                    .Include(g => g.Owner)
                    .AsQueryable();

                // cautare dupa nume si descriere
                groupsQuery = groupsQuery.Where(g =>
                    (g.Name != null && g.Name.ToLower().Contains(query)) ||
                    (g.Description != null && g.Description.ToLower().Contains(query))
                );
                groups = await groupsQuery
                    .OrderByDescending(g => g.CreatedAt)
                    .Take(20)
                    .ToListAsync();
            }

            ViewBag.Profiles = profiles;
            ViewBag.Posts = posts;
            ViewBag.Groups = groups;
            ViewBag.CurrentUser = currentUser;
            ViewBag.CurrentUserId = currentUser?.Id;

            return View();
        }
    }
}
