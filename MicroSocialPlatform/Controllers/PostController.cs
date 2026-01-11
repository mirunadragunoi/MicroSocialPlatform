using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MicroSocialPlatform.Models;
using MicroSocialPlatform.Models.ViewModels;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Services;
using Microsoft.EntityFrameworkCore;

namespace MicroSocialPlatform.Controllers
{
    public class PostController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly IContentModerationService _contentModerationService;
        public PostController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IFileUploadService fileUploadService, IContentModerationService contentModerationService)
        {
            _userManager = userManager;
            _context = context;
            _fileUploadService = fileUploadService;
            _contentModerationService = contentModerationService;
        }

        // afisez formularul pentru creare postare
        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // procesez formularul de creare postare
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(CreatePostViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // aflu cine posteaza
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // verific cu componenta AI daca continutul este ok
            if (!string.IsNullOrWhiteSpace(model.Content))
            {
                Console.WriteLine("Calling moderation service...");
                var moderationResult = await _contentModerationService.ModerateContentAsync(model.Content);
                Console.WriteLine($"Moderation result: {moderationResult.IsClean}");

                if (!moderationResult.IsClean)
                {
                    Console.WriteLine($"BLOCKED! Reason: {moderationResult.Reason}");
                    TempData["ErrorMessage"] = $"❌ Postarea ta conține conținut nepotrivit. {moderationResult.Reason}";
                    return View(model);
                }
            }

            Console.WriteLine("Content approved, creating post...");

            // creez noua postare fara imagine/video initial
            var post = new Post
            {
                Content = model.Content,
                CreatedAt = DateTime.UtcNow,
                UserId = user.Id,
                User = user,
                LikesCount = 0,
                CommentsCount = 0,
                Visibility = PostVisibility.Public
            };

            // salvez postarea pentru a obtine un Id valid
            _context.Add(post);
            await _context.SaveChangesAsync();

            // incarc imaginile/video-urile daca exista
            if (model.Images != null && model.Images.Count > 0)
            {
                foreach (var image in model.Images)
                {
                    string imageUrl = await _fileUploadService.UploadFileAsync(image);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        var media = new PostMedia
                        {
                            PostId = post.Id,
                            MediaType = MediaType.Image,
                            Url = imageUrl
                        };
                        _context.Add(media);
                    }
                }
            }

            if (model.Videos != null && model.Videos.Count > 0)
            {
                foreach (var video in model.Videos)
                {
                    string videoUrl = await _fileUploadService.UploadFileAsync(video);
                    if (!string.IsNullOrEmpty(videoUrl))
                    {
                        var media = new PostMedia
                        {
                            PostId = post.Id,
                            MediaType = MediaType.Video,
                            Url = videoUrl
                        };
                        _context.Add(media);
                    }
                }
            }

            // salvez modificarile
            await _context.SaveChangesAsync();

            // creez notificarile pentru urmaritori
            var followers = await _context.Follows
                .Where(f => f.FollowingId == user.Id && f.Status == FollowStatus.Accepted)
                .Select(f => f.FollowerId)
                .ToListAsync();

            foreach (var followerId in followers)
            {
                var notification = new Notification
                {
                    RecipientId = followerId,
                    SenderId = user.Id,
                    Type = NotificationType.NewPost,
                    Content = $"{user.UserName} a publicat o nouă postare",
                    RelatedUrl = $"/Post/Details/{post.Id}",
                    RelatedEntityId = post.Id,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Postarea a fost creată cu succes!";

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // caut postarea
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            // aflu cine este userul curent
            var user = await _userManager.GetUserAsync(User);

            if (!User.IsInRole("Administrator") && post.UserId != user.Id)
            {
                return Forbid(); // doar adminul sau proprietarul postarii poate sterge
            }

            // salvez informatiile inainte de stergere
            var postOwnerId = post.UserId;
            var postOwner = post.User;

            // daca post.User e null, incarc manual
            if (postOwner == null)
            {
                postOwner = await _context.Users.FindAsync(postOwnerId);
            }

            var savedPosts = await _context.SavedPosts
                .Where(sp => sp.PostId == id)
                .ToListAsync();

            if (savedPosts.Any())
            {
                _context.SavedPosts.RemoveRange(savedPosts);
                await _context.SaveChangesAsync();
            }

            // trimite notificare doar daca altcineva sterge postarea
            if (postOwnerId != user.Id && postOwner != null)
            {
                var notification = new Notification
                {
                    RecipientId = postOwnerId,
                    SenderId = user.Id,
                    Type = NotificationType.PostDeleted,
                    Content = "ți-a șters o postare.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    RelatedUrl = $"/Profile/Index?username={postOwner.CustomUsername ?? postOwner.UserName}"
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync(); 
            }

            // sterg media asociata
            if (post.PostMedias != null)
            {
                foreach (var media in post.PostMedias)
                {
                    _fileUploadService.DeleteFile(media.Url);
                }
            }

            // sterg postarea
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            // mesaj de succes
            TempData["SuccessMessage"] = "Postarea a fost ștearsă cu succes. 🗑️";

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // caut postarea
            var post = await _context.Posts
                .Include(p => p.PostMedias)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            // aflu cine este userul curent
            var user = await _userManager.GetUserAsync(User);
            if (post.UserId != user.Id)
            {
                return Forbid(); // doar proprietarul postarii poate edita
            }

            // mut datele in ViewModel
            var model = new EditPostViewModel
            {
                Id = post.Id,
                Content = post.Content,
                ExistingMedia = post.PostMedias.ToList()
            };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditPostViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ExistingMedia = _context.PostMedias
                    .Where(pm => pm.PostId == model.Id)
                    .ToList();
                return View(model);
            }

            // caut postarea
            var post = await _context.Posts
                .Include(p => p.PostMedias)
                .FirstOrDefaultAsync(p => p.Id == model.Id);

            if (post == null)
            {
                return NotFound();
            }

            // aflu cine este userul curent
            var user = await _userManager.GetUserAsync(User);
            if (post.UserId != user.Id)
            {
                return Forbid(); // doar proprietarul postarii poate edita
            }

            // actualizez continutul
            post.Content = model.Content;
            post.UpdatedAt = DateTime.UtcNow;

            // sterg media selectata
            if (model.MediaIdsToDelete != null && model.MediaIdsToDelete.Count > 0)
            {
                foreach (var mediaId in model.MediaIdsToDelete)
                {
                    // caut media in postare
                    var media = post.PostMedias.FirstOrDefault(pm => pm.Id == mediaId);
                    if (media != null)
                    {
                        _context.PostMedias.Remove(media);
                        _context.PostMedias.Remove(media);
                    }
                }
            }

            // incarc noile imagini daca exista
            if (model.NewImages != null && model.NewImages.Count > 0)
            {
                foreach (var image in model.NewImages)
                {
                    string imageUrl = await _fileUploadService.UploadFileAsync(image);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        var media = new PostMedia
                        {
                            PostId = post.Id,
                            MediaType = MediaType.Image,
                            Url = imageUrl
                        };
                        _context.Add(media);
                    }
                }
            }

            // incarc noile video-uri daca exista
            if (model.NewVideos != null && model.NewVideos.Count > 0)
            {
                foreach (var video in model.NewVideos)
                {
                    string videoUrl = await _fileUploadService.UploadFileAsync(video);
                    if (!string.IsNullOrEmpty(videoUrl))
                    {
                        var media = new PostMedia
                        {
                            PostId = post.Id,
                            MediaType = MediaType.Video,
                            Url = videoUrl
                        };
                        _context.Add(media);
                    }
                }
            }

            // salvez modificarile
            await _context.SaveChangesAsync();

            // mesaj de succes
            TempData["SuccessMessage"] = "Postarea a fost actualizată cu succes! ✨"; 

            return RedirectToAction("Index", "Home");
        }

        // GET - feed/pagina de acasa cu toate postarile publice
        [Authorize]
        public async Task<IActionResult> Feed()
        {
            var publicPosts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Include(p => p.PostMedias)
                .Where(p => p.User.IsPublic)
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .ToListAsync();
            return View(publicPosts);
        }

        // GET -  following feed (doar postari de la cei pe care ii urmaresti)
        [Authorize]
        public async Task<IActionResult> FollowingFeed()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // obtine ID-urile utilizatorilor pe care ii urmaresti
            var followingIds = await _context.Follows
                .Where(f => f.FollowerId == currentUser.Id)
                .Select(f => f.FollowingId)
                .ToListAsync();

            // obtine postarile de la utilizatorii urmariti
            // nu mai verificam IsPublic pentru ca follow = ai acces
            var followingPosts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Where(p => followingIds.Contains(p.UserId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(followingPosts);
        }

        // GET - detalii postare individuala 
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                    .ThenInclude(l => l.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Include(p => p.PostMedias)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                TempData["ErrorMessage"] = "Nu ai permisiunea să vezi această postare.";
                return NotFound();
            }

            // verific daca utilizatorul poate vedea postarea
            var currentUser = await _userManager.GetUserAsync(User);
            var canView = await CanViewPost(post, currentUser);

            if (!canView)
            {
                return Forbid();
            }

            // verific daca utilizatorul curent a dat like
            bool hasLiked = false;
            if (currentUser != null)
            {
                hasLiked = await _context.Likes
                    .AnyAsync(l => l.PostId == post.Id && l.UserId == currentUser.Id);
            }

            ViewBag.HasLiked = hasLiked;
            ViewBag.CurrentUser = currentUser;

            return View(post);
        }

        // helper -->> verific daca utilizatorul poate vedea postarea
        private async Task<bool> CanViewPost(Post post, ApplicationUser? currentUser)
        {
            // daca utilizatorul care a postat are profil public, oricine poate vedea
            if (post.User?.IsPublic == true)
            {
                return true;
            }

            // daca nu e autentificat, nu poate vedea postarile de la profiluri private
            if (currentUser == null)
            {
                return false;
            }

            // daca e proprietarul postarii, poate vedea
            if (post.UserId == currentUser.Id)
            {
                return true;
            }

            // daca e administrator, poate vedea tot
            if (User.IsInRole("Administrator"))
            {
                return true;
            }

            // verific daca utilizatorul curent urmareste pe cel care a postat
            var isFollowing = await _context.Follows
                .AnyAsync(f => f.FollowerId == currentUser.Id && f.FollowingId == post.UserId);

            return isFollowing;
        }

        // actiune pentru butonul de salvare postare
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleSave(int postId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var existingSave = await _context.SavedPosts
                .FirstOrDefaultAsync(sp => sp.PostId == postId && sp.UserId == user.Id);

            bool isSaved = false;

            if (existingSave != null)
            {
                // daca exista deja, sterg salvarea
                _context.SavedPosts.Remove(existingSave);
                isSaved = false;
            }
            else
            {
                // daca nu exista, creez o noua salvare
                var savedPost = new SavedPost
                {
                    PostId = postId,
                    UserId = user.Id,
                    SavedAt = DateTime.UtcNow
                };
                _context.SavedPosts.Add(savedPost);
                isSaved = true;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, isSaved = isSaved });
        }

        // GET - lista de postari salvate
        [Authorize]
        public async Task<IActionResult> Saved()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var savedPosts = await _context.SavedPosts
                .Include(s => s.Post)
                    .ThenInclude(p => p.User)
                .Include(s => s.Post)
                    .ThenInclude(p => p.Comments)
                .Include(s => s.Post)
                    .ThenInclude(p => p.Likes)
                .Where(s => s.UserId == user.Id)
                .OrderByDescending(s => s.SavedAt)
                .Select(s => s.Post)
                .ToListAsync();

            return View(savedPosts);
        }

        // reactiile de la o postare pentru modala
        [HttpGet]
        public async Task<IActionResult> GetPostLikes(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Likes)
                .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            // returnam un obiect anonim 
            var likers = post.Likes.Select(l => new
            {
                userId = l.UserId,
                userName = l.User.UserName,
                fullName = l.User.FullName,
                profilePicture = l.User.ProfilePicture,
                reactionType = (int)l.Type // trimitem tipul reactiei ca numar (1, 2, 3...)
            }).ToList();

            return Json(likers);
        }

        // functie pentru unsaved post
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnsavePost(int postId)
        {
            try
            {


                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {

                    return Json(new { success = false, message = "Trebuie să fii autentificat!" });
                }



                var savedPost = await _context.SavedPosts
                    .FirstOrDefaultAsync(sp => sp.PostId == postId && sp.UserId == currentUser.Id);

                if (savedPost == null)
                {

                    return Json(new { success = false, message = "Postarea nu este salvată!" });
                }

                _context.SavedPosts.Remove(savedPost);
                await _context.SaveChangesAsync();



                return Json(new
                {
                    success = true,
                    message = "Postarea a fost eliminată din salvate!"
                });
            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = "Eroare la eliminarea postării: " + ex.Message });
            }
        }

    }
}