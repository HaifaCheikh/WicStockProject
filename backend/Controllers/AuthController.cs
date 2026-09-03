using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using WicStock_.Models;
using WicStock_.Models.Dtos;
using WicStock_.Services;

namespace WicStock_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;
        private readonly PasswordResetService _resetService;
        private readonly EmailService _emailService;
        private readonly WhatsAppService _whatsAppService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AuthController(
            AppDbContext context,
            JwtService jwtService,
            PasswordResetService resetService,
            EmailService emailService,
            WhatsAppService whatsAppService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _resetService = resetService;
            _emailService = emailService;
            _whatsAppService = whatsAppService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        private class TurnstileVerifyResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("error-codes")]
            public List<string>? ErrorCodes { get; set; }
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        {
            bool emailExiste = await _context.Utilisateurs
                .AnyAsync(u => u.Email == dto.Email);

            if (emailExiste)
                return BadRequest("Un utilisateur avec cet email existe déjà.");

            var utilisateur = new Utilisateur
            {
                Nom = dto.Nom,
                Prenom = dto.Prenom,
                Email = dto.Email,
                Telephone = dto.Telephone,
                MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(dto.MotDePasse),
                Role = dto.Role
            };

            _context.Utilisateurs.Add(utilisateur);
            await _context.SaveChangesAsync();

            var token = _jwtService.GenererToken(utilisateur);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Nom = utilisateur.Nom,
                Email = utilisateur.Email,
                Telephone = utilisateur.Telephone,
                Role = utilisateur.Role.ToString()
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            // Vérification Turnstile côté serveur (temporairement désactivée pour débogage)
            /*
            if (string.IsNullOrEmpty(dto.CaptchaToken))
                return BadRequest("Vérification de sécurité manquante.");

            var secret = _configuration["Turnstile:SecretKey"];
            if (string.IsNullOrEmpty(secret))
            {
                Console.WriteLine("[TURNSTILE] Secret key non configurée.");
                return StatusCode(500, "Erreur de configuration serveur (Turnstile).");
            }

            var client = _httpClientFactory.CreateClient();
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "secret", secret },
                { "response", dto.CaptchaToken },
                { "remoteip", HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty }
            });

            var verifyResponse = await client.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", form);
            var verifyResult = await verifyResponse.Content.ReadFromJsonAsync<TurnstileVerifyResponse>();

            if (verifyResult is null || !verifyResult.Success)
            {
                Console.WriteLine($"[TURNSTILE] Vérification échouée: {verifyResult?.ErrorCodes?.Count ?? 0} erreurs");
                return BadRequest("Vérification de sécurité invalide, réessayez.");
            }
            */

            try
            {
                Console.WriteLine($"[LOGIN] Tentative de connexion pour: {dto.Email}");

                var utilisateur = await _context.Utilisateurs
                    .FirstOrDefaultAsync(u => u.Email == dto.Email);

                Console.WriteLine($"[LOGIN] Utilisateur trouvé: {utilisateur != null}");

                if (utilisateur == null)
                    return Unauthorized("Email ou mot de passe incorrect.");

                Console.WriteLine($"[LOGIN] Vérification du mot de passe");
                bool motDePasseValide = BCrypt.Net.BCrypt.Verify(
                    dto.MotDePasse, utilisateur.MotDePasseHash);

                Console.WriteLine($"[LOGIN] Mot de passe valide: {motDePasseValide}");

                if (!motDePasseValide)
                    return Unauthorized("Email ou mot de passe incorrect.");

                Console.WriteLine($"[LOGIN] Génération du token JWT");
                var token = _jwtService.GenererToken(utilisateur);

                Console.WriteLine($"[LOGIN] Token généré avec succès");

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    Nom = utilisateur.Nom,
                    Email = utilisateur.Email,
                    Telephone = utilisateur.Telephone,
                    Role = utilisateur.Role.ToString()
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGIN] Erreur: {ex.Message}");
                Console.WriteLine($"[LOGIN] Stack trace: {ex.StackTrace}");
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            Utilisateur? utilisateur = null;
            var cleanIdent = (dto.Identifiant ?? "").Trim().ToLower();

            if (dto.Methode.Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                utilisateur = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email.ToLower() == cleanIdent);
            }
            else if (dto.Methode.Equals("WhatsApp", StringComparison.OrdinalIgnoreCase))
            {
                var digitsOnly = new string(cleanIdent.Where(char.IsDigit).ToArray());
                utilisateur = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Telephone != null && 
                    (u.Telephone.ToLower() == cleanIdent || (digitsOnly.Length > 0 && u.Telephone.Replace(" ", "").Replace("+", "").EndsWith(digitsOnly))));
            }

            if (utilisateur == null)
                return NotFound("Utilisateur introuvable avec ces informations.");

            // Génère le code OTP (indexé de façon sécurisée en mémoire backend uniquement)
            var code = _resetService.GenerateCode(dto.Identifiant);
            if (!string.IsNullOrEmpty(utilisateur.Email) && !utilisateur.Email.Equals(dto.Identifiant, StringComparison.OrdinalIgnoreCase))
                _resetService.GenerateCode(utilisateur.Email);
            if (!string.IsNullOrEmpty(utilisateur.Telephone) && !utilisateur.Telephone.Equals(dto.Identifiant, StringComparison.OrdinalIgnoreCase))
                _resetService.GenerateCode(utilisateur.Telephone);

            if (dto.Methode.Equals("WhatsApp", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(utilisateur.Telephone))
                    {
                        return BadRequest("Aucun numéro de téléphone enregistré pour cet utilisateur.");
                    }

                    var originalNumero = utilisateur.Telephone;
                    var numeroNormalized = new string(originalNumero.Where(char.IsDigit).ToArray());
                    await _whatsAppService.EnvoyerCodeReinitialisationAsync(numeroNormalized, utilisateur.Prenom, code);

                    return Ok(new
                    {
                        Message = "Message WhatsApp envoyé avec succès !"
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WHATSAPP ERROR] {ex.Message}");
                    return StatusCode(500, "Impossible d'envoyer le message WhatsApp. Vérifiez que le service WhatsApp est connecté.");
                }
            }
            else
            {
                try
                {
                    await _emailService.EnvoyerCodeReinitialisationAsync(utilisateur.Email, utilisateur.Prenom, code);

                    return Ok(new
                    {
                        Message = "E-mail envoyé avec succès !"
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EMAIL ERROR] {ex.GetType().Name}: {ex.Message}");
                    return StatusCode(500, $"Impossible d'envoyer l'e-mail : {ex.Message}");
                }
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            bool valide = _resetService.VerifyCode(dto.Identifiant, dto.Code);
            if (!valide)
                return BadRequest("Code de réinitialisation incorrect ou expiré.");

            var cleanIdent = (dto.Identifiant ?? "").Trim().ToLower();
            var digitsOnly = new string(cleanIdent.Where(char.IsDigit).ToArray());

            Utilisateur? utilisateur = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.Email.ToLower() == cleanIdent || 
                                          (digitsOnly.Length > 0 && u.Telephone != null && u.Telephone.Replace(" ", "").Replace("+", "").EndsWith(digitsOnly)));

            if (utilisateur == null)
                return NotFound("Utilisateur introuvable.");

            utilisateur.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(dto.NouveauMotDePasse);
            await _context.SaveChangesAsync();

            _resetService.RemoveCode(dto.Identifiant);

            return Ok(new { Message = "Mot de passe réinitialisé avec succès." });
        }
    }
}