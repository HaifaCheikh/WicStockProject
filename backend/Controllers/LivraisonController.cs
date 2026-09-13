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
                    .ThenInclude(p => p!.Variantes)
                .Include(h => h.Utilisateur)
                .Include(h => h.Livreur)
                .Include(h => h.LigneCommandes)
                    .ThenInclude(l => l.Produit)
                .Include(h => h.LigneCommandes)
                    .ThenInclude(l => l.VarianteProduit)
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
                    .ThenInclude(p => p!.Variantes)
                .Include(h => h.Utilisateur)
                .Include(h => h.Livreur)
                .Include(h => h.LigneCommandes)
                    .ThenInclude(l => l.Produit)
                .Include(h => h.LigneCommandes)
                    .ThenInclude(l => l.VarianteProduit)
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

        private static (string? Genre, string? Taille, string? Couleur) ResolveAttributs(
            string? genre,
            string? taille,
            string? couleur,
            string? refStr,
            string? nomStr,
            VarianteProduit? variante,
            Produit? produit)
        {
            var g = !string.IsNullOrWhiteSpace(genre) ? genre.Trim()
                : (!string.IsNullOrWhiteSpace(variante?.Genre) ? variante.Genre.Trim()
                : (!string.IsNullOrWhiteSpace(produit?.Genre) ? produit.Genre.Trim()
                : (produit?.Variantes?.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.Genre))?.Genre?.Trim())));

            var t = !string.IsNullOrWhiteSpace(taille) ? taille.Trim()
                : (!string.IsNullOrWhiteSpace(variante?.Taille) ? variante.Taille.Trim()
                : (!string.IsNullOrWhiteSpace(produit?.Taille) ? produit.Taille.Trim()
                : (produit?.Variantes?.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.Taille))?.Taille?.Trim())));

            var c = !string.IsNullOrWhiteSpace(couleur) ? couleur.Trim()
                : (!string.IsNullOrWhiteSpace(variante?.Couleur) ? variante.Couleur.Trim()
                : (!string.IsNullOrWhiteSpace(produit?.Couleur) ? produit.Couleur.Trim()
                : (produit?.Variantes?.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.Couleur))?.Couleur?.Trim())));

            var textCombined = $"{(refStr ?? "")} {(nomStr ?? "")}".Trim();

            if (string.IsNullOrWhiteSpace(g))
            {
                if (textCombined.Contains("Dress", StringComparison.OrdinalIgnoreCase) ||
                    textCombined.Contains("Robe", StringComparison.OrdinalIgnoreCase) ||
                    textCombined.Contains("Jupe", StringComparison.OrdinalIgnoreCase) ||
                    textCombined.Contains("Femme", StringComparison.OrdinalIgnoreCase) ||
                    textCombined.Contains("-F-", StringComparison.OrdinalIgnoreCase) ||
                    textCombined.EndsWith("-F", StringComparison.OrdinalIgnoreCase))
                {
                    g = "Femme";
                }
                else if (textCombined.Contains("Homme", StringComparison.OrdinalIgnoreCase) ||
                         textCombined.Contains("-H-", StringComparison.OrdinalIgnoreCase) ||
                         textCombined.EndsWith("-H", StringComparison.OrdinalIgnoreCase))
                {
                    g = "Homme";
                }
            }

            if (string.IsNullOrWhiteSpace(c))
            {
                if (textCombined.Contains("Indigo", StringComparison.OrdinalIgnoreCase)) c = "Bleu indigo";
                else if (textCombined.Contains("Light Blue", StringComparison.OrdinalIgnoreCase) || textCombined.Contains("Bleu Ciel", StringComparison.OrdinalIgnoreCase)) c = "Bleu ciel";
                else if (textCombined.Contains("Dark Blue", StringComparison.OrdinalIgnoreCase) || textCombined.Contains("Marine", StringComparison.OrdinalIgnoreCase)) c = "Bleu marine";
                else if (textCombined.Contains("Bleu", StringComparison.OrdinalIgnoreCase) || textCombined.Contains("Blue", StringComparison.OrdinalIgnoreCase)) c = "Bleu";
                else if (textCombined.Contains("Noir", StringComparison.OrdinalIgnoreCase) || textCombined.Contains("Black", StringComparison.OrdinalIgnoreCase)) c = "Noir";
                else if (textCombined.Contains("Blanc", StringComparison.OrdinalIgnoreCase) || textCombined.Contains("White", StringComparison.OrdinalIgnoreCase)) c = "Blanc";
                else if (textCombined.Contains("Gris", StringComparison.OrdinalIgnoreCase) || textCombined.Contains("Grey", StringComparison.OrdinalIgnoreCase)) c = "Gris";
                else if (textCombined.Contains("Rouge", StringComparison.OrdinalIgnoreCase) || textCombined.Contains("Red", StringComparison.OrdinalIgnoreCase)) c = "Rouge";
                else if (textCombined.Contains("Vert", StringComparison.OrdinalIgnoreCase) || textCombined.Contains("Green", StringComparison.OrdinalIgnoreCase)) c = "Vert";
            }

            if (string.IsNullOrWhiteSpace(t))
            {
                var parts = textCombined.Split(new[] { '-', ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    var pUpper = p.ToUpperInvariant();
                    if (pUpper is "XS" or "S" or "M" or "L" or "XL" or "XXL" or "36" or "38" or "40" or "42" or "44" or "46")
                    {
                        t = pUpper;
                        break;
                    }
                }
                if (string.IsNullOrWhiteSpace(t))
                {
                    t = "M";
                }
            }

            return (g, t, c);
        }

        private static LivraisonCommandeDto MapToLivraisonDto(HistoriqueVente h)
        {
            var lignesDto = new List<LigneCommandeResultDto>();

            if (h.LigneCommandes != null && h.LigneCommandes.Any())
            {
                foreach (var l in h.LigneCommandes)
                {
                    var refStr = l.VarianteProduit?.Reference ?? l.Produit?.Reference ?? h.Produit?.Reference ?? "";
                    var nomStr = l.Produit?.Nom ?? h.Produit?.Nom ?? "";
                    var (gL, tL, cL) = ResolveAttributs(l.Genre, l.Taille, l.Couleur, refStr, nomStr, l.VarianteProduit, l.Produit ?? h.Produit);

                    lignesDto.Add(new LigneCommandeResultDto
                    {
                        ProduitId = l.ProduitId,
                        VarianteProduitId = l.VarianteProduitId,
                        ProduitNom = nomStr,
                        ProduitReference = refStr,
                        ProduitImageUrl = l.Produit?.ImageUrl ?? h.Produit?.ImageUrl,
                        Genre = gL,
                        Taille = tL,
                        Couleur = cL,
                        Quantite = l.Quantite,
                        PrixUnitaire = l.PrixUnitaire,
                        EstSurCommande = l.EstSurCommande
                    });
                }
            }

            var firstLine = lignesDto.FirstOrDefault();
            var hRefStr = h.Produit?.Reference ?? firstLine?.ProduitReference ?? "";
            var hNomStr = h.Produit?.Nom ?? firstLine?.ProduitNom ?? "Article";
            var (hG, hT, hC) = ResolveAttributs(h.Genre, h.Taille, h.Couleur, hRefStr, hNomStr, null, h.Produit);

            bool estMulti = h.EstMultiLignes && lignesDto.Count > 1;

            return new LivraisonCommandeDto
            {
                Id = h.Id,
                DateVente = h.DateVente,
                QuantiteVendue = estMulti ? lignesDto.Sum(l => l.Quantite) : h.QuantiteVendue,
                PrixUnitaire = h.PrixUnitaire,
                Statut = h.Statut?.ToString(),
                StatutCommande = h.StatutCommande,
                EstSurCommande = h.EstSurCommande,
                ProduitNom = estMulti ? $"{lignesDto.Count} articles" : hNomStr,
                ProduitReference = hRefStr,
                ProduitImageUrl = h.Produit?.ImageUrl ?? firstLine?.ProduitImageUrl,
                Genre = hG,
                Taille = hT,
                Couleur = hC,
                EstMultiLignes = estMulti,
                Lignes = lignesDto,
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
