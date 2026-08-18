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

            if (dto.Methode.Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                utilisateur = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == dto.Identifiant);
            }
            else if (dto.Methode.Equals("WhatsApp", StringComparison.OrdinalIgnoreCase))
            {
                utilisateur = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Telephone == dto.Identifiant);
            }

            if (utilisateur == null)
                return NotFound("Utilisateur introuvable avec ces informations.");

            // Génère le code OTP — jamais renvoyé dans la réponse HTTP
            var code = _resetService.GenerateCode(dto.Identifiant);

            if (dto.Methode.Equals("WhatsApp", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Vérifier que le numéro existe
                    if (string.IsNullOrWhiteSpace(utilisateur.Telephone))
                    {
                        Console.WriteLine("[WHATSAPP] Numéro utilisateur manquant.");
                        return StatusCode(500, "Numéro de téléphone introuvable pour l'utilisateur.");
                    }

                    // Normaliser le numéro : supprimer '+' et espaces (ne garder que les chiffres)
                    var originalNumero = utilisateur.Telephone;
                    var numeroNormalized = new string(originalNumero.Where(char.IsDigit).ToArray());

                    Console.WriteLine($"[WHATSAPP] Numéro original: '{originalNumero}', normalisé: '{numeroNormalized}'");

                    await _whatsAppService.EnvoyerCodeReinitialisationAsync(numeroNormalized, utilisateur.Prenom, code);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WHATSAPP ERROR] {ex.Message}");
                    return StatusCode(500, "Erreur lors de l'envoi du message WhatsApp. Vérifiez que le service whatsapp-service est démarré et connecté.");
                }

                return Ok(new
                {
                    Message = $"Un code de réinitialisation a été envoyé par WhatsApp au {dto.Identifiant}."
                });
            }
            else
            {
                // Envoi du vrai e-mail via SMTP Gmail — le code n'est pas dans la réponse
                try
                {
                    await _emailService.EnvoyerCodeReinitialisationAsync(utilisateur.Email, utilisateur.Prenom, code);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
                    return StatusCode(500, "Erreur lors de l'envoi de l'e-mail. Vérifiez la configuration SMTP dans appsettings.json.");
                }

                return Ok(new
                {
                    Message = $"Un code de réinitialisation a été envoyé à l'adresse {dto.Identifiant}. Vérifiez votre boîte mail (et les spams)."
                });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            bool valide = _resetService.VerifyCode(dto.Identifiant, dto.Code);
            if (!valide)
                return BadRequest("Code de réinitialisation incorrect ou expiré.");

            Utilisateur? utilisateur = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.Email == dto.Identifiant || u.Telephone == dto.Identifiant);

            if (utilisateur == null)
                return NotFound("Utilisateur introuvable.");

            utilisateur.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(dto.NouveauMotDePasse);
            await _context.SaveChangesAsync();

            _resetService.RemoveCode(dto.Identifiant);

            return Ok(new { Message = "Mot de passe réinitialisé avec succès." });
        }
    }
}