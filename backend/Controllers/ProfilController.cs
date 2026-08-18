using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WicStock_.Models;
using WicStock_.Models.Dtos;

namespace WicStock_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfilController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProfilController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/profil/me
        [HttpGet("me")]
        public async Task<ActionResult<ProfilDto>> GetMonProfil()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var utilisateur = await _context.Utilisateurs.FindAsync(userId);
            if (utilisateur == null)
                return NotFound();

            return Ok(new ProfilDto
            {
                Id = utilisateur.Id,
                Nom = utilisateur.Nom,
                Prenom = utilisateur.Prenom,
                Email = utilisateur.Email,
                Telephone = utilisateur.Telephone,
                PhotoUrl = utilisateur.PhotoUrl,
                Role = utilisateur.Role.ToString()
            });
        }

        // PUT: api/profil/me
        [HttpPut("me")]
        public async Task<IActionResult> ModifierMonProfil(ModifierProfilDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var utilisateur = await _context.Utilisateurs.FindAsync(userId);
            if (utilisateur == null)
                return NotFound();

            var newEmail = dto.Email.Trim();
            if (!string.Equals(utilisateur.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            {
                var emailExiste = await _context.Utilisateurs
                    .AnyAsync(u => u.Id != userId && u.Email.ToLower() == newEmail.ToLower());
                if (emailExiste)
                {
                    return BadRequest("Cet adresse email est déjà utilisée par un autre compte.");
                }
                utilisateur.Email = newEmail;
            }

            utilisateur.Nom = dto.Nom.Trim();
            utilisateur.Prenom = dto.Prenom.Trim();
            utilisateur.Telephone = dto.Telephone?.Trim();

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Profil mis à jour avec succès." });
        }

        // POST: api/profil/me/photo
        [HttpPost("me/photo")]
        public async Task<ActionResult> TeleverserPhoto(IFormFile file)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var utilisateur = await _context.Utilisateurs.FindAsync(userId);
            if (utilisateur == null)
                return NotFound();

            if (file == null || file.Length == 0)
                return BadRequest("Aucun fichier n'a été envoyé.");

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("La taille du fichier ne doit pas dépasser 5 Mo.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest("Seuls les fichiers JPG, JPEG, PNG et WEBP sont autorisés.");

            // Generate unique filename
            var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "photos");
            
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update user photo URL
            utilisateur.PhotoUrl = $"/uploads/photos/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new PhotoResponseDto { PhotoUrl = utilisateur.PhotoUrl });
        }

        // PUT: api/profil/changer-mot-de-passe
        [HttpPut("changer-mot-de-passe")]
        public async Task<IActionResult> ChangerMotDePasse(ChangerMotDePasseDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var utilisateur = await _context.Utilisateurs.FindAsync(userId);
            if (utilisateur == null)
                return NotFound();

            // Verify old password
            bool oldPasswordValid = BCrypt.Net.BCrypt.Verify(dto.AncienMotDePasse, utilisateur.MotDePasseHash);
            if (!oldPasswordValid)
                return BadRequest("L'ancien mot de passe est incorrect.");

            // Validate new password
            if (dto.NouveauMotDePasse != dto.ConfirmationMotDePasse)
                return BadRequest("La confirmation du mot de passe ne correspond pas.");

            if (string.IsNullOrWhiteSpace(dto.NouveauMotDePasse) || dto.NouveauMotDePasse.Length < 6)
                return BadRequest("Le nouveau mot de passe doit contenir au moins 6 caractères.");

            // Update password
            utilisateur.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(dto.NouveauMotDePasse);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Mot de passe modifié avec succès." });
        }
    }
}