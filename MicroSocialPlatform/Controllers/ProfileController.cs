using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MicroSocialPlatform.Models;
using MicroSocialPlatform.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MicroSocialPlatform.Controllers
{
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<ProfileController> logger)
        {
            _userManager = userManager;
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // GET -> afisare profil utilizator (propriul profil sau al altcuiva)
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? username)
        {
            ApplicationUser? user;

            // daca nu e specificat username, afisez profilul utilizatorului curent
            if (string.IsNullOrEmpty(username))
            {
                if (!User.Identity?.IsAuthenticated ?? true)
                {
                    return RedirectToAction("Login", "Account");
                }

                user = await _userManager.GetUserAsync(User);
            }
            else
            {
                // caut utilizatorul dupa CustomUsername sau dupa email
                user = await _context.Users
                    .Include(u => u.Posts)
                    .Include(u => u.Followers)
                    .Include(u => u.Following)
                    .FirstOrDefaultAsync(u => u.CustomUsername == username || u.UserName == username);
            }

            if (user == null)
            {
                return NotFound("Utilizatorul nu a fost gasit!");
            }

            // obtin utilizatorul curent (poate fi null daca nu e autentificat)
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;

            // verific daca e propriul profil
            bool isOwnProfile = currentUserId == user.Id;

            // verific daca profilul poate fi vazut
            bool canViewProfile = await CanViewProfile(user, currentUserId);

            // calculez statistici pt afisarea profilului
            var postsCount = await _context.Posts.CountAsync(p => p.UserId == user.Id);
            var followersCount = await _context.Follows.CountAsync(f => f.FollowingId == user.Id);
            var followingCount = await _context.Follows.CountAsync(f => f.FollowerId == user.Id);

            // verific daca utilizatorul curent urmareste acest profil
            bool isFollowingUser = false;
            if (currentUser != null && !isOwnProfile)
            {
                isFollowingUser = await _context.Follows
                    .AnyAsync(f => f.FollowerId == currentUser.Id && f.FollowingId == user.Id);
            }

            // obtin postarile doar daca e vizibil
            var posts = canViewProfile
                ? await _context.Posts
                    .Where(p => p.UserId == user.Id)
                    .OrderByDescending(p => p.CreatedAt)
                    .Include(p => p.Likes)
                    .Include(p => p.Comments)
                    .ToListAsync()
                : new List<Post>();

            ViewBag.User = user;
            ViewBag.IsOwnProfile = isOwnProfile;
            ViewBag.CanViewProfile = canViewProfile;
            ViewBag.PostsCount = postsCount;
            ViewBag.FollowersCount = followersCount;
            ViewBag.FollowingCount = followingCount;
            ViewBag.IsFollowing = isFollowingUser;
            ViewBag.Posts = posts;
            ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated ?? false; // ADĂUGAT

            return View(user);
        }

        // metoda helper: verific daca cineva poate vedea un profil
        private async Task<bool> CanViewProfile(ApplicationUser profileUser, string? currentUserId)
        {
            // daca profilul este public, oricine poate vedea
            if (profileUser.IsPublic)
            {
                return true;
            }

            // daca profilul este privat
            // 1. proprietarul poate vedea mereu propriul profil
            if (currentUserId == profileUser.Id)
            {
                return true;
            }

            // 2. utilizatorii neautentificati NU pot vedea profiluri private
            if (string.IsNullOrEmpty(currentUserId))
            {
                return false;
            }

            // 3. verific dacă utilizatorul curent urmareste profilul privat
            var isFollowing = await _context.Follows
                .AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == profileUser.Id);

            return isFollowing;
        }

        // GET -> EDITARE PROFIL
        [Authorize]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                FullName = user.FullName ?? "",
                CustomUsername = user.CustomUsername,
                Bio = user.Bio,
                Status = user.Status,
                StatusEmoji = user.StatusEmoji,
                Website = user.Website,
                Location = user.Location,
                DateOfBirth = user.DateOfBirth,
                IsPublic = user.IsPublic,
                CurrentProfilePicture = user.ProfilePicture,
                CurrentCoverPhoto = user.CoverPhoto
            };

            return View(model);
        }

        // POST -> EDITARE PROFIL
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // verific daca username ul e disponibil - daca nu cumva s a schimbat
            if (!string.IsNullOrEmpty(model.CustomUsername) && model.CustomUsername != user.CustomUsername)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.CustomUsername == model.CustomUsername);

                if (existingUser != null)
                {
                    ModelState.AddModelError("CustomUsername", "Acest username este deja folosit!!");
                    return View(model);
                }
            }

            // actualizez datele utilizatorului
            user.FullName = model.FullName;
            user.CustomUsername = model.CustomUsername;
            user.Bio = model.Bio;
            user.Status = model.Status;
            user.StatusEmoji = model.StatusEmoji;
            user.Website = model.Website;
            user.Location = model.Location;
            user.DateOfBirth = model.DateOfBirth;
            user.IsPublic = model.IsPublic;

            // proceseaza upload-ul pozei de profil
            if (model.ProfilePictureFile != null)
            {
                var profilePicturePath = await SaveProfilePicture(model.ProfilePictureFile, "profile");
                if (profilePicturePath != null)
                {
                    // sterg poza veche daca exista
                    if (!string.IsNullOrEmpty(user.ProfilePicture))
                    {
                        DeleteOldPicture(user.ProfilePicture);
                    }
                    user.ProfilePicture = profilePicturePath;
                }
            }

            // proceseaza upload-ul pozei de cover
            if (model.CoverPhotoFile != null)
            {
                var coverPhotoPath = await SaveProfilePicture(model.CoverPhotoFile, "cover");
                if (coverPhotoPath != null)
                {
                    // sterg poza veche daca exista
                    if (!string.IsNullOrEmpty(user.CoverPhoto))
                    {
                        DeleteOldPicture(user.CoverPhoto);
                    }
                    user.CoverPhoto = coverPhotoPath;
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User profile updated successfully.");
                TempData["SuccessMessage"] = "Profilul a fost actualizat cu succes!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }


        // POST -> FOLLOW / UNFOLLOW UTILIZATOR
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFollow(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Trebuie să fii autentificat!" });
            }

            if (currentUser.Id == userId)
            {
                return Json(new { success = false, message = "Nu te poți urmări pe tine însuți!" });
            }

            var targetUser = await _context.Users.FindAsync(userId);
            if (targetUser == null)
            {
                return Json(new { success = false, message = "Utilizatorul nu a fost găsit!" });
            }

            var existingFollow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUser.Id && f.FollowingId == userId);

            if (existingFollow != null)
            {
                // unfollow
                _context.Follows.Remove(existingFollow);
                await _context.SaveChangesAsync();

                var followersCount = await _context.Follows.CountAsync(f => f.FollowingId == userId);

                return Json(new
                {
                    success = true,
                    isFollowing = false,
                    followersCount = followersCount,
                    message = "Ai încetat să urmărești utilizatorul!"
                });
            }
            else
            {
                // follow
                var newFollow = new Follow
                {
                    FollowerId = currentUser.Id,
                    FollowingId = userId,
                    FollowedAt = DateTime.UtcNow
                };

                _context.Follows.Add(newFollow);
                await _context.SaveChangesAsync();

                var followersCount = await _context.Follows.CountAsync(f => f.FollowingId == userId);

                return Json(new
                {
                    success = true,
                    isFollowing = true,
                    followersCount = followersCount,
                    message = "Urmărești acum utilizatorul!"
                });
            }
        }

        // HELPER -> SALVARE POZA DE PROFIL SAU COVER
        private async Task<string?> SaveProfilePicture(IFormFile file, string type)
        {
            if (file == null || file.Length == 0)
                return null;

            // validare tip fisier
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return null;
            }

            // validare marime (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return null;
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", type);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{type}/{uniqueFileName}";
        }

        // HELPER -> STERGERE POZA VECHE
        private void DeleteOldPicture(string? picturePath)
        {
            if (string.IsNullOrEmpty(picturePath))
                return;

            var fullPath = Path.Combine(_environment.WebRootPath, picturePath.TrimStart('/'));

            if (System.IO.File.Exists(fullPath))
            {
                try
                {
                    System.IO.File.Delete(fullPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting old picture: {Path}", fullPath);
                }
            }
        }
    }
}