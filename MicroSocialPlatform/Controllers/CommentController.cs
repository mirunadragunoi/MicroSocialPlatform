using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Models;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Helpers;

namespace MicroSocialPlatform.Controllers
{
    public class CommentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // POST: Comment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(int postId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Comentariul nu poate fi gol!" });
            }

            if (content.Length > 500)
            {
                return Json(new { success = false, message = "Comentariul nu poate depăși 500 de caractere!" });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Trebuie să fii autentificat!" });
            }

            // verific daca postarea exista
            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
            {
                return Json(new { success = false, message = "Postarea nu a fost găsită!" });
            }

            // verific daca utilizatorul poate vedea postarea (si implicit sa comenteze)
            var canView = await CanViewPost(post, currentUser);
            if (!canView)
            {
                return Json(new { success = false, message = "Nu ai permisiunea să comentezi la această postare!" });
            }

            // creez comentariul
            var comment = new Comment
            {
                PostId = postId,
                UserId = currentUser.Id,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            post.CommentsCount++;

            // creez notificarea
            if (post.UserId != currentUser.Id)
            {
                var notification = new Notification
                {
                    RecipientId = post.UserId,
                    SenderId = currentUser.Id,
                    Type = NotificationType.Comment,
                    Content = $"{currentUser.UserName} a comentat la postarea ta",
                    RelatedUrl = $"/Post/Details/{postId}",
                    RelatedEntityId = postId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            // actualizez numarul de comentarii dupa salvare
            post.CommentsCount = await _context.Comments.CountAsync(c => c.PostId == postId);
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();

            // incarc comentariul cu user-ul pentru a-l returna
            var commentWithUser = await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == comment.Id);

            await _context.Entry(comment).Reference(c => c.User).LoadAsync();

            return Json(new
            {
                success = true,
                comment = new
                {
                    id = commentWithUser.Id,
                    content = commentWithUser.Content,
                    createdAt = commentWithUser.CreatedAt,
                    user = new
                    {
                        id = commentWithUser.User?.Id,
                        userName = commentWithUser.User?.UserName,
                        customUsername = commentWithUser.User?.CustomUsername ?? commentWithUser.User?.UserName,
                        fullName = commentWithUser.User?.FullName ?? commentWithUser.User?.UserName,
                        profilePicture = commentWithUser.User?.ProfilePicture
                    }
                },
                commentsCount = post.CommentsCount
            });
        }

        // POST: Comment/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Trebuie să fii autentificat!" });
            }

            var comment = await _context.Comments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
            {
                return Json(new { success = false, message = "Comentariul nu a fost găsit!" });
            }

            // verific daca utilizatorul poate sterge comentariul (proprietar, admin sau proprietarul postarii)
            var isOwner = comment.UserId == currentUser.Id;
            var isAdmin = User.IsInRole("Administrator");
            var isPostOwner = comment.Post?.UserId == currentUser.Id;

            if (!isOwner && !isAdmin && !isPostOwner)
            {
                return Json(new { success = false, message = "Nu ai permisiunea să ștergi acest comentariu!" });
            }

            var postId = comment.PostId;

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            // actualizez numarul de comentarii dupa stergere
            var post = await _context.Posts.FindAsync(postId);
            if (post != null)
            {
                post.CommentsCount = await _context.Comments.CountAsync(c => c.PostId == postId);
                _context.Posts.Update(post);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, commentsCount = post?.CommentsCount ?? 0 });
        }

        // POST: Comment/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Comentariul nu poate fi gol!" });
            }

            if (content.Length > 500)
            {
                return Json(new { success = false, message = "Comentariul nu poate depăși 500 de caractere!" });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Trebuie să fii autentificat!" });
            }

            var comment = await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
            {
                return Json(new { success = false, message = "Comentariul nu a fost găsit!" });
            }

            // doar proprietarul comentariului poate edita
            if (comment.UserId != currentUser.Id)
            {
                return Json(new { success = false, message = "Nu ai permisiunea să editezi acest comentariu!" });
            }

            // actualizez comentariul
            comment.Content = content.Trim();
            comment.UpdatedAt = DateTime.UtcNow;

            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                comment = new
                {
                    id = comment.Id,
                    content = comment.Content,
                    createdAt = comment.CreatedAt,
                    updatedAt = comment.UpdatedAt,
                    user = new
                    {
                        id = comment.User?.Id,
                        userName = comment.User?.UserName,
                        customUsername = comment.User?.CustomUsername ?? comment.User?.UserName,
                        fullName = comment.User?.FullName ?? comment.User?.UserName
                    }
                }
            });
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
    }
}

