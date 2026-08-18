using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WicStock_.Models;
using static WicStock_.Models.Enums;

namespace WicStock_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
    public class AlerteController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AlerteController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/alerte
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Alerte>>> GetAlertes()
        {
            return await _context.Alertes
                .Include(a => a.Produit)
                .Include(a => a.Utilisateur)
                .OrderByDescending(a => a.DateDetection)
                .ToListAsync();
        }

        // GET: api/alerte/non-traitees
        [HttpGet("non-traitees")]
        public async Task<ActionResult<IEnumerable<Alerte>>> GetAlertesNonTraitees()
        {
            return await _context.Alertes
                .Include(a => a.Produit)
                .Where(a => a.Statut == StatutAlerte.NON_TRAITEE)
                .OrderByDescending(a => a.NiveauCriticite)
                .ToListAsync();
        }

        // GET: api/alerte/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Alerte>> GetAlerte(int id)
        {
            var alerte = await _context.Alertes
                .Include(a => a.Produit)
                .Include(a => a.Utilisateur)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (alerte == null)
                return NotFound();

            return alerte;
        }

        // POST: api/alerte
        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<Alerte>> CreerAlerte(Alerte alerte)
        {
            _context.Alertes.Add(alerte);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAlerte), new { id = alerte.Id }, alerte);
        }

        // PUT: api/alerte/5/traiter
        // Endpoint dédié pour qu'un utilisateur prenne en charge une alerte
        [HttpPut("{id}/traiter")]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<IActionResult> TraiterAlerte(int id, [FromBody] int utilisateurId)
        {
            var alerte = await _context.Alertes.FindAsync(id);
            if (alerte == null)
                return NotFound();

            alerte.Statut = StatutAlerte.EN_COURS;
            alerte.UtilisateurId = utilisateurId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/alerte/5
        [HttpPut("{id}")]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<IActionResult> ModifierAlerte(int id, Alerte alerte)
        {
            if (id != alerte.Id)
                return BadRequest();

            _context.Entry(alerte).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Alertes.Any(a => a.Id == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // DELETE: api/alerte/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> SupprimerAlerte(int id)
        {
            var alerte = await _context.Alertes.FindAsync(id);
            if (alerte == null)
                return NotFound();

            _context.Alertes.Remove(alerte);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}