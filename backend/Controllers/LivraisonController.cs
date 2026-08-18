using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WicStock_.Models;
using WicStock_.Models.Dtos;
using WicStock_.Services;
using static WicStock_.Models.Enums;

namespace WicStock_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LivraisonController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly ILogger<LivraisonController> _logger;

        public LivraisonController(AppDbContext context, NotificationService notificationService, ILogger<LivraisonController> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId)) return userId;
            return null;
        }

        // GET: api/livraison/mes-livraisons
        [HttpGet("mes-livraisons")]
        [Authorize(Roles = "LIVREUR")]
        public async Task<ActionResult<IEnumerable<LivraisonCommandeDto>>> GetMesLivraisons()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var livraisons = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .Include(h => h.Utilisateur)
                .Include(h => h.Livreur)
                .Where(h => h.LivreurId == userId.Value)
                .OrderByDescending(h => h.DateVente)
                .Select(h => MapToLivraisonDto(h))
                .ToListAsync();

            return Ok(livraisons);
        }

        // GET: api/livraison/disponibles
        [HttpGet("disponibles")]
        [Authorize(Roles = "LIVREUR")]
        public async Task<ActionResult<IEnumerable<LivraisonCommandeDto>>> GetLivraisonsDisponibles()
        {
            // Commandes sans livreur assigné qui sont prêtes et payées
            var disponibles = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .Include(h => h.Utilisateur)
                .Include(h => h.Livreur)
                .Where(h => h.LivreurId == null
                         && (h.Statut == StatutCommandeDetaille.PAYEE
                          || (h.StatutCommande == "ACCEPTEE" && h.DatePaiement != null && h.Statut != StatutCommandeDetaille.EN_LIVRAISON && h.Statut != StatutCommandeDetaille.LIVREE)))
                .OrderByDescending(h => h.DateVente)
                .Select(h => MapToLivraisonDto(h))
                .ToListAsync();

            return Ok(disponibles);
        }

        // POST: api/livraison/auto-assigner/{commandeId}
        [HttpPost("auto-assigner/{commandeId}")]
        [Authorize(Roles = "LIVREUR")]
        public async Task<IActionResult> AutoAssigner(int commandeId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var commande = await _context.HistoriqueVentes.FindAsync(commandeId);
            if (commande == null) return NotFound("Commande introuvable.");

            if (commande.LivreurId != null)
                return BadRequest("Cette commande est déjà assignée à un livreur.");

            // Vérification paiement & état prêt
            bool estPayee = commande.Statut == StatutCommandeDetaille.PAYEE || commande.DatePaiement != null;
            if (!estPayee)
                return BadRequest("La commande doit être payée avant d'être prise en charge pour la livraison.");

            commande.LivreurId = userId.Value;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Commande assignée avec succès." });
        }

        // POST: api/livraison/passer-en-livraison/{commandeId}
        [HttpPost("passer-en-livraison/{commandeId}")]
        [Authorize(Roles = "LIVREUR")]
        public async Task<IActionResult> PasserEnLivraison(int commandeId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var commande = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .Include(h => h.Livreur)
                .FirstOrDefaultAsync(h => h.Id == commandeId && h.LivreurId == userId.Value);

            if (commande == null)
                return NotFound("Commande introuvable ou vous n'êtes pas le livreur assigné à cette commande.");

            // RÈGLE MÉTIER STRICTE CÔTÉ SERVEUR :
            // Ne peut passer à EN_LIVRAISON que si la commande est marquée Payée (ou PRETE + Payée)
            bool estPayee = commande.Statut == StatutCommandeDetaille.PAYEE || commande.DatePaiement != null;
            if (!estPayee)
            {
                return BadRequest("Règle de livraison : La commande doit être marquée 'Payée' avant de passer en livraison.");
            }

            if (commande.Statut == StatutCommandeDetaille.EN_LIVRAISON)
                return BadRequest("La commande est déjà en cours de livraison.");

            if (commande.Statut == StatutCommandeDetaille.LIVREE)
                return BadRequest("La commande a déjà été livrée.");

            commande.Statut = StatutCommandeDetaille.EN_LIVRAISON;
            await _context.SaveChangesAsync();

            // Notification client
            if (commande.UtilisateurId.HasValue)
            {
                var produitNom = commande.Produit?.Nom ?? "Produit";
                var livreurNom = commande.Livreur != null ? $"{commande.Livreur.Prenom} {commande.Livreur.Nom}".Trim() : "votre livreur";
                await _notificationService.NotifierNouvelEvenementAsync(
                    TypeNotification.COMMANDE_EN_LIVRAISON,
                    $"Votre commande de « {produitNom} » est désormais en cours de livraison par {livreurNom}.",
                    $"/mes-commandes/suivi/{commande.Id}",
                    RoleUtilisateur.CLIENT,
                    commande.UtilisateurId
                );
            }

            return Ok(new { message = "Statut mis à jour : En livraison." });
        }

        // POST: api/livraison/marquer-livree/{commandeId}
        [HttpPost("marquer-livree/{commandeId}")]
        [Authorize(Roles = "LIVREUR")]
        public async Task<IActionResult> MarquerLivree(int commandeId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized();

            var commande = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .FirstOrDefaultAsync(h => h.Id == commandeId && h.LivreurId == userId.Value);

            if (commande == null)
                return NotFound("Commande introuvable ou non assignée à votre compte.");

            if (commande.Statut == StatutCommandeDetaille.LIVREE)
                return BadRequest("La commande est déjà marquée comme livrée.");

            commande.Statut = StatutCommandeDetaille.LIVREE;
            commande.DateLivraison = DateTime.Now;
            await _context.SaveChangesAsync();

            // Notification client
            if (commande.UtilisateurId.HasValue)
            {
                var produitNom = commande.Produit?.Nom ?? "Produit";
                await _notificationService.NotifierNouvelEvenementAsync(
                    TypeNotification.COMMANDE_LIVREE,
                    $"Excellente nouvelle ! Votre commande de « {produitNom} » a été livrée avec succès.",
                    $"/mes-commandes/suivi/{commande.Id}",
                    RoleUtilisateur.CLIENT,
                    commande.UtilisateurId
                );
            }

            return Ok(new { message = "Commande marquée comme livrée avec succès." });
        }

        // GET: api/livraison/livreurs
        [HttpGet("livreurs")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<IEnumerable<LivreurInfoDto>>> GetLivreurs()
        {
            var livreurs = await _context.Utilisateurs
                .Where(u => u.Role == RoleUtilisateur.LIVREUR)
                .Select(u => new LivreurInfoDto
                {
                    Id = u.Id,
                    Nom = $"{u.Prenom} {u.Nom}".Trim(),
                    Email = u.Email,
                    Telephone = u.Telephone
                })
                .ToListAsync();

            return Ok(livreurs);
        }

        // POST: api/livraison/assigner
        [HttpPost("assigner")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> AssignerLivreur([FromBody] AssignerLivreurDto dto)
        {
            var commande = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .FirstOrDefaultAsync(h => h.Id == dto.CommandeId);

            if (commande == null)
                return NotFound("Commande introuvable.");

            var livreur = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.Id == dto.LivreurId && u.Role == RoleUtilisateur.LIVREUR);

            if (livreur == null)
                return BadRequest("Le livreur sélectionné est invalide ou n'a pas le rôle LIVREUR.");

            commande.LivreurId = livreur.Id;
            await _context.SaveChangesAsync();

            // Notification livreur
            var produitNom = commande.Produit?.Nom ?? "Produit";
            await _notificationService.NotifierNouvelEvenementAsync(
                TypeNotification.COMMANDE_ASSIGNATION_LIVREUR,
                $"Une nouvelle commande de « {produitNom} » (Qté : {commande.QuantiteVendue}) vous a été assignée pour livraison.",
                "/livraisons",
                RoleUtilisateur.LIVREUR,
                livreur.Id
            );

            return Ok(new { message = $"Commande #{commande.Id} assignée au livreur {livreur.Prenom} {livreur.Nom}." });
        }

        private static LivraisonCommandeDto MapToLivraisonDto(HistoriqueVente h)
        {
            return new LivraisonCommandeDto
            {
                Id = h.Id,
                DateVente = h.DateVente,
                QuantiteVendue = h.QuantiteVendue,
                PrixUnitaire = h.PrixUnitaire,
                Statut = h.Statut?.ToString(),
                StatutCommande = h.StatutCommande,
                EstSurCommande = h.EstSurCommande,
                ProduitNom = h.Produit?.Nom ?? "Article",
                ProduitReference = h.Produit?.Reference ?? "",
                ProduitImageUrl = h.Produit?.ImageUrl,
                ClientId = h.UtilisateurId,
                ClientNom = h.Utilisateur != null ? $"{h.Utilisateur.Prenom} {h.Utilisateur.Nom}".Trim() : "Client",
                ClientEmail = h.Utilisateur?.Email ?? "",
                ClientTelephone = h.Utilisateur?.Telephone,
                AdresseLivraison = !string.IsNullOrWhiteSpace(h.AdresseLivraison) ? h.AdresseLivraison : h.Utilisateur?.Adresse,
                CodePostal = !string.IsNullOrWhiteSpace(h.CodePostal) ? h.CodePostal : h.Utilisateur?.CodePostal,
                Ville = !string.IsNullOrWhiteSpace(h.Ville) ? h.Ville : h.Utilisateur?.Ville,
                Pays = !string.IsNullOrWhiteSpace(h.Pays) ? h.Pays : "Tunisie",
                LivreurId = h.LivreurId,
                LivreurNom = h.Livreur != null ? $"{h.Livreur.Prenom} {h.Livreur.Nom}".Trim() : null,
                DatePrete = h.DatePrete,
                DatePaiement = h.DatePaiement,
                DateLivraison = h.DateLivraison
            };
        }
    }
}
