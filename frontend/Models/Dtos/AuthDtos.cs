namespace WicStock.Web.Models.Dtos
{
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string MotDePasse { get; set; } = string.Empty;
        public string? CaptchaToken { get; set; }
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class RegisterDto
    {
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;

        public string MotDePasse { get; set; } = string.Empty;
        public string ConfirmationMotDePasse { get; set; } = string.Empty;
        public string Role { get; set; } = "CLIENT";
    }

    public class ForgotPasswordDto
    {
        public string Methode { get; set; } = "Email"; // "Email" ou "WhatsApp"
        public string Identifiant { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        public string Identifiant { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string NouveauMotDePasse { get; set; } = string.Empty;
    }

    public class ForgotPasswordResponse
    {
        public string Message { get; set; } = string.Empty;
        public string? WhatsAppLink { get; set; }
    }
}