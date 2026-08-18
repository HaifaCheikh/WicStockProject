using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static WicStock_.Models.Enums;

namespace WicStock_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context)
        {
            _context = context;
        }

        private RoleUtilisateur? ObtenirRoleUtilisateur()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(roleClaim))
                return null;
            return Enum.TryParse<RoleUtilisateur>(roleClaim, out var role) ? role : null;
        }

        private IQueryable<WicStock_.Models.Notification> QueryPourUtilisateurConnecte()
        {
            var role = ObtenirRoleUtilisateur();
            var query = _context.Notifications.AsQueryable();

            if (role.HasValue)
            {
                query = query.Where(n =>
                    n.RoleDestinataire == null || n.RoleDestinataire == role.Value);
            }

            return query;
        }

        // GET: api/notification
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetNotifications()
        {
            var notifications = await QueryPourUtilisateurConnecte()
                .OrderByDescending(n => n.DateCreation)
                .Take(100)
                .Select(n => new
                {
                    n.Id,
                    Type = n.Type.ToString(),
                    n.Message,
                    n.UrlCible,
                    n.DateCreation,
                    n.Lue,
                    RoleDestinataire = n.RoleDestinataire != null ? n.RoleDestinataire.ToString() : null
                })
                .ToListAsync();

            return Ok(notifications);
        }

        // GET: api/notification/non-lues
        [HttpGet("non-lues")]
        public async Task<ActionResult<IEnumerable<object>>> GetNotificationsNonLues()
        {
            var notifications = await QueryPourUtilisateurConnecte()
                .Where(n => !n.Lue)
                .OrderByDescending(n => n.DateCreation)
                .Take(50)
                .Select(n => new
                {
                    n.Id,
                    Type = n.Type.ToString(),
                    n.Message,
                    n.UrlCible,
                    n.DateCreation,
                    n.Lue,
                    RoleDestinataire = n.RoleDestinataire != null ? n.RoleDestinataire.ToString() : null
                })
                .ToListAsync();

            return Ok(notifications);
        }

        // PUT: api/notification/{id}/marquer-lue
        [HttpPut("{id}/marquer-lue")]
        public async Task<IActionResult> MarquerCommeLue(int id)
        {
            var notification = await QueryPourUtilisateurConnecte()
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notification == null)
                return NotFound();

            notification.Lue = true;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/notification/marquer-toutes-lues
        [HttpPut("marquer-toutes-lues")]
        public async Task<IActionResult> MarquerToutesCommeLues()
        {
            var notifications = await QueryPourUtilisateurConnecte()
                .Where(n => !n.Lue)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.Lue = true;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
