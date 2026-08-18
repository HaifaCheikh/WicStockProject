using System.ComponentModel.DataAnnotations;
using static global::WicStock_.Models.Enums;

    namespace WicStock_.Models.Dtos
    {
    public class RegisterDto
    {
        [Required, MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Prenom { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string MotDePasse { get; set; } = string.Empty;

        [Phone]
        public string? Telephone { get; set; }

        [Required]
        public RoleUtilisateur Role { get; set; }
    }

    public class LoginDto
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            public string MotDePasse { get; set; } = string.Empty;
        // Jeton Cloudflare Turnstile envoyé par le client (optionnel si Turnstile non configuré)
        public string? CaptchaToken { get; set; }
        }

        public class AuthResponseDto
        {
            public string Token { get; set; } = string.Empty;
            public string Nom { get; set; } = string.Empty;
            public string Prenom { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? Telephone { get; set; }
            public string Role { get; set; } = string.Empty;
        }

        public class ForgotPasswordDto
        {
            [Required]
            public string Methode { get; set; } = "Email"; // "Email" ou "WhatsApp"

            [Required]
            public string Identifiant { get; set; } = string.Empty;
        }

        public class ResetPasswordDto
        {
            [Required]
            public string Identifiant { get; set; } = string.Empty;

            [Required]
            public string Code { get; set; } = string.Empty;

            [Required, MinLength(6)]
            public string NouveauMotDePasse { get; set; } = string.Empty;
        }
    }

