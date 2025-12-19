using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using MicroSocialPlatform.Models.ViewModels;
using AspNetCoreGeneratedDocument;

namespace MicroSocialPlatform.Controllers
{
    [Authorize] // doar userii autentificați pot umbla la grupuri
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GroupsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // lista grupurilor (index)
        [AllowAnonymous] // oricine poate vedea lista grupurilor
        public async Task<IActionResult> Index()
        {
            ViewBag.CurrentUserId = _userManager.GetUserId(User);

            var groups = await _context.Groups
                .Include(g => g.Members) // includ membrii pentru a numara cati sunt
                .Include(g => g.Owner)   // includ proprietarul pentru afisare
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            return View(groups);
        }

        // creare grup (GET)
        public IActionResult Create()
        {
            return View();
        }

        // creare grup (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GroupCreateViewModel group)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                // salvare poza
                string? groupPicturePath = null;

                if (group.GroupPicture != null && group.GroupPicture.Length > 0)
                {
                    // definim calea de stocare
                    var storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/groups");

                    if (!Directory.Exists(storagePath)) Directory.CreateDirectory(storagePath);
                    // generam nume unic pentru fisier
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(group.GroupPicture.FileName);
                    var filePath = Path.Combine(storagePath, fileName);

                    // salvam fizic fisierul
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await group.GroupPicture.CopyToAsync(stream);
                    }

