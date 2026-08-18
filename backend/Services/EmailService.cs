using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace WicStock_.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task EnvoyerCodeReinitialisationAsync(string destinataire, string prenom, string code)
        {
            var host = _config["Email:Host"]!;
            var port = int.Parse(_config["Email:Port"]!);
            var expediteur = _config["Email:Expediteur"]!;
            var motDePasse = _config["Email:MotDePasse"]!;
            var nomAffiche = _config["Email:NomAffiché"] ?? "WicStock";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(nomAffiche, expediteur));
            message.To.Add(MailboxAddress.Parse(destinataire));
            message.Subject = "Réinitialisation de votre mot de passe WicStock";

            var body = new BodyBuilder
            {
                HtmlBody = $"""
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
                    """
            };

            message.Body = body.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(expediteur, motDePasse);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
