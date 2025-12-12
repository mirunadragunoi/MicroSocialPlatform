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
    [Authorize] // doar utilizatorii logati pot posta
    public class PostController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadService _fileUploadService;
        public PostController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IFileUploadService fileUploadService)
        {
            _userManager = userManager;
            _context = context;
            _fileUploadService = fileUploadService;
        }

        // afisez formularul pentru creare postare
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // procesez formularul de creare postare
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
            return RedirectToAction("Index", "Home");
        }

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

            if (!User.IsInRole("Admin") && post.UserId != user.Id)
            {
                return Forbid(); // doar adminul sau proprietarul postarii poate sterge
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
            TempData["Message"] = "Postarea a fost ștearsă definitiv. 🗑️";
            TempData["MessageType"] = "danger"; 

            return RedirectToAction("Index", "Home");
        }

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
            TempData["Message"] = "Postarea a fost actualizată cu succes! ✨";
            TempData["MessageType"] = "success"; 

            return RedirectToAction("Index", "Home");
        }
    }
}
