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
    [Authorize]
    public class HistoriqueVenteController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notificationService;

        public HistoriqueVenteController(AppDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: api/historiquevente (Vue globale pour Responsable Stock & Production)
        [HttpGet]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<ActionResult<IEnumerable<object>>> GetHistoriqueVentes()
        {
            var ventes = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .Include(h => h.Utilisateur)
                .Include(h => h.Responsable)
                .Include(h => h.Livreur)
                .OrderByDescending(h => h.DateVente)
                .ToListAsync();

            var result = ventes.Select(h => new
            {
                h.Id,
                h.DateVente,
                h.QuantiteVendue,
                h.PrixUnitaire,
                h.StatutCommande,
                Statut = h.Statut?.ToString(),
                h.EstSurCommande,
                h.ProduitId,
                ProduitNom = h.Produit?.Nom,
                ProduitReference = h.Produit?.Reference,
                h.UtilisateurId,
                h.DateSouhaitee,
                h.DateEstimeePreparation,
                h.DatePaiement,
                h.ResponsableId,
                ResponsableNom = h.Responsable != null ? $"{h.Responsable.Prenom} {h.Responsable.Nom}" : null,
                h.LivreurId,
                LivreurNom = h.Livreur != null ? $"{h.Livreur.Prenom} {h.Livreur.Nom}" : null,
                ClientNom = h.Utilisateur != null ? $"{h.Utilisateur.Prenom} {h.Utilisateur.Nom}" : "Client anonyme",
                ClientEmail = h.Utilisateur?.Email
            });

            return Ok(result);
        }

        // GET: api/historiquevente/mes-commandes (Historique du CLIENT connecté)
        [HttpGet("mes-commandes")]
        [Authorize(Roles = "CLIENT,ADMIN")]
        public async Task<ActionResult<IEnumerable<object>>> GetMesCommandes()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var commandes = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .Where(h => h.UtilisateurId == userId)
                .OrderByDescending(h => h.DateVente)
                .ToListAsync();

            var result = commandes.Select(h => new
            {
                h.Id,
                h.DateVente,
                h.QuantiteVendue,
                h.PrixUnitaire,
                h.StatutCommande,
                Statut = h.Statut?.ToString(),
                h.EstSurCommande,
                h.ProduitId,
                ProduitNom = h.Produit?.Nom,
                ProduitReference = h.Produit?.Reference,
                ProduitCategorie = h.Produit?.Categorie,
                ProduitImageUrl = h.Produit?.ImageUrl,
                h.DateSouhaitee,
                h.DateEstimeePreparation,
                TotalCommande = h.QuantiteVendue * h.PrixUnitaire
            });

            return Ok(result);
        }

        // GET: api/historiquevente/produit/3
        [HttpGet("produit/{produitId}")]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<ActionResult<IEnumerable<HistoriqueVente>>> GetParProduit(int produitId)
        {
            return await _context.HistoriqueVentes
                .Where(h => h.ProduitId == produitId)
                .OrderByDescending(h => h.DateVente)
                .ToListAsync();
        }

        // POST: api/historiquevente (Passer une commande client)
        [HttpPost]
        [Authorize(Roles = "CLIENT,ADMIN")]
        public async Task<ActionResult<object>> CreerVente(CommandeDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? utilisateurId = int.TryParse(userIdClaim, out int uid) ? uid : null;

            var produit = await _context.Produits
                .Include(p => p.Stock)
                .FirstOrDefaultAsync(p => p.Id == dto.ProduitId);

            if (produit == null)
                return NotFound("Produit introuvable.");

            var stockQty = produit.Stock?.QuantiteActuelle ?? 0;

            if (stockQty <= 0 && !produit.DisponibleSurCommande)
                return BadRequest(new { message = "Ce produit est en rupture de stock." });

            if (stockQty < dto.QuantiteVendue && !produit.DisponibleSurCommande)
                return BadRequest(new { message = $"Stock insuffisant. Seulement {stockQty} pièce(s) disponible(s)." });

            string statut = "EN_ATTENTE";
            StatutCommandeDetaille? statutDetaille = null;
            bool estSurCommande = false;
            var stock = produit.Stock;

            if (stock != null && stock.QuantiteActuelle >= dto.QuantiteVendue)
            {
                statut = "ACCEPTEE";
                statutDetaille = StatutCommandeDetaille.ACCEPTEE;
                stock.QuantiteActuelle -= dto.QuantiteVendue;
                stock.DateMiseAJour = DateTime.Now;
            }
            else if (produit.DisponibleSurCommande)
            {
                statut = "EN_ATTENTE";
                statutDetaille = StatutCommandeDetaille.EN_ATTENTE_CONFIRMATION;
                estSurCommande = true;
            }

            var vente = new HistoriqueVente
            {
                ProduitId = dto.ProduitId,
                QuantiteVendue = dto.QuantiteVendue,
                PrixUnitaire = dto.PrixUnitaire,
                StatutCommande = statut,
                Statut = statutDetaille,
                EstSurCommande = estSurCommande,
                DateVente = DateTime.Now,
                UtilisateurId = utilisateurId,
                DateSouhaitee = dto.DateSouhaitee?.Date
            };

            _context.HistoriqueVentes.Add(vente);
            await _context.SaveChangesAsync();

            if (statut == "EN_ATTENTE")
            {
                await _notificationService.NotifierNouvelEvenementAsync(
                    TypeNotification.COMMANDE_EN_ATTENTE,
                    $"Nouvelle commande en attente : {dto.QuantiteVendue} × « {produit.Nom} ».",
                    "/commandes",
                    RoleUtilisateur.RESPONSABLE_STOCK_PRODUCTION
                );
            }

            return CreatedAtAction(nameof(GetMesCommandes), new { }, new
            {
                vente.Id,
                vente.DateVente,
                vente.QuantiteVendue,
                vente.PrixUnitaire,
                vente.StatutCommande,
                Statut = vente.Statut?.ToString(),
                vente.EstSurCommande,
                vente.ProduitId,
                vente.UtilisateurId,
                vente.DateSouhaitee,
                Message = statut == "ACCEPTEE"
                    ? "Commande acceptée immédiatement."
                    : estSurCommande
                        ? "Commande sur commande enregistrée, en attente de confirmation."
                        : "Commande enregistrée en attente de validation."
            });
        }

        // PUT: api/historiquevente/5/accepter (Manager accepte la commande)
        [HttpPut("{id}/accepter")]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<IActionResult> AccepterCommande(int id, [FromBody] AccepterCommandeDto? dto = null)
        {
            var vente = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (vente == null)
                return NotFound("Commande introuvable.");

            if (vente.StatutCommande == "ACCEPTEE")
                return BadRequest("La commande est déjà acceptée.");

            // Pour les commandes sur commande, seul l'Admin peut accepter
            if (vente.EstSurCommande)
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "ADMIN")
                    return Forbid("Seul un administrateur peut accepter une commande sur commande.");
            }

            var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.ProduitId == vente.ProduitId);
            var stockDisponible = stock?.QuantiteActuelle ?? 0;

            if (stock != null && stockDisponible >= vente.QuantiteVendue)
            {
                stock.QuantiteActuelle -= vente.QuantiteVendue;
                stock.DateMiseAJour = DateTime.Now;
                vente.StatutCommande = "ACCEPTEE";

                // Créer un mouvement de stock de type SORTIE
                var mouvement = new MouvementStock
                {
                    StockId = stock.Id,
                    Type = TypeMouvement.SORTIE,
                    Quantite = vente.QuantiteVendue,
                    Date = DateTime.Now
                };
                _context.MouvementsStock.Add(mouvement);

                await _context.SaveChangesAsync();

                await _notificationService.NotifierNouvelEvenementAsync(
                    TypeNotification.COMMANDE_CONFIRMEE,
                    $"Votre commande « {vente.Produit?.Nom} » ({vente.QuantiteVendue} pièce(s)) est confirmée et prête.",
                    "/catalogue",
                    RoleUtilisateur.CLIENT
                );

                return Ok(new { message = "Commande acceptée avec succès.", statut = vente.StatutCommande });
            }

            if (dto?.DateEstimeePreparation.HasValue == true)
            {
                vente.DateEstimeePreparation = dto.DateEstimeePreparation.Value.Date;
                vente.StatutCommande = "EN_ATTENTE";
                await _context.SaveChangesAsync();

                var dateStr = vente.DateEstimeePreparation.Value.ToString("dd/MM/yyyy");
                await _notificationService.NotifierNouvelEvenementAsync(
                    TypeNotification.COMMANDE_CONFIRMEE,
                    $"Votre commande « {vente.Produit?.Nom} » est confirmée. Préparation estimée le {dateStr}.",
                    "/catalogue",
                    RoleUtilisateur.CLIENT
                );

                return Ok(new
                {
                    message = $"Commande confirmée. Préparation estimée le {dateStr}.",
                    statut = vente.StatutCommande,
                    vente.DateEstimeePreparation
                });
            }

            return BadRequest(new
            {
                message = $"Stock insuffisant ({stockDisponible} disponible(s) pour {vente.QuantiteVendue} demandée(s)). Indiquez une date de préparation estimée."
            });
        }

        // PUT: api/historiquevente/modifier/{id} (Modifier une commande client)
        [HttpPut("modifier/{id}")]
        [Authorize(Roles = "CLIENT,ADMIN")]
        public async Task<IActionResult> ModifierCommande(int id, CommandeDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var vente = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .ThenInclude(p => p!.Stock)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (vente == null)
                return NotFound("Commande introuvable.");

            if (vente.UtilisateurId != userId && !User.IsInRole("ADMIN"))
                return Forbid();

            if (vente.StatutCommande == "ACCEPTEE")
                return BadRequest(new { message = "Impossible de modifier une commande déjà acceptée." });

            var produit = vente.Produit;
            if (produit == null)
                return BadRequest(new { message = "Produit introuvable." });

            var stockQty = produit.Stock?.QuantiteActuelle ?? 0;

            if (stockQty <= 0 && !produit.DisponibleSurCommande)
                return BadRequest(new { message = "Produit en rupture de stock." });

            if (stockQty < dto.QuantiteVendue && !produit.DisponibleSurCommande)
                return BadRequest(new { message = $"Stock insuffisant ({stockQty} disponible(s))." });

            vente.QuantiteVendue = dto.QuantiteVendue;
            vente.PrixUnitaire = dto.PrixUnitaire;
            vente.DateSouhaitee = dto.DateSouhaitee?.Date;
            vente.DateEstimeePreparation = null;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Commande modifiée avec succès." });
        }

        // PUT: api/historiquevente/5/refuser (Manager refuse la commande)
        [HttpPut("{id}/refuser")]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<IActionResult> RefuserCommande(int id)
        {
            var vente = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (vente == null)
                return NotFound("Commande introuvable.");

            // Pour les commandes sur commande, seul l'Admin peut refuser
            if (vente.EstSurCommande)
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "ADMIN")
                    return Forbid("Seul un administrateur peut refuser une commande sur commande.");
            }

            if (vente.StatutCommande == "REFUSEE")
                return BadRequest("La commande est déjà refusée.");

            if (vente.StatutCommande == "ACCEPTEE")
            {
                var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.ProduitId == vente.ProduitId);
                if (stock != null)
                {
                    stock.QuantiteActuelle += vente.QuantiteVendue;
                    stock.DateMiseAJour = DateTime.Now;
                }
            }

            _context.HistoriqueVentes.Remove(vente);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Commande refusée et supprimée.", statut = "REFUSEE" });
        }

        // GET: api/historiquevente/{id}/suivi
        [HttpGet("{id}/suivi")]
        [Authorize(Roles = "CLIENT,ADMIN,RESPONSABLE_STOCK_PRODUCTION")]
        public async Task<ActionResult<SuiviCommandeDto>> GetSuiviCommande(int id)
        {
            var vente = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .Include(h => h.Responsable)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (vente == null)
                return NotFound("Commande introuvable.");

            if (User.IsInRole("CLIENT"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                    return Unauthorized();

                if (vente.UtilisateurId != userId)
                    return Forbid();
            }

            return Ok(new SuiviCommandeDto
            {
                Id = vente.Id,
                ProduitId = vente.ProduitId,
                ProduitNom = vente.Produit?.Nom,
                ProduitReference = vente.Produit?.Reference,
                ProduitImageUrl = vente.Produit?.ImageUrl,
                QuantiteVendue = vente.QuantiteVendue,
                PrixUnitaire = vente.PrixUnitaire,
                Statut = vente.Statut?.ToString() ?? vente.StatutCommande,
                StatutCommande = vente.StatutCommande,
                EstSurCommande = vente.EstSurCommande,
                DateVente = vente.DateVente,
                DateSouhaitee = vente.DateSouhaitee,
                DateConfirmation = vente.DateConfirmation,
                DateDebutPreparation = vente.DateDebutPreparation,
                DateEstimeePreparation = vente.DateEstimeePreparation,
                DatePrete = vente.DatePrete,
                DatePaiement = vente.DatePaiement,
                DateLivraison = vente.DateLivraison,
                PaymentIntentId = vente.PaymentIntentId,
                AdresseLivraison = vente.AdresseLivraison,
                CodePostal = vente.CodePostal,
                Ville = vente.Ville,
                Pays = vente.Pays,
                ResponsableId = vente.ResponsableId,
                ResponsableNom = vente.Responsable != null ? $"{vente.Responsable.Prenom} {vente.Responsable.Nom}" : null
            });
        }

        // PUT: api/historiquevente/{id}/confirmer
        [HttpPut("{id}/confirmer")]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<IActionResult> ConfirmerCommande(int id, [FromBody] ConfirmerCommandeDto? dto = null)
        {
            var vente = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (vente == null)
                return NotFound("Commande introuvable.");

            if (!vente.EstSurCommande)
                return BadRequest("Cette commande ne fait pas partie du cycle sur commande.");

            if (vente.Statut != StatutCommandeDetaille.EN_ATTENTE_CONFIRMATION)
                return BadRequest("La commande doit être en attente de confirmation pour cette action.");

            // Vérifier que l'utilisateur est le responsable assigné ou un admin
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            // Si c'est un responsable, vérifier qu'il est assigné à cette commande
            if (userRole == "RESPONSABLE_STOCK_PRODUCTION" && vente.ResponsableId != userId)
                return Forbid("Vous n'êtes pas assigné à cette commande.");

            vente.Statut = StatutCommandeDetaille.CONFIRMEE;
            vente.DateConfirmation = DateTime.Now;

            if (dto?.DateEstimeePreparation.HasValue == true)
                vente.DateEstimeePreparation = dto.DateEstimeePreparation.Value.Date;

            await _context.SaveChangesAsync();

            await _notificationService.NotifierNouvelEvenementAsync(
                TypeNotification.COMMANDE_CONFIRMEE,
                "Votre commande est confirmée",
                $"/mes-commandes/suivi/{vente.Id}",
                RoleUtilisateur.CLIENT
            );

            return Ok(new { message = "Commande confirmée.", statut = vente.Statut.ToString(), vente.DateConfirmation, vente.DateEstimeePreparation });
        }

        // PUT: api/historiquevente/{id}/demarrer-preparation
        [HttpPut("{id}/demarrer-preparation")]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<IActionResult> DemarrerPreparation(int id)
        {
            var vente = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (vente == null)
                return NotFound("Commande introuvable.");

            if (!vente.EstSurCommande)
                return BadRequest("Cette commande ne fait pas partie du cycle sur commande.");

            if (vente.Statut != StatutCommandeDetaille.CONFIRMEE)
                return BadRequest("La commande doit être confirmée pour démarrer la préparation.");

            // Pour les commandes sur commande, seul le responsable assigné peut démarrer la préparation
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (vente.EstSurCommande)
            {
                if (userRole == "ADMIN")
                    return Forbid("Les administrateurs ne peuvent pas démarrer la préparation des commandes sur commande.");
                
                if (userRole == "RESPONSABLE_STOCK_PRODUCTION" && vente.ResponsableId != userId)
                    return Forbid("Vous n'êtes pas assigné à cette commande.");
            }

            vente.Statut = StatutCommandeDetaille.EN_PREPARATION;
            vente.DateDebutPreparation = DateTime.Now;

            await _context.SaveChangesAsync();

            await _notificationService.NotifierNouvelEvenementAsync(
                TypeNotification.COMMANDE_CONFIRMEE,
                "Votre commande est en cours de préparation",
                $"/mes-commandes/suivi/{vente.Id}",
                RoleUtilisateur.CLIENT
            );

            return Ok(new { message = "Préparation démarrée.", statut = vente.Statut.ToString(), vente.DateDebutPreparation });
        }

        // PUT: api/historiquevente/{id}/marquer-prete
        [HttpPut("{id}/marquer-prete")]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<IActionResult> MarquerPrete(int id)
        {
            var vente = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (vente == null)
                return NotFound("Commande introuvable.");

            if (!vente.EstSurCommande)
                return BadRequest("Cette commande ne fait pas partie du cycle sur commande.");

            if (vente.Statut != StatutCommandeDetaille.EN_PREPARATION)
                return BadRequest("La commande doit être en préparation pour être marquée prête.");

            // Pour les commandes sur commande, seul le responsable assigné peut marquer comme prête
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (vente.EstSurCommande)
            {
                if (userRole == "ADMIN")
                    return Forbid("Les administrateurs ne peuvent pas marquer comme prête les commandes sur commande.");
                
                if (userRole == "RESPONSABLE_STOCK_PRODUCTION" && vente.ResponsableId != userId)
                    return Forbid("Vous n'êtes pas assigné à cette commande.");
            }

            var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.ProduitId == vente.ProduitId);
            if (stock == null)
                return BadRequest("Stock introuvable pour ce produit.");

            if (stock.QuantiteActuelle < vente.QuantiteVendue)
                return BadRequest(new { message = $"Stock insuffisant ({stock.QuantiteActuelle} disponible(s) pour {vente.QuantiteVendue} demandée(s))." });

            stock.QuantiteActuelle -= vente.QuantiteVendue;
            stock.DateMiseAJour = DateTime.Now;

            _context.MouvementsStock.Add(new MouvementStock
            {
                StockId = stock.Id,
                Type = TypeMouvement.SORTIE,
                Quantite = vente.QuantiteVendue,
                Date = DateTime.Now,
                Motif = $"Commande sur commande prête - {vente.Produit?.Nom ?? "Produit"}"
            });

            vente.Statut = StatutCommandeDetaille.PRETE;
            vente.StatutCommande = "ACCEPTEE";
            vente.DatePrete = DateTime.Now;

            await _context.SaveChangesAsync();

            await _notificationService.NotifierNouvelEvenementAsync(
                TypeNotification.COMMANDE_CONFIRMEE,
                "Votre commande est prête !",
                $"/mes-commandes/suivi/{vente.Id}",
                RoleUtilisateur.CLIENT
            );

            return Ok(new { message = "Commande marquée prête, stock mis à jour.", statut = vente.Statut.ToString(), vente.DatePrete });
        }

        // POST: api/historiquevente/import
        [HttpPost("import")]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<ActionResult> ImporterVentes(List<HistoriqueVente> ventes)
        {
            _context.HistoriqueVentes.AddRange(ventes);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"{ventes.Count} ventes importées." });
        }

        // DELETE: api/historiquevente/annuler/5 (Annulation d'une commande par un CLIENT)
        [HttpDelete("annuler/{id}")]
        [Authorize(Roles = "CLIENT,ADMIN")]
        public async Task<IActionResult> AnnulerCommande(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var vente = await _context.HistoriqueVentes.FindAsync(id);
            if (vente == null)
                return NotFound("Commande introuvable.");

            if (vente.UtilisateurId != userId && !User.IsInRole("ADMIN"))
                return Forbid();

            if (vente.StatutCommande == "ACCEPTEE")
            {
                var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.ProduitId == vente.ProduitId);
                if (stock != null)
                {
                    stock.QuantiteActuelle += vente.QuantiteVendue;
                    stock.DateMiseAJour = DateTime.Now;
                }
            }

            _context.HistoriqueVentes.Remove(vente);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Commande annulée avec succès." });
        }

        // GET: api/historiquevente/responsables
        [HttpGet("responsables")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<IEnumerable<ResponsableDto>>> GetResponsables()
        {
            var responsables = await _context.Utilisateurs
                .Where(u => u.Role == RoleUtilisateur.RESPONSABLE_STOCK_PRODUCTION)
                .Select(u => new ResponsableDto
                {
                    Id = u.Id,
                    Nom = u.Nom,
                    Prenom = u.Prenom,
                    Email = u.Email
                })
                .ToListAsync();

            return Ok(responsables);
        }

        // POST: api/historiquevente/{id}/assigner
        [HttpPost("{id}/assigner")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> AssignerCommande(int id, [FromBody] AssignerCommandeDto dto)
        {
            var vente = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (vente == null)
                return NotFound("Commande introuvable.");

            if (!vente.EstSurCommande)
                return BadRequest("Cette commande ne fait pas partie du cycle sur commande.");

            if (vente.Statut != StatutCommandeDetaille.EN_ATTENTE_CONFIRMATION && vente.Statut != StatutCommandeDetaille.CONFIRMEE)
                return BadRequest("La commande doit être en attente de confirmation ou confirmée pour être assignée.");

            var responsable = await _context.Utilisateurs.FindAsync(dto.ResponsableId);
            if (responsable == null)
                return NotFound("Responsable introuvable.");

            if (responsable.Role != RoleUtilisateur.RESPONSABLE_STOCK_PRODUCTION)
                return BadRequest("L'utilisateur sélectionné n'a pas le rôle RESPONSABLE_STOCK_PRODUCTION.");

            vente.ResponsableId = dto.ResponsableId;
            
            // Si la commande était en attente, la passer automatiquement à confirmée lors de l'assignation
            if (vente.Statut == StatutCommandeDetaille.EN_ATTENTE_CONFIRMATION)
            {
                vente.Statut = StatutCommandeDetaille.CONFIRMEE;
                vente.DateConfirmation = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // Notifier le responsable assigné spécifiquement
            await _notificationService.NotifierNouvelEvenementAsync(
                TypeNotification.COMMANDE_EN_ATTENTE,
                $"Une nouvelle commande sur commande vous a été assignée : {vente.QuantiteVendue} × « {vente.Produit?.Nom} ».",
                "/commandes",
                RoleUtilisateur.RESPONSABLE_STOCK_PRODUCTION,
                dto.ResponsableId
            );

            return Ok(new { message = "Commande assignée avec succès.", responsableNom = $"{responsable.Prenom} {responsable.Nom}" });
        }

        // DELETE: api/historiquevente/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> SupprimerVente(int id)
        {
            var vente = await _context.HistoriqueVentes.FindAsync(id);
            if (vente == null)
                return NotFound();

            _context.HistoriqueVentes.Remove(vente);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class CommandeDto
    {
        public int ProduitId { get; set; }
        public int QuantiteVendue { get; set; }
        public decimal PrixUnitaire { get; set; }
        public DateTime? DateSouhaitee { get; set; }
    }

    public class AccepterCommandeDto
    {
        public DateTime? DateEstimeePreparation { get; set; }
    }

    public class ConfirmerCommandeDto
    {
        public DateTime? DateEstimeePreparation { get; set; }
    }
}
