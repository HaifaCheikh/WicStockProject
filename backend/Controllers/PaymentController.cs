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
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly LemonSqueezyService _lemonSqueezyService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            AppDbContext context,
            NotificationService notificationService,
            LemonSqueezyService lemonSqueezyService,
            IConfiguration configuration,
            ILogger<PaymentController> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _lemonSqueezyService = lemonSqueezyService;
            _configuration = configuration;
            _logger = logger;
        }

        // GET: api/payment/config
        [HttpGet("config")]
        [AllowAnonymous]
        public ActionResult GetPaymentConfig()
        {
            return Ok(new
            {
                storeId = _lemonSqueezyService.GetStoreId(),
                variantId = _lemonSqueezyService.GetVariantId(),
                provider = "lemonsqueezy"
            });
        }

        // POST: api/payment/create-checkout
        [HttpPost("create-checkout")]
        public async Task<ActionResult<PaymentResponseDto>> CreateCheckout([FromBody] PaymentRequestDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var commande = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .Include(h => h.Utilisateur)
                .FirstOrDefaultAsync(h => h.Id == dto.CommandeId && h.UtilisateurId == userId);

            if (commande == null)
                return NotFound("Commande introuvable.");

            // Enregistrer l'adresse de livraison et code postal fournis
            if (!string.IsNullOrWhiteSpace(dto.AdresseLivraison))
                commande.AdresseLivraison = dto.AdresseLivraison.Trim();
            if (!string.IsNullOrWhiteSpace(dto.CodePostal))
                commande.CodePostal = dto.CodePostal.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Ville))
                commande.Ville = dto.Ville.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Pays))
                commande.Pays = dto.Pays.Trim();

            // Mettre à jour également le profil client si non renseigné
            if (commande.Utilisateur != null)
            {
                if (string.IsNullOrWhiteSpace(commande.Utilisateur.Adresse) && !string.IsNullOrWhiteSpace(dto.AdresseLivraison))
                    commande.Utilisateur.Adresse = dto.AdresseLivraison.Trim();
                if (string.IsNullOrWhiteSpace(commande.Utilisateur.CodePostal) && !string.IsNullOrWhiteSpace(dto.CodePostal))
                    commande.Utilisateur.CodePostal = dto.CodePostal.Trim();
                if (string.IsNullOrWhiteSpace(commande.Utilisateur.Ville) && !string.IsNullOrWhiteSpace(dto.Ville))
                    commande.Utilisateur.Ville = dto.Ville.Trim();
            }
            await _context.SaveChangesAsync();

            // Vérifier que la commande peut être payée :
            // Commande simple acceptée (StatutCommande = ACCEPTEE, Statut = null ou ACCEPTEE)
            // OU commande sur commande prête (Statut = PRETE)
            bool peutEtrePayee = commande.StatutCommande == "ACCEPTEE" && (commande.Statut == null || commande.Statut == StatutCommandeDetaille.ACCEPTEE)
                || commande.Statut == StatutCommandeDetaille.PRETE;

            if (!peutEtrePayee)
                return BadRequest("Cette commande ne peut pas être payée (elle doit être acceptée ou prête).");

            if (commande.Statut == StatutCommandeDetaille.PAYEE
                || commande.Statut == StatutCommandeDetaille.EN_LIVRAISON
                || commande.Statut == StatutCommandeDetaille.LIVREE)
                return BadRequest("Cette commande a déjà été payée.");

            var montant = commande.QuantiteVendue * commande.PrixUnitaire;

            try
            {
                var apiBaseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7179";
                var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "https://localhost:7121";
                // LemonSqueezy redirige vers l'API qui met à jour le statut, puis redirige vers Blazor
                var successUrl = $"{apiBaseUrl}/api/payment/success/{commande.Id}";
                var cancelUrl = $"{frontendUrl}/mes-commandes/suivi/{commande.Id}?payment=cancelled";

                var clientName = commande.Utilisateur != null ? $"{commande.Utilisateur.Prenom} {commande.Utilisateur.Nom}".Trim() : null;
                var clientEmail = commande.Utilisateur?.Email;
                var country = commande.Pays ?? "Tunisia";
                var zip = commande.CodePostal ?? commande.Utilisateur?.CodePostal;

                var checkout = await _lemonSqueezyService.CreateCheckoutAsync(
                    montant,
                    commande.QuantiteVendue,
                    commande.Produit?.Nom ?? "Produit",
                    commande.Id,
                    successUrl,
                    cancelUrl,
                    clientName,
                    clientEmail,
                    country,
                    zip
                );

                if (checkout == null)
                    return BadRequest("Erreur lors de la création du checkout Lemon Squeezy.");

                return Ok(new PaymentResponseDto
                {
                    CheckoutUrl = checkout.Attributes.Url,
                    CheckoutId = checkout.Id,
                    Montant = montant,
                    Currency = dto.Currency
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError("LemonSqueezy checkout error: {Message}", ex.Message);
                return BadRequest($"Erreur LemonSqueezy : {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating checkout");
                return BadRequest($"Erreur lors de la création du checkout: {ex.Message}");
            }
        }

        // GET: api/payment/success/{commandeId}
        // Appelé par LemonSqueezy après paiement réussi (redirect_url)
        [HttpGet("success/{commandeId}")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentSuccess(
            int commandeId,
            [FromQuery(Name = "order_id")] string? orderId = null,
            [FromQuery(Name = "country")] string? country = null,
            [FromQuery(Name = "zip")] string? zip = null,
            [FromQuery(Name = "city")] string? city = null,
            [FromQuery(Name = "address")] string? address = null)
        {
            var commande = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .Include(h => h.Utilisateur)
                .FirstOrDefaultAsync(h => h.Id == commandeId);

            if (commande == null)
                return NotFound("Commande introuvable.");

            // Si LemonSqueezy a transmis un order_id, tenter de récupérer l'adresse de facturation/livraison
            if (!string.IsNullOrEmpty(orderId))
            {
                try
                {
                    var order = await _lemonSqueezyService.GetOrderAsync(orderId);
                    if (order?.Attributes != null)
                    {
                        if (string.IsNullOrWhiteSpace(country)) country = order.Attributes.CountryFormatted ?? order.Attributes.Country;
                        if (string.IsNullOrWhiteSpace(zip)) zip = order.Attributes.Zip;
                        if (string.IsNullOrWhiteSpace(city)) city = order.Attributes.City;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Impossible de récupérer les détails de l'ordre LemonSqueezy {OrderId}", orderId);
                }
            }

            // Mettre à jour les informations d'adresse si disponibles
            if (!string.IsNullOrWhiteSpace(address))
                commande.AdresseLivraison = address.Trim();
            else if (string.IsNullOrWhiteSpace(commande.AdresseLivraison) && !string.IsNullOrWhiteSpace(country))
                commande.AdresseLivraison = country.Trim();

            if (!string.IsNullOrWhiteSpace(zip))
                commande.CodePostal = zip.Trim();
            if (!string.IsNullOrWhiteSpace(city))
                commande.Ville = city.Trim();
            if (!string.IsNullOrWhiteSpace(country))
                commande.Pays = country.Trim();

            // Mettre à jour le profil client également si non renseigné
            if (commande.Utilisateur != null)
            {
                if (string.IsNullOrWhiteSpace(commande.Utilisateur.Adresse) && !string.IsNullOrWhiteSpace(commande.AdresseLivraison))
                    commande.Utilisateur.Adresse = commande.AdresseLivraison;
                if (string.IsNullOrWhiteSpace(commande.Utilisateur.CodePostal) && !string.IsNullOrWhiteSpace(commande.CodePostal))
                    commande.Utilisateur.CodePostal = commande.CodePostal;
                if (string.IsNullOrWhiteSpace(commande.Utilisateur.Ville) && !string.IsNullOrWhiteSpace(commande.Ville))
                    commande.Utilisateur.Ville = commande.Ville;
            }

            // Ne pas re-traiter si déjà payée
            if (commande.Statut != StatutCommandeDetaille.PAYEE
                && commande.Statut != StatutCommandeDetaille.EN_LIVRAISON
                && commande.Statut != StatutCommandeDetaille.LIVREE)
            {
                commande.Statut = StatutCommandeDetaille.PAYEE;
                commande.DatePaiement = DateTime.Now;
                await _context.SaveChangesAsync();

                // Notifier le client
                if (commande.UtilisateurId.HasValue)
                {
                    await _notificationService.NotifierNouvelEvenementAsync(
                        TypeNotification.PAIEMENT_RECU,
                        $"Votre paiement a été reçu avec succès. Préparation de la livraison en cours.",
                        $"/mes-commandes/suivi/{commande.Id}",
                        RoleUtilisateur.CLIENT,
                        commande.UtilisateurId.Value
                    );
                }

                _logger.LogInformation("Paiement confirmé pour commande #{CommandeId}", commandeId);
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            // Rediriger directement vers la page de suivi de commande
            var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "https://localhost:7121";
            return Redirect($"{frontendUrl}/mes-commandes/suivi/{commandeId}?payment=success");
        }

        // PUT: api/payment/adresse/{commandeId}
        [HttpPut("adresse/{commandeId}")]
        public async Task<IActionResult> MettreAJourAdresse(int commandeId, [FromBody] MettreAJourAdresseDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var commande = await _context.HistoriqueVentes
                .Include(h => h.Utilisateur)
                .FirstOrDefaultAsync(h => h.Id == commandeId && (h.UtilisateurId == userId || User.IsInRole("ADMIN")));

            if (commande == null)
                return NotFound("Commande introuvable.");

            if (!string.IsNullOrWhiteSpace(dto.AdresseLivraison))
                commande.AdresseLivraison = dto.AdresseLivraison.Trim();
            if (!string.IsNullOrWhiteSpace(dto.CodePostal))
                commande.CodePostal = dto.CodePostal.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Ville))
                commande.Ville = dto.Ville.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Pays))
                commande.Pays = dto.Pays.Trim();

            if (commande.Utilisateur != null)
            {
                if (!string.IsNullOrWhiteSpace(dto.AdresseLivraison))
                    commande.Utilisateur.Adresse = dto.AdresseLivraison.Trim();
                if (!string.IsNullOrWhiteSpace(dto.CodePostal))
                    commande.Utilisateur.CodePostal = dto.CodePostal.Trim();
                if (!string.IsNullOrWhiteSpace(dto.Ville))
                    commande.Utilisateur.Ville = dto.Ville.Trim();
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Adresse mise à jour avec succès." });
        }

        // GET: api/payment/cancel/{commandeId}
        [HttpGet("cancel/{commandeId}")]
        [AllowAnonymous]
        public IActionResult PaymentCancel(int commandeId)
        {
            var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "https://localhost:7121";
            return Redirect($"{frontendUrl}/mes-commandes/suivi/{commandeId}?payment=cancelled");
        }

        // POST: api/payment/confirm-payment
        // Endpoint optionnel pour confirmation manuelle (si webhook non configuré)
        [HttpPost("confirm-payment")]
        public async Task<ActionResult<PaymentSuccessDto>> ConfirmPayment([FromBody] PaymentSuccessDto dto)
        {
            var commande = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .FirstOrDefaultAsync(h => h.PaymentIntentId == dto.PaymentIntentId);

            if (commande == null)
                return NotFound("Commande introuvable.");

            if (!dto.Success)
                return BadRequest("Le paiement a échoué.");

            if (commande.Statut == StatutCommandeDetaille.PAYEE
                || commande.Statut == StatutCommandeDetaille.EN_LIVRAISON
                || commande.Statut == StatutCommandeDetaille.LIVREE)
                return BadRequest("Cette commande a déjà été payée.");

            commande.Statut = StatutCommandeDetaille.PAYEE;
            commande.DatePaiement = DateTime.Now;
            commande.PaymentIntentId = dto.PaymentIntentId;

            await _context.SaveChangesAsync();

            if (commande.UtilisateurId.HasValue)
            {
                await _notificationService.NotifierNouvelEvenementAsync(
                    TypeNotification.PAIEMENT_RECU,
                    $"Votre paiement a été reçu avec succès. Préparation de la livraison de votre commande.",
                    $"/mes-commandes/suivi/{commande.Id}",
                    RoleUtilisateur.CLIENT,
                    commande.UtilisateurId.Value
                );
            }

            return Ok(new PaymentSuccessDto
            {
                PaymentIntentId = dto.PaymentIntentId,
                Success = true,
                Message = "Paiement confirmé avec succès."
            });
        }

        // POST: api/payment/marquer-livree/{commandeId}
        [HttpPost("marquer-livree/{commandeId}")]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<IActionResult> MarquerLivree(int commandeId)
        {
            var commande = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .FirstOrDefaultAsync(h => h.Id == commandeId);

            if (commande == null)
                return NotFound("Commande introuvable.");

            if (commande.Statut != StatutCommandeDetaille.PAYEE)
                return BadRequest("La commande doit être payée avant d'être marquée comme en livraison.");

            commande.Statut = StatutCommandeDetaille.EN_LIVRAISON;
            commande.DateLivraison = DateTime.Now;

            await _context.SaveChangesAsync();

            if (commande.UtilisateurId.HasValue)
            {
                await _notificationService.NotifierNouvelEvenementAsync(
                    TypeNotification.COMMANDE_LIVREE,
                    $"Votre commande #{commande.Id} est en cours de livraison.",
                    $"/mes-commandes/suivi/{commande.Id}",
                    RoleUtilisateur.CLIENT,
                    commande.UtilisateurId.Value
                );
            }

            return Ok(new { message = "Commande marquée comme en livraison." });
        }

        // POST: api/payment/confirmer-livraison/{commandeId}
        [HttpPost("confirmer-livraison/{commandeId}")]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<IActionResult> ConfirmerLivraison(int commandeId)
        {
            var commande = await _context.HistoriqueVentes
                .Include(h => h.Produit)
                .FirstOrDefaultAsync(h => h.Id == commandeId);

            if (commande == null)
                return NotFound("Commande introuvable.");

            if (commande.Statut != StatutCommandeDetaille.EN_LIVRAISON)
                return BadRequest("La commande doit être en livraison pour être confirmée comme livrée.");

            commande.Statut = StatutCommandeDetaille.LIVREE;

            await _context.SaveChangesAsync();

            if (commande.UtilisateurId.HasValue)
            {
                await _notificationService.NotifierNouvelEvenementAsync(
                    TypeNotification.COMMANDE_LIVREE,
                    $"Votre commande #{commande.Id} a été livrée avec succès. Merci pour votre confiance !",
                    $"/mes-commandes/suivi/{commande.Id}",
                    RoleUtilisateur.CLIENT,
                    commande.UtilisateurId.Value
                );
            }

            return Ok(new { message = "Commande livrée avec succès." });
        }

        // GET: api/payment/statut/{commandeId}
        // Permet au client de vérifier le statut de paiement de sa commande
        [HttpGet("statut/{commandeId}")]
        public async Task<IActionResult> GetStatutPaiement(int commandeId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var commande = await _context.HistoriqueVentes
                .FirstOrDefaultAsync(h => h.Id == commandeId && h.UtilisateurId == userId);

            if (commande == null)
                return NotFound("Commande introuvable.");

            return Ok(new
            {
                commandeId = commande.Id,
                statut = commande.Statut?.ToString(),
                statutCommande = commande.StatutCommande,
                datePaiement = commande.DatePaiement,
                estPayee = commande.Statut == StatutCommandeDetaille.PAYEE
                    || commande.Statut == StatutCommandeDetaille.EN_LIVRAISON
                    || commande.Statut == StatutCommandeDetaille.LIVREE
            });
        }
    }
}