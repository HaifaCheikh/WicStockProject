using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WicStock_.Models;
using WicStock_.Models.Dtos;
using static WicStock_.Models.Enums;

namespace WicStock_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AvisController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AvisController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/avis/mes-avis (Avis de l'utilisateur connecté)
        [HttpGet("mes-avis")]
        [Authorize(Roles = "CLIENT,ADMIN")]
        public async Task<ActionResult<IEnumerable<AvisDto>>> GetMesAvis()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var avisList = await _context.Avis
                .Include(a => a.Produit)
                .Include(a => a.Client)
                .Where(a => a.ClientId == userId)
                .OrderByDescending(a => a.DateCreation)
                .ToListAsync();

            return Ok(avisList.Select(MapToDto));
        }

        // GET: api/avis/commande/{commandeId}
        [HttpGet("commande/{commandeId}")]
        public async Task<ActionResult<AvisDto>> GetAvisParCommande(int commandeId)
        {
            var avis = await _context.Avis
                .Include(a => a.Produit)
                .Include(a => a.Client)
                .FirstOrDefaultAsync(a => a.CommandeId == commandeId);

            if (avis == null)
                return NotFound();

            return Ok(MapToDto(avis));
        }

        // GET: api/avis/produit/{produitId} (Avis publics affichables sur la fiche produit)
        [HttpGet("produit/{produitId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<AvisDto>>> GetAvisPublicsProduit(int produitId)
        {
            var avisList = await _context.Avis
                .Include(a => a.Produit)
                .Include(a => a.Client)
                .Where(a => a.ProduitId == produitId && a.Statut == StatutAvis.PUBLIE && !a.EstMasque)
                .OrderByDescending(a => a.DateCreation)
                .ToListAsync();

            return Ok(avisList.Select(MapToDto));
        }

        // POST: api/avis (Créer ou modifier un avis)
        [HttpPost]
        [Authorize(Roles = "CLIENT,ADMIN")]
        public async Task<ActionResult<AvisDto>> SoumettreOuModifierAvis(CreerModifierAvisDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            if (dto.Note < 1 || dto.Note > 5)
                return BadRequest("La note doit être comprise entre 1 et 5 étoiles.");

            var commande = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .FirstOrDefaultAsync(h => h.Id == dto.CommandeId);

            if (commande == null)
                return NotFound("Commande introuvable.");

            if (commande.UtilisateurId != userId && !User.IsInRole("ADMIN"))
                return Forbid("Vous n'êtes pas le propriétaire de cette commande.");

            // Règle 1: Statut doit être LIVREE
            var statutStr = commande.Statut?.ToString() ?? commande.StatutCommande;
            if (!string.Equals(statutStr, "LIVREE", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Vous ne pouvez déposer un avis que sur une commande livrée.");

            // Règle 2: Délai de 14 jours max après livraison
            var dateReference = commande.DateLivraison ?? commande.DateVente;
            if ((DateTime.Now - dateReference).TotalDays > 14)
                return BadRequest("Le délai de 14 jours après la livraison pour déposer un avis est dépassé.");

            var avisExistant = await _context.Avis.FirstOrDefaultAsync(a => a.CommandeId == dto.CommandeId);

            if (avisExistant != null)
            {
                // Modification de l'avis existant
                avisExistant.Note = dto.Note;
                avisExistant.Commentaire = dto.Commentaire;
                avisExistant.DateCreation = DateTime.Now;
                await _context.SaveChangesAsync();

                await _context.Entry(avisExistant).Reference(a => a.Produit).LoadAsync();
                await _context.Entry(avisExistant).Reference(a => a.Client).LoadAsync();

                return Ok(MapToDto(avisExistant));
            }
            else
            {
                // Création d'un nouvel avis
                var nouvelAvis = new Avis
                {
                    CommandeId = commande.Id,
                    ProduitId = commande.ProduitId,
                    ClientId = userId,
                    Note = dto.Note,
                    Commentaire = dto.Commentaire,
                    DateCreation = DateTime.Now,
                    Statut = StatutAvis.PUBLIE,
                    EstMasque = false
                };

                _context.Avis.Add(nouvelAvis);
                await _context.SaveChangesAsync();

                await _context.Entry(nouvelAvis).Reference(a => a.Produit).LoadAsync();
                await _context.Entry(nouvelAvis).Reference(a => a.Client).LoadAsync();

                return CreatedAtAction(nameof(GetAvisParCommande), new { commandeId = me(nouvelAvis.CommandeId) }, MapToDto(nouvelAvis));
            }

            static int me(int id) => id;
        }

        // GET: api/avis/admin (Consultation et modération par Admin)
        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<IEnumerable<AvisDto>>> GetTousLesAvisAdmin()
        {
            var avisList = await _context.Avis
                .Include(a => a.Produit)
                .Include(a => a.Client)
                .OrderByDescending(a => a.DateCreation)
                .ToListAsync();

            return Ok(avisList.Select(MapToDto));
        }

        // PUT: api/avis/admin/{id}/visibilite
        [HttpPut("admin/{id}/visibilite")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ModererAvis(int id, ModererAvisDto dto)
        {
            var avis = await _context.Avis.FindAsync(id);
            if (avis == null)
                return NotFound("Avis introuvable.");

            avis.Statut = dto.Statut;
            avis.EstMasque = dto.EstMasque;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Statut et visibilité de l'avis mis à jour." });
        }

        // DELETE: api/avis/admin/{id} (Suppression définitive par Admin)
        [HttpDelete("admin/{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> SupprimerAvis(int id)
        {
            var avis = await _context.Avis.FindAsync(id);
            if (avis == null)
                return NotFound("Avis introuvable.");

            _context.Avis.Remove(avis);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static AvisDto MapToDto(Avis a) => new()
        {
            Id = a.Id,
            CommandeId = a.CommandeId,
            ProduitId = a.ProduitId,
            ProduitNom = a.Produit?.Nom,
            ProduitReference = a.Produit?.Reference,
            ProduitImageUrl = a.Produit?.ImageUrl,
            ClientId = a.ClientId,
            ClientNom = a.Client != null ? $"{a.Client.Prenom} {a.Client.Nom}" : "Client",
            Note = a.Note,
            Commentaire = a.Commentaire,
            DateCreation = a.DateCreation,
            Statut = a.Statut.ToString(),
            EstMasque = a.EstMasque
        };
    }
}
