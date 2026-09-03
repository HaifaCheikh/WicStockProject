using System.Net.Http;
using System.Text;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace WicStock_.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public EmailService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        public async Task EnvoyerCodeReinitialisationAsync(string destinataire, string prenom, string code)
        {
            var resendKey = _config["Email:ResendApiKey"] ?? _config["Email__ResendApiKey"] ?? _config["RESEND_API_KEY"];
            var brevoKey = _config["Email:BrevoApiKey"] ?? _config["Email__BrevoApiKey"] ?? _config["BREVO_API_KEY"];

            var rawHost = _config["Email:Host"] ?? _config["Email__Host"] ?? _config["EMAIL_HOST"];
            var host = string.IsNullOrWhiteSpace(rawHost) 
                ? "smtp.gmail.com" 
                : rawHost.Replace("http://", "").Replace("https://", "").Trim().Trim('"', '\'');
            if (string.IsNullOrWhiteSpace(host)) host = "smtp.gmail.com";

            var portStr = _config["Email:Port"] ?? _config["Email__Port"] ?? _config["EMAIL_PORT"];
            var port = int.TryParse(portStr?.Trim(), out var p) ? p : 587;

            var expediteur = (_config["Email:Expediteur"] ?? _config["Email__Expediteur"] ?? _config["EMAIL_EXPEDITEUR"])?.Trim().Trim('"', '\'');
            var rawPassword = _config["Email:MotDePasse"] ?? _config["Email__MotDePasse"] ?? _config["EMAIL_MOTDEPASSE"];
            var motDePasse = rawPassword?.Replace(" ", "").Trim().Trim('"', '\'');
            var nomAffiche = _config["Email:NomAffiché"] ?? _config["Email__NomAffiché"] ?? "WicStock";
            destinataire = destinataire?.Trim().Trim('"', '\'');

            var htmlContent = $"""
                <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 480px; margin: 0 auto; background: #f8fafc; padding: 32px 16px;">
                    <div style="background: white; border-radius: 16px; padding: 40px 32px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05);">
                        <div style="text-align: center; margin-bottom: 32px;">
                            <h1 style="color: #4f46e5; font-size: 1.8rem; font-weight: 800; margin: 0;">WicStock</h1>
                        </div>
                        <h2 style="color: #1e1b4b; font-size: 1.25rem; font-weight: 700; margin-top: 0;">Bonjour {prenom},</h2>
                        <p style="color: #475569; line-height: 1.6;">
                            Vous avez demandé à réinitialiser votre mot de passe WicStock.
                            Voici votre code de réinitialisation :
                        </p>
                        <div style="background: #f1f5f9; border-radius: 12px; padding: 24px; text-align: center; margin: 24px 0; border: 2px dashed #e2e8f0;">
                            <span style="font-size: 2.5rem; font-weight: 800; letter-spacing: 0.2em; color: #4f46e5; font-family: monospace;">{code}</span>
                        </div>
                        <p style="color: #64748b; font-size: 0.875rem; line-height: 1.6;">
                            ⏱️ Ce code expire dans <strong>15 minutes</strong>.<br>
                            Si vous n'avez pas demandé cette réinitialisation, ignorez cet e-mail.
                        </p>
                        <hr style="border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;">
                        <p style="color: #94a3b8; font-size: 0.75rem; text-align: center; margin: 0;">
                            © 2026 WicStock — Ne répondez pas à cet e-mail.
                        </p>
                    </div>
                </div>
                """;

            // Option A: API HTTPS Resend (port 443, non bloqué sur Render)
            if (!string.IsNullOrWhiteSpace(resendKey))
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", resendKey);
                var payload = new
                {
                    from = $"{nomAffiche} <{expediteur ?? "onboarding@resend.dev"}>",
                    to = new[] { destinataire },
                    subject = "Réinitialisation de votre mot de passe WicStock",
                    html = htmlContent
                };
                var json = JsonSerializer.Serialize(payload);
                var response = await client.PostAsync("https://api.resend.com/emails", new StringContent(json, Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode) return;
                var err = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[RESEND EMAIL ERROR] {err}");
            }

            // Option B: API HTTPS Brevo (port 443)
            if (!string.IsNullOrWhiteSpace(brevoKey))
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("api-key", brevoKey);
                var payload = new
                {
                    sender = new { name = nomAffiche, email = expediteur ?? "no-reply@wicstock.com" },
                    to = new[] { new { email = destinataire, name = prenom } },
                    subject = "Réinitialisation de votre mot de passe WicStock",
                    htmlContent = htmlContent
                };
                var json = JsonSerializer.Serialize(payload);
                var response = await client.PostAsync("https://api.brevo.com/v3/smtp/email", new StringContent(json, Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode) return;
                var err = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[BREVO EMAIL ERROR] {err}");
            }

            // Option C: MailKit SMTP classique avec fallback automatique 587 -> 465 (SSL)
            Console.WriteLine($"[EMAIL SERVICE] Connexion SMTP vers {host}:{port} avec l'expéditeur {expediteur}...");

            if (string.IsNullOrWhiteSpace(expediteur) || string.IsNullOrWhiteSpace(motDePasse))
            {
                throw new InvalidOperationException("Configuration SMTP incomplète (Email:Expediteur / Email:MotDePasse non définis).");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(nomAffiche, expediteur));
            message.To.Add(MailboxAddress.Parse(destinataire));
            message.Subject = "Réinitialisation de votre mot de passe WicStock";
            message.Body = new BodyBuilder { HtmlBody = htmlContent }.ToMessageBody();

            using var smtpClient = new SmtpClient();
            smtpClient.Timeout = 10000;
            var secureOption = port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

            try
            {
                await smtpClient.ConnectAsync(host, port, secureOption);
                await smtpClient.AuthenticateAsync(expediteur, motDePasse);
                await smtpClient.SendAsync(message);
                await smtpClient.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SMTP ERROR] {ex.GetType().Name}: {ex.Message}");
                if (port == 587)
                {
                    try
                    {
                        Console.WriteLine("[SMTP FALLBACK] Tentative sur le port 465 (SslOnConnect)...");
                        using var fallbackClient = new SmtpClient();
                        fallbackClient.Timeout = 8000;
                        await fallbackClient.ConnectAsync(host, 465, SecureSocketOptions.SslOnConnect);
                        await fallbackClient.AuthenticateAsync(expediteur, motDePasse);
                        await fallbackClient.SendAsync(message);
                        await fallbackClient.DisconnectAsync(true);
                        return;
                    }
                    catch (Exception fallbackEx)
                    {
                        Console.WriteLine($"[SMTP FALLBACK ERROR] {fallbackEx.Message}");
                    }
                }

                if (ex.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase) || ex is TimeoutException || ex is System.IO.IOException)
                {
                    throw new InvalidOperationException("Render bloque le port SMTP (587/465). Veuillez ajouter la clé gratuite Resend API (Email__ResendApiKey) sur Render.");
                }

                throw;
            }
        }
    }
}
