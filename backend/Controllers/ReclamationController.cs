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
    public class ReclamationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly IWebHostEnvironment _env;

        // Extensions de fichiers autorisées pour les photos justificatives
        private static readonly string[] ExtensionsAutorisees = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".bmp" };

        public ReclamationController(AppDbContext context, NotificationService notificationService, IWebHostEnvironment env)
        {
            _context = context;
            _notificationService = notificationService;
            _env = env;
        }

        // GET: api/reclamations/mes-reclamations (Réclamations du client connecté)
        [HttpGet("mes-reclamations")]
        [Authorize(Roles = "CLIENT,ADMIN")]
        public async Task<ActionResult<IEnumerable<ReclamationDto>>> GetMesReclamations()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var reclamations = await _context.Reclamations
                .Include(r => r.Produit)
                .Include(r => r.Client)
                .Where(r => r.ClientId == userId)
                .OrderByDescending(r => r.DateCreation)
                .ToListAsync();

            return Ok(reclamations.Select(MapToDto));
        }

        // GET: api/reclamations/commande/{commandeId}
        [HttpGet("commande/{commandeId}")]
        public async Task<ActionResult<IEnumerable<ReclamationDto>>> GetReclamationsParCommande(int commandeId)
        {
            var reclamations = await _context.Reclamations
                .Include(r => r.Produit)
                .Include(r => r.Client)
                .Where(r => r.CommandeId == commandeId)
                .OrderByDescending(r => r.DateCreation)
                .ToListAsync();

            return Ok(reclamations.Select(MapToDto));
        }

        // POST: api/reclamations (Créer une réclamation)
        [HttpPost]
        [Authorize(Roles = "CLIENT,ADMIN")]
        public async Task<ActionResult<ReclamationDto>> CreerReclamation(CreerReclamationDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Motif))
                return BadRequest("Le motif de la réclamation est obligatoire.");

            if (string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest("La description détaillée de la réclamation est obligatoire.");

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
                return BadRequest("Vous ne pouvez signaler un problème que sur une commande livrée.");

            // Règle 2: Délai de 14 jours max après livraison
            var dateReference = commande.DateLivraison ?? commande.DateVente;
            if ((DateTime.Now - dateReference).TotalDays > 14)
                return BadRequest("Le délai de 14 jours après la livraison pour effectuer une réclamation est dépassé.");

            var reclamation = new Reclamation
            {
                CommandeId = commande.Id,
                ProduitId = commande.ProduitId,
                ClientId = userId,
                Motif = dto.Motif.Trim(),
                Description = dto.Description.Trim(),
                PhotosUrls = dto.PhotosUrls,
                DateCreation = DateTime.Now,
                Statut = StatutReclamation.ENVOYEE
            };

            _context.Reclamations.Add(reclamation);
            await _context.SaveChangesAsync();

            await _context.Entry(reclamation).Reference(r => r.Produit).LoadAsync();
            await _context.Entry(reclamation).Reference(r => r.Client).LoadAsync();

            // Notifier l'admin
            await _notificationService.NotifierNouvelEvenementAsync(
                TypeNotification.COMMANDE_EN_ATTENTE,
                $"Nouvelle réclamation ({dto.Motif}).",
                "/admin/reclamations",
                RoleUtilisateur.ADMIN
            );

            return CreatedAtAction(nameof(GetReclamationsParCommande), new { commandeId = reclamation.CommandeId }, MapToDto(reclamation));
        }

        // POST: api/reclamations/upload-photo
        [HttpPost("upload-photo")]
        [Authorize(Roles = "CLIENT,ADMIN")]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Fichier non fourni ou vide.");

            // Validation du type de fichier (sécurité + évite d'accepter n'importe quoi)
            var fileExt = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExt) || !ExtensionsAutorisees.Contains(fileExt))
                return BadRequest("Format de fichier non autorisé. Formats acceptés : jpg, jpeg, png, gif, webp.");

            // Limite de taille (ex: 10 Mo), cohérent avec le maxAllowedSize côté client
            const long tailleMaxOctets = 10 * 1024 * 1024;
            if (file.Length > tailleMaxOctets)
                return BadRequest("Le fichier dépasse la taille maximale autorisée (10 Mo).");

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "reclamations");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{fileExt}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // IMPORTANT : on construit une URL ABSOLUE (schéma + host + chemin).
            // Une URL relative ("/uploads/reclamations/xxx.jpg") est résolue par le
            // navigateur par rapport à l'origine de la page Blazor, pas de l'API.
            // Si Blazor et l'API sont sur des origines différentes (ports/domaines
            // différents), l'image ne se charge jamais -> icône "image cassée".
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var photoUrl = $"{baseUrl}/uploads/reclamations/{uniqueFileName}";

            return Ok(new { url = photoUrl });
        }

        // GET: api/reclamations/admin (Liste Back-office avec filtrage)
        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<IEnumerable<ReclamationDto>>> GetToutesLesReclamationsAdmin([FromQuery] string? statut = null)
        {
            var query = _context.Reclamations
                .Include(r => r.Produit)
                .Include(r => r.Client)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statut) && Enum.TryParse<StatutReclamation>(statut, true, out var statutParsed))
            {
                query = query.Where(r => r.Statut == statutParsed);
            }

            var reclamations = await query.OrderByDescending(r => r.DateCreation).ToListAsync();

            return Ok(reclamations.Select(MapToDto));
        }

        // PUT: api/reclamations/admin/{id}/traiter (Changer le statut + réponse admin)
        [HttpPut("admin/{id}/traiter")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> TraiterReclamation(int id, TraiterReclamationDto dto)
        {
            var reclamation = await _context.Reclamations
                .Include(r => r.Commande)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reclamation == null)
                return NotFound("Réclamation introuvable.");

            reclamation.Statut = dto.Statut;
            reclamation.ReponseAdmin = dto.ReponseAdmin;
            reclamation.DateReponse = DateTime.Now;

            await _context.SaveChangesAsync();

            // Notifier le client de la réponse
            string messageStatut = dto.Statut switch
            {
                StatutReclamation.EN_COURS => "est en cours de traitement.",
                StatutReclamation.RESOLUE => "a été résolue.",
                StatutReclamation.REJETEE => "a été traitée.",
                _ => "a été mise à jour."
            };

            await _notificationService.NotifierNouvelEvenementAsync(
                TypeNotification.COMMANDE_CONFIRMEE,
                $"Votre réclamation {messageStatut}",
                "/mes-avis-reclamations",
                RoleUtilisateur.CLIENT,
                reclamation.ClientId
            );

            return Ok(new { message = "Réclamation mise à jour.", statut = reclamation.Statut.ToString() });
        }

        // DELETE: api/reclamations/admin/{id} (Supprimer une réclamation)
        [HttpDelete("admin/{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> SupprimerReclamation(int id)
        {
            var reclamation = await _context.Reclamations.FirstOrDefaultAsync(r => r.Id == id);

            if (reclamation == null)
                return NotFound("Réclamation introuvable.");

            // Supprime aussi les fichiers photos justificatifs stockés sur le disque,
            // pour éviter d'accumuler des fichiers orphelins dans wwwroot/uploads/reclamations.
            if (!string.IsNullOrEmpty(reclamation.PhotosUrls))
            {
                var photosFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "reclamations");
                var photos = reclamation.PhotosUrls.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var photoUrl in photos)
                {
                    var fileName = Path.GetFileName(photoUrl.Trim());
                    if (string.IsNullOrEmpty(fileName)) continue;

                    var filePath = Path.Combine(photosFolder, fileName);
                    if (System.IO.File.Exists(filePath))
                    {
                        try
                        {
                            System.IO.File.Delete(filePath);
                        }
                        catch
                        {
                            // On n'échoue pas la suppression de la réclamation si un fichier
                            // ne peut pas être supprimé (verrouillé, déjà absent, etc.).
                        }
                    }
                }
            }

            _context.Reclamations.Remove(reclamation);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Réclamation supprimée." });
        }

        private static ReclamationDto MapToDto(Reclamation r) => new()
        {
            Id = r.Id,
            CommandeId = r.CommandeId,
            ProduitId = r.ProduitId,
            ProduitNom = r.Produit?.Nom,
            ProduitReference = r.Produit?.Reference,
            ProduitImageUrl = r.Produit?.ImageUrl,
            ClientId = r.ClientId,
            ClientNom = r.Client != null ? $"{r.Client.Prenom} {r.Client.Nom}" : "Client",
            ClientEmail = r.Client?.Email,
            Motif = r.Motif,
            Description = r.Description,
            PhotosUrls = r.PhotosUrls,
            DateCreation = r.DateCreation,
            Statut = r.Statut.ToString(),
            ReponseAdmin = r.ReponseAdmin,
            DateReponse = r.DateReponse
        };
    }
}