                    // setam calea pentru baza de date
                    groupPicturePath = "/images/groups/" + fileName;
                }

                // copiez datele din ViewModel in modelul Group
                var groupEntity = new Group
                {
                    Name = group.Name,
                    Description = group.Description,
                    CreatedAt = DateTime.UtcNow,
                    OwnerId = user.Id,
                    GroupPicture = groupPicturePath
                };

                // adaug grupul in baza de date
                _context.Add(groupEntity);
                await _context.SaveChangesAsync();

                // dupa ce avem id-ul grupului, adaug userul ca admin
                var member = new GroupMember
                {
                    GroupId = groupEntity.Id,
                    UserId = user.Id,
                    Role = GroupRole.Admin,
                    JoinedAt = DateTime.UtcNow
                };

                _context.GroupMembers.Add(member);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Grupul a fost creat cu succes!";
                TempData["MessageType"] = "success";

                // redirect la pagina grupului
                return RedirectToAction(nameof(Index));
            }
            return View(group);
        }

        // detalii grup
        [AllowAnonymous] // oricine poate vedea detaliile unui grup
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var group = await _context.Groups
                .Include(g => g.Owner)
                .Include(g => g.Members)
                    .ThenInclude(gm => gm.User)
                .Include(g => g.Messages)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (group == null)
            {
                return NotFound();
            }

            // verificam pentru userul curebtt daca este membru al grupului
            var user = await _userManager.GetUserAsync(User);
            bool isMember = false;
            bool isAdmin = false;

            if (user != null)
            {
                var membership = group.Members.FirstOrDefault(m => m.UserId == user.Id);
                if (membership != null)
                {
                    isMember = true;
                    if (membership.Role == GroupRole.Admin)
                    {
                        isAdmin = true;
                    }
                }
            }

            ViewBag.IsMember = isMember;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.CurrentUserId = user?.Id;

            return View(group);
        }

        // join grup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == id);
            if (group == null)
            {
                return NotFound();
            }

            // verificam daca userul este deja membru
            if (group.Members.Any(m => m.UserId == user.Id))
            {
                TempData["Message"] = "Esti deja membru al acestui grup.";
                TempData["MessageType"] = "info";
                return RedirectToAction(nameof(Details), new { id = id });
            }
            var member = new GroupMember
            {
                GroupId = group.Id,
                UserId = user.Id,
                Role = GroupRole.Member,
                JoinedAt = DateTime.UtcNow
            };
            _context.GroupMembers.Add(member);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Te-ai alaturat grupului cu succes!";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // trimite mesaj in grup (PostMessage)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostMessage(int groupId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // verific daca userul e membru al grupului
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == user.Id);
            if (!isMember)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(content) || content.Length > 1000)
            {
                return BadRequest("Continutul mesajului este invalid.");
            }
            var message = new GroupMessage
            {
                GroupId = groupId,
                UserId = user.Id,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            _context.GroupMessages.Add(message);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                messageId = message.Id,
                content = message.Content,
                sentAt = message.SentAt.ToLocalTime().ToString("HH:mm"),
                userName = user.UserName,
                userAvatar = user.ProfilePicture
            });
        }

        // leave grup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var group = await _context.Groups
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == id);
            if (group == null)
            {
                return NotFound();
            }

            var membership = group.Members.FirstOrDefault(m => m.UserId == user.Id);
            if (membership == null)
            {
                return RedirectToAction(nameof(Index));
            }

            // daca proprietarul paraseste grupul
            if (group.OwnerId == user.Id)
            {
                // cautam un succesor
                // prioritate: moderatori => membrii cei mai vechi
                var successor = group.Members
                    .Where(m => m.UserId != user.Id)
                    .OrderByDescending(m => m.Role == GroupRole.Moderator)
                    .ThenBy(m => m.JoinedAt)
                    .FirstOrDefault();

                if (successor != null)
                {
                    // transferam puterea
                    group.OwnerId = successor.User.Id;
                    successor.Role = GroupRole.Admin;

                    _context.Update(group);
                    _context.Update(successor);

                    TempData["Message"] = $"Ai parasit grupul. Proprietatea a fost transferata unui alt membru.";
                }
                else
                {
                    // daca nu mai e alticneva in grup, stergem grupul
                    _context.Groups.Remove(group);
                    await _context.SaveChangesAsync();

                    TempData["Message"] = "Ai parasit grupul. Grupul a fost sters deoarece nu mai are membri.";
                    TempData["MessageType"] = "warning";
                    return RedirectToAction(nameof(Index));
                }
            }

            else
            {
                TempData["Message"] = "Ai parasit grupul cu succes.";
            }

            // stergem membrul
            _context.GroupMembers.Remove(membership);
            await _context.SaveChangesAsync();
            TempData["MessageType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        // stergere grup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var group = await _context.Groups.FindAsync(id);
            if (group == null)
            {
                return NotFound();
            }

            // doar proprietarul poate sterge grupul
            if (group.OwnerId != user.Id)
            {
                return Forbid();
            }

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Grupul a fost sters cu succes.";
            TempData["MessageType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        // stergere mesaj in grup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // caut mesajul 
            var message = await _context.GroupMessages
                .Include(m => m.Group)
                .ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return NotFound();
            }

            // verificam daca userul este admin/moderator sau autorul mesajului
            var membership = message.Group.Members
                .FirstOrDefault(m => m.UserId == user.Id);
            if (membership == null ||
                (membership.Role != GroupRole.Admin && membership.Role != GroupRole.Moderator && message.UserId != user.Id))
            {
                return Forbid();
            }

            _context.GroupMessages.Remove(message);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Mesajul a fost sters cu succes.";
            TempData["MessageType"] = "success";

            return RedirectToAction(nameof(Details), new { id = message.GroupId });
        }

        // promote => face membru moderator
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Promote(int groupId, string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
            {
                return NotFound();
            }

            // verificam daca currentUser este admin
            var currentMembership = group.Members.FirstOrDefault(m => m.UserId == currentUser.Id);
            if (currentMembership == null || currentMembership.Role != GroupRole.Admin)
            {
                return Forbid();
            }

            // cautam membrul de promovat
            var memberToPromote = group.Members.FirstOrDefault(m => m.UserId == userId);
            if (memberToPromote == null)
            {
                return NotFound();
            }

            // promovam membrul
            memberToPromote.Role = GroupRole.Moderator;
            await _context.SaveChangesAsync();
            TempData["Message"] = "Membrul a fost promovat la moderator.";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        // demote => face moderator membru simplu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Demote(int groupId, string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
            {
                return NotFound();
            }

            // verificam daca currentUser este admin
            var currentMembership = group.Members.FirstOrDefault(m => m.UserId == currentUser.Id);
            if (currentMembership == null || currentMembership.Role != GroupRole.Admin)
            {
                return Forbid();
            }

            // cautam membrul de retrogradat
            var memberToDemote = group.Members.FirstOrDefault(m => m.UserId == userId);
            if (memberToDemote == null)
            {
                return NotFound();
            }

            // retrogradam membrul
            memberToDemote.Role = GroupRole.Member;
            await _context.SaveChangesAsync();
            TempData["Message"] = "Membrul a fost demovat la membru simplu.";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        // editare grup - doar pentru admin
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var group = await _context.Groups.FindAsync(id);
            if (group == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || group.OwnerId != currentUser.Id)
            {
                return Forbid();
            }

            return View(group);
        }

        // editare grup (post)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Group group, IFormFile? groupImage)
        {
            if (id != group.Id) return NotFound();

            // caut grupul din baza de date 
            var groupToUpdate = await _context.Groups.FindAsync(id);

            if (groupToUpdate == null) return NotFound();

            // doar adminul poate edita
            var user = await _userManager.GetUserAsync(User);
            if (groupToUpdate.OwnerId != user.Id) return Forbid();

            try
            {
                groupToUpdate.Name = group.Name;
                groupToUpdate.Description = group.Description;

                // poza de grup
                if (groupImage != null && groupImage.Length > 0)
                {
                    // definim calea de stocare
                    var storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/groups");
                    if (!Directory.Exists(storagePath)) Directory.CreateDirectory(storagePath);

                    // generam nume unic pentru fisier
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(groupImage.FileName);
                    var filePath = Path.Combine(storagePath, fileName);

                    // salvam fizic fisierul
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await groupImage.CopyToAsync(stream);
                    }

                    // actualizam calea in baza de date
                    groupToUpdate.GroupPicture = "/images/groups/" + fileName;
                }

                // salvam modificarile
                _context.Update(groupToUpdate);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Grupul a fost actualizat cu succes!";
                TempData["MessageType"] = "success";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Groups.Any(e => e.Id == id)) return NotFound();
                else throw;
            }

            // redirect la detalii grup
            return RedirectToAction(nameof(Details), new { id = groupToUpdate.Id });
        }

        // kick membru
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Kick(int groupId, string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null)
            {
                return NotFound();
            }

            // verificam daca currentUser este admin
            var currentMembership = group.Members.FirstOrDefault(m => m.UserId == currentUser.Id);
            if (currentMembership == null || currentMembership.Role != GroupRole.Admin)
            {
                return Forbid();
            }

            // cautam membrul de dat afara
            var memberToKick = group.Members.FirstOrDefault(m => m.UserId == userId);
            if (memberToKick == null)
            {
                return NotFound();
            }

            // stergem membrul
            // moderatorul nu poate da kick la admin sau la alti moderatori
            if ((memberToKick.Role == GroupRole.Admin || memberToKick.Role == GroupRole.Moderator) && currentMembership.Role == GroupRole.Moderator)
            {
                TempData["Message"] = "Nu ai permisiunea sa dai afara acest membru.";
                TempData["MessageType"] = "danger";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }

            _context.GroupMembers.Remove(memberToKick);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Membrul a fost dat afara cu succes.";
            TempData["MessageType"] = "warning";

            return RedirectToAction(nameof(Details), new { id = groupId });
        }
    }
}
