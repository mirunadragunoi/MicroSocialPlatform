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
    [Authorize] // doar userii autentificati pot umbla la grupuri
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

                TempData["SuccessMessage"] = "Grupul a fost creat cu succes!";

                // redirect la pagina grupului
                return RedirectToAction(nameof(Index));
            }
            return View(group);
        }

        // detalii grup
        [AllowAnonymous] // oricine poate vedea detaliile unui grup
        public async Task<IActionResult> Details(int? id, string? returnUrl = null)
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

            // verificam pentru userul curent daca este membru al grupului
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

            // numarul de cereri in pending -->> doar pentru admin
            if (isAdmin)
            {
                var pendingRequestsCount = await _context.GroupJoinRequests
                    .CountAsync(r => r.GroupId == id && r.Status == GroupJoinRequestStatus.Pending);
                ViewBag.PendingRequestsCount = pendingRequestsCount;
            }
            else
            {
                ViewBag.PendingRequestsCount = 0;
            }

            // verific si daca userul curent are cerere de pending
            if (user != null && !isMember)
            {
                var userRequest = await _context.GroupJoinRequests
                    .FirstOrDefaultAsync(r => r.GroupId == id && r.UserId == user.Id && r.Status == GroupJoinRequestStatus.Pending);
                ViewBag.HasPendingRequest = userRequest != null;
            }
            else
            {
                ViewBag.HasPendingRequest = false;
            }

            ViewBag.ReturnUrl = returnUrl;

            return View(group);
        }

        // join grup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int id, string? returnUrl = null)
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
                TempData["InfoMessage"] = "Esti deja membru al acestui grup.";
                return RedirectToAction(nameof(Details), new { id = id, returnUrl = returnUrl });
            }

            // verific daca nu cumva exista deja o cerere pending
            var existingRequest = await _context.GroupJoinRequests
                 .FirstOrDefaultAsync(r => r.GroupId == id && r.UserId == user.Id && r.Status == GroupJoinRequestStatus.Pending);

            if (existingRequest != null)
            {
                TempData["InfoMessage"] = "Ai deja o cerere în așteptare pentru acest grup.";
                return RedirectToAction(nameof(Details), new { id = id, returnUrl = returnUrl });
            }

            // creez cerere de join in grup
            var request = new GroupJoinRequest
            {
                GroupId = id,
                UserId = user.Id,
                Status = GroupJoinRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            _context.GroupJoinRequests.Add(request);

            // creez notificarea pentru administratorul grupului
            _context.Notifications.Add(new Notification
            {
                RecipientId = group.OwnerId,
                SenderId = user.Id,
                Type = NotificationType.GroupJoinRequest,
                Content = $"{user.UserName} dorește să se alăture grupului \"{group.Name}\"",
                RelatedUrl = $"/Groups/JoinRequests/{group.Id}",
                RelatedEntityId = request.Id,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cererea ta de intrare în grup a fost trimisă! Așteaptă aprobarea administratorului.";

            return RedirectToAction(nameof(Details), new { id = id, returnUrl = returnUrl });
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
            if (!isMember && !User.IsInRole("Administrator"))
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
                userAvatar = user.ProfilePicture,
            });
        }

        // leave grup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int id, string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var group = await _context.Groups
                .Include(g => g.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            var membership = group.Members.FirstOrDefault(m => m.UserId == user.Id);
            if (membership == null) return RedirectToAction(nameof(Index));

            // daca pleaca proprietarul
            if (group.OwnerId == user.Id)
            {
                // cautam succesor (moderatori -> cei mai vechi membri)
                var successor = group.Members
                    .Where(m => m.UserId != user.Id)
                    .OrderByDescending(m => m.Role == GroupRole.Moderator)
                    .ThenBy(m => m.JoinedAt)
                    .FirstOrDefault();

                if (successor != null)
                {
                    // exista urmas -> transferam puterea si stergem membrul
                    group.OwnerId = successor.User.Id;
                    successor.Role = GroupRole.Admin;

                    _context.Update(group);
                    _context.Update(successor);

                    _context.GroupMembers.Remove(membership);

                    TempData["InfoMessage"] = "Ai părăsit grupul. Proprietatea a fost transferată unui alt membru.";
                }
                else
                {
                    // nu exista urmas -> stergem grupul complet
                    _context.Groups.Remove(group);

                    TempData["InfoMessage"] = "Ai părăsit grupul. Grupul a fost șters deoarece nu mai are membri.";
                }
            }
            // daca pleaca un membru simplu sau moderator
            else
            {
                // grupul ramane -> stergem manual membrul
                _context.GroupMembers.Remove(membership);
                TempData["SuccessMessage"] = "Ai părăsit grupul cu succes.";
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
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

            var group = await _context.Groups
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == id);
            if (group == null)
            {
                return NotFound();
            }

            var isAdmin = User.IsInRole("Administrator");

            // doar proprietarul sau adminul platformei poate sterge grupul 
            if (group.OwnerId != user.Id && !isAdmin)
            {
                return Forbid();
            }

            // salvez informatiile inainte de stergere
            var groupName = group.Name;
            var memberIds = group.Members
                .Where(m => m.UserId != user.Id)
                .Select(m => m.UserId)
                .ToList();

            // trimit notificare la toti membrii grupului, fara userului care il sterge
            foreach (var memberId in memberIds)
            {
                var notification = new Notification
                {
                    RecipientId = memberId,
                    SenderId = user.Id,
                    Type = NotificationType.GroupDeleted,
                    Content = $"a șters grupul '{group.Name}' din care făceai parte.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    RelatedUrl = "/Groups/Index"
                };
                _context.Notifications.Add(notification);
            }

            // salvez modificarile inainte de stergere
            if (memberIds.Any())
            {
                await _context.SaveChangesAsync();
            }

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Grupul a fost sters cu succes.";

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
            if ((membership == null ||
                (membership.Role != GroupRole.Admin && membership.Role != GroupRole.Moderator && message.UserId != user.Id)) && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            _context.GroupMessages.Remove(message);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Mesajul a fost sters cu succes.";

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
            if ((currentMembership == null || currentMembership.Role != GroupRole.Admin) && !User.IsInRole("Administrator"))
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
            TempData["InfoMessage"] = "Membrul a fost promovat la moderator.";
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
            if ((currentMembership == null || currentMembership.Role != GroupRole.Admin) && !User.IsInRole("Administrator"))
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
            TempData["InfoMessage"] = "Membrul a fost retrogradat la membru simplu.";
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
            if ((currentUser == null || group.OwnerId != currentUser.Id) && !User.IsInRole("Administrator"))
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
            if ((groupToUpdate.OwnerId != user.Id) && !User.IsInRole("Administrator")) return Forbid();

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

                TempData["SuccessMessage"] = "Grupul a fost actualizat cu succes!";
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
            if ((currentMembership == null || currentMembership.Role != GroupRole.Admin) && !User.IsInRole("Administrator"))
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
            if (!User.IsInRole("Administrator"))
            {
                if (currentMembership != null && // verificam sa nu fie null
                    (memberToKick.Role == GroupRole.Admin || memberToKick.Role == GroupRole.Moderator) &&
                    currentMembership.Role == GroupRole.Moderator)
                {
                    TempData["ErrorMessage"] = "Nu ai permisiunea sa dai afara acest membru.";
                    return RedirectToAction(nameof(Details), new { id = groupId });
                }
            }

            // daca grupul avea un singur membru, il stergem complet
            if (group.Members.Count == 1)
            {
                _context.Groups.Remove(group);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Grupul a fost șters automat deoarece ultimul membru a fost eliminat.";
                return RedirectToAction(nameof(Index)); // ne intoarcem la lista de grupuri
            }

            // daca dam afara proprietarul grupului (dar mai sunt altii)
            if (memberToKick.UserId == group.OwnerId)
            {
                // cautam un urmas
                var successor = group.Members
                    .Where(m => m.UserId != memberToKick.UserId)
                    .OrderByDescending(m => m.Role == GroupRole.Moderator) // prioritate moderatori
                    .ThenBy(m => m.JoinedAt) // apoi vechime
                    .FirstOrDefault();

                if (successor != null)
                {
                    group.OwnerId = successor.UserId;
                    successor.Role = GroupRole.Admin;
                    _context.Update(group);
                    _context.Update(successor);

                    string newAdminName = successor.User != null ? successor.User.UserName : "un alt membru";
                    TempData["InfoMessage"] = $"Proprietarul a fost eliminat. Rolul de Admin a fost transferat catre {newAdminName}.";
                }
            }

            _context.GroupMembers.Remove(memberToKick);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Membrul a fost dat afara cu succes.";

            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        // afisare cereri de intrare in grup ->> doar pentru moderatorul grupului
        [HttpGet]
        public async Task<IActionResult> JoinRequests(int id)
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

            // verific daca userul este admin
            var membership = group.Members.FirstOrDefault(m => m.UserId == user.Id);
            if ((membership == null || membership.Role != GroupRole.Admin) && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            // obtin cererile pending
            var requests = await _context.GroupJoinRequests
                .Include(r => r.User)
                .Where(r => r.GroupId == id && r.Status == GroupJoinRequestStatus.Pending)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            ViewBag.Group = group;
            ViewBag.IsAdmin = true;

            return View(requests);
        }

        // acceptare cerere de join in grup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptJoinRequest(int requestId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var request = await _context.GroupJoinRequests
                .Include(r => r.Group)
                    .ThenInclude(g => g.Members)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                return NotFound();
            }

            // verific daca userul este admin
            var membership = request.Group.Members.FirstOrDefault(m => m.UserId == user.Id);
            if ((membership == null || membership.Role != GroupRole.Admin) && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            // accept cererea -->> adauga automat userul in grup
            var newMember = new GroupMember
            {
                GroupId = request.GroupId,
                UserId = request.UserId,
                Role = GroupRole.Member,
                JoinedAt = DateTime.UtcNow
            };

            _context.GroupMembers.Add(newMember);

            // actualizeaza status cerere
            request.Status = GroupJoinRequestStatus.Accepted;
            request.RespondedAt = DateTime.UtcNow;
            _context.Update(request);

            // creez notificarea pentru utilizatorul care a cerut sa intre in grup
            _context.Notifications.Add(new Notification
            {
                RecipientId = request.UserId,
                SenderId = user.Id,
                Type = NotificationType.GroupJoinAccepted,
                Content = $"Cererea ta de a intra în grupul \"{request.Group.Name}\" a fost acceptată!",
                RelatedUrl = $"/Groups/Details/{request.GroupId}",
                RelatedEntityId = request.GroupId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Ai acceptat cererea lui {request.User.UserName}!";

            return RedirectToAction(nameof(JoinRequests), new { id = request.GroupId });
        }

        // respingere cerere de join in grup 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectJoinRequest(int requestId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var request = await _context.GroupJoinRequests
                .Include(r => r.Group)
                    .ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                return NotFound();
            }

            // verifica daca userul este admin
            var membership = request.Group.Members.FirstOrDefault(m => m.UserId == user.Id);
            if ((membership == null || membership.Role != GroupRole.Admin) && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            // resping cererea
            request.Status = GroupJoinRequestStatus.Rejected;
            request.RespondedAt = DateTime.UtcNow;
            _context.Update(request);

            await _context.SaveChangesAsync();

            TempData["InfoMessage"] = "Cererea a fost respinsă.";

            return RedirectToAction(nameof(JoinRequests), new { id = request.GroupId });
        }

        // editarea unui mesaj in grup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMessage(int messageId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var message = await _context.GroupMessages
                .Include(m => m.Group)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return NotFound();
            }

            // doar autorul mesajului poate sa l editeze
            if (message.UserId != user.Id && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(content) || content.Length > 1000)
            {
                return BadRequest("Conținutul mesajului este invalid.");
            }

            // actualizez mesajul
            message.Content = content;
            message.EditedAt = DateTime.UtcNow;

            _context.Update(message);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                content = message.Content,
                editedAt = message.EditedAt.Value.ToLocalTime().ToString("HH:mm")
            });
        }
    }
}
