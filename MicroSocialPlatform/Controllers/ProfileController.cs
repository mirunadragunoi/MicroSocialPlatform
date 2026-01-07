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
            var followersCount = await _context.Follows.CountAsync(f => f.FollowingId == user.Id && f.Status == FollowStatus.Accepted);
            var followingCount = await _context.Follows.CountAsync(f => f.FollowerId == user.Id && f.Status == FollowStatus.Accepted);

            // verific daca utilizatorul curent urmareste acest profil
            bool isFollowingUser = false;
            bool isPending = false; // cerere trimisa

            int? incomingRequest = null; // daca exista o cerere de follow primita

            if (currentUser != null && !isOwnProfile)
            {
                var followRelation = await _context.Follows
                    .FirstOrDefaultAsync(f => f.FollowerId == currentUser.Id && f.FollowingId == user.Id);
                if (followRelation != null)
                {
                    if (followRelation.Status == FollowStatus.Accepted)
                    {
                        isFollowingUser = true;
                    }
                    else if (followRelation.Status == FollowStatus.Pending)
                    {
                        isPending = true;
                    }
                }

                // verific daca exista o cerere de follow primita de la acest utilizator
                var incomingRequestObj = await _context.Follows
                    .FirstOrDefaultAsync(f => f.FollowerId == user.Id && f.FollowingId == currentUser.Id && f.Status == FollowStatus.Pending);
                if (incomingRequestObj != null)
                {
                    incomingRequest = incomingRequestObj.Id;
                }
            }

            // verific daca exista o cerere de follow primita de la acest utilizator
            if (currentUser != null && !isOwnProfile)
            {
                var incomingRequestObj = await _context.Follows
                    .FirstOrDefaultAsync(f => f.FollowerId == user.Id
                        && f.FollowingId == currentUser.Id
                        && f.Status == FollowStatus.Pending);
                if (incomingRequestObj != null)
                {
                    incomingRequest = incomingRequestObj.Id;
                }
            }

            // obtin postarile doar daca e vizibil
            var posts = canViewProfile
                ? await _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Likes)
                    .Include(p => p.Comments)
                    .Include(p => p.PostMedias)
                    .Where(p => p.UserId == user.Id)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync()
                : new List<Post>();

            ViewBag.User = user;
            ViewBag.IsOwnProfile = isOwnProfile;
            ViewBag.CanViewProfile = canViewProfile;
            ViewBag.PostsCount = postsCount;
            ViewBag.FollowersCount = followersCount;
            ViewBag.FollowingCount = followingCount;
            ViewBag.IsFollowing = isFollowingUser;
            ViewBag.IsPending = isPending;
            ViewBag.Posts = posts;
            ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated ?? false; // ADAUGAT
            ViewBag.IncomingRequestId = incomingRequest;

            return View(user);
        }

        // metoda helper: verific daca cineva poate vedea un profil
        private async Task<bool> CanViewProfile(ApplicationUser profileUser, string? currentUserId)
        {
            // ✅ 0. ADMINISTRATORUL poate vedea ORICE profil (public sau privat)
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var currentUser = await _userManager.FindByIdAsync(currentUserId);
                if (currentUser != null)
                {
                    var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Administrator");
                    if (isAdmin)
                    {
                        return true; // Admin vede tot!
                    }
                }
            }

            // 1. daca profilul este public, oricine poate vedea
            if (profileUser.IsPublic)
            {
                return true;
            }

            // 2. daca profilul este privat
            // proprietarul poate vedea mereu propriul profil
            if (currentUserId == profileUser.Id)
            {
                return true;
            }

            // 3. utilizatorii neautentificati NU pot vedea profiluri private
            if (string.IsNullOrEmpty(currentUserId))
            {
                return false;
            }

            // 4. verific dacă utilizatorul curent urmareste profilul privat
            var isFollowing = await _context.Follows
                .AnyAsync(f => f.FollowerId == currentUserId
                            && f.FollowingId == profileUser.Id
                            && f.Status == FollowStatus.Accepted);

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
                    RequestedAt = DateTime.UtcNow,
                    Status = targetUser.IsPublic ? FollowStatus.Accepted : FollowStatus.Pending,
                };

                // daca profilul e public, setez AcceptedAt
                if (targetUser.IsPublic)
                {
                    newFollow.AcceptedAt = DateTime.UtcNow;
                }

                _context.Follows.Add(newFollow);

                // logica pentru notificari
                var notification = new Notification
                {
                    RecipientId = userId,
                    SenderId = currentUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    RelatedUrl = $"/Profile/Index?username={currentUser.UserName}"
                };

                // decidem tipul notificarii in functie de confidentialitatea contului
                if (targetUser.IsPublic)
                {
                    // cont public -> notificare Follow
                    notification.Type = NotificationType.Follow;
                    notification.Content = $"a inceput sa te urmareasca.";
                }
                else
                {
                    // cont privat -> notificare FollowRequest
                    notification.Type = NotificationType.FollowRequest;
                    notification.Content = $"ti-a trimis o cerere de urmarire.";
                }

                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                var followersCount = await _context.Follows.CountAsync(f => f.FollowingId == userId && f.Status == FollowStatus.Accepted);

                string message = newFollow.Status == FollowStatus.Accepted
                    ? "Urmărești acum utilizatorul!"
                    : "Cererea de urmărire a fost trimisă!";

                return Json(new
                {
                    success = true,
                    isFollowing = newFollow.Status == FollowStatus.Accepted,
                    isPending = newFollow.Status == FollowStatus.Pending,
                    followersCount = followersCount,
                    message = message
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

        // functionalitati pentru gestionarea cererilor de follow 

        // get - viziualizare cereri de follow primite
        [Authorize]
        public async Task<IActionResult> PendingRequests()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // caut in Follows toate intrerile unde userul curent este urmarit si statusul e Pending
            var requests = await _context.Follows
                .Include(f => f.Follower) // luam toate datele despre follower
                .Where(f => f.FollowingId == currentUser.Id && f.Status == FollowStatus.Pending)
                .OrderByDescending(f => f.RequestedAt)
                .ToListAsync();

            return View(requests);
        }

        // post - acceptare cerere follow
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var request = await _context.Follows.FindAsync(requestId);

            if (currentUser == null)
            {
                return NotFound();
            }

            // verific daca cererea este pentru userul curent
            if (request.FollowingId != currentUser.Id)
            {
                return Forbid();
            }

            request.Status = FollowStatus.Accepted;
            request.AcceptedAt = DateTime.UtcNow;

            // logica pentru notificari
            var notification = new Notification
            {
                RecipientId = request.FollowerId,
                SenderId = currentUser.Id,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                Type = NotificationType.FollowAccepted,
                Content = $"a acceptat cererea ta de urmărire.",
                RelatedUrl = $"/Profile/Index?username={currentUser.UserName}"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cererea de urmărire a fost acceptată." });
        }

        // post - respingere cerere follow
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineRequest(int requestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var request = await _context.Follows.FindAsync(requestId);

            if (currentUser == null)
            {
                return NotFound();
            }

            // verific daca cererea este pentru userul curent
            if (request.FollowingId != currentUser.Id)
            {
                return Forbid();
            }

            _context.Follows.Remove(request);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cererea de urmărire a fost respinsă." });
        }

        // get - vizualizare lista de utilizatori urmariti
        [HttpGet]
        public async Task<IActionResult> GetFollowersList(string? userId)
        {
            var followers = await _context.Follows
                .Where(f => f.FollowingId == userId && f.Status == FollowStatus.Accepted)
                .Select(f => f.Follower)
                .ToListAsync();
            return PartialView("_UserListModal", followers);
        }

        // get - vizualizare lista de utilizatori care urmaresc utilizatorul curent
        [HttpGet]
        public async Task<IActionResult> GetFollowingList(string? userId)
        {
            var following = await _context.Follows
                .Where(f => f.FollowerId == userId && f.Status == FollowStatus.Accepted)
                .Select(f => f.Following)
                .ToListAsync();

            return PartialView("_UserListModal", following);
        }

        // metode pentru acceptare/respingere cereri de follow din pagina de profil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequestFromProfile(int requestId, string returnUrl)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var request = await _context.Follows.FindAsync(requestId);

            if (request == null)
            {
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            // acceptam cererea
            request.Status = FollowStatus.Accepted;
            request.AcceptedAt = DateTime.UtcNow;

            // trimitem notificare celui care a cerut follow
            var notification = new Notification
            {
                RecipientId = request.FollowerId,
                SenderId = currentUser.Id,
                Type = NotificationType.FollowAccepted,
                Content = "ți-a acceptat cererea de urmărire.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                RelatedUrl = $"/Profile/Index?username={currentUser.CustomUsername ?? currentUser.UserName}"
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // redirectionare inapoi
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineRequestFromProfile(int requestId, string returnUrl)
        {
            var request = await _context.Follows.FindAsync(requestId);

            if (request != null)
            {
                // stergem cererea
                _context.Follows.Remove(request);
                await _context.SaveChangesAsync();
            }

            // redirectionare inapoi
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}