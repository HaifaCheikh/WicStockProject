using Microsoft.AspNetCore.SignalR;
using WicStock_.Hubs;
using WicStock_.Models;
using static WicStock_.Models.Enums;

namespace WicStock_.Services
{
    public class NotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<Notification> NotifierNouvelEvenementAsync(TypeNotification type, string message, string? urlCible, RoleUtilisateur? roleDestinataire = null, int? utilisateurDestinataireId = null)
        {
            var notificationPrincipale = new Notification
            {
                Type = type,
                Message = message,
                UrlCible = urlCible,
                DateCreation = DateTime.Now,
                Lue = false,
                RoleDestinataire = roleDestinataire,
                UtilisateurDestinataireId = utilisateurDestinataireId
            };

            _context.Notifications.Add(notificationPrincipale);
            await _context.SaveChangesAsync();

            var dtoPrincipale = new
            {
                notificationPrincipale.Id,
                Type = notificationPrincipale.Type.ToString(),
                notificationPrincipale.Message,
                notificationPrincipale.UrlCible,
                notificationPrincipale.DateCreation,
                notificationPrincipale.Lue,
                RoleDestinataire = notificationPrincipale.RoleDestinataire?.ToString(),
                UtilisateurDestinataireId = notificationPrincipale.UtilisateurDestinataireId
            };

            if (utilisateurDestinataireId.HasValue)
            {
                // Envoyer à un utilisateur spécifique
                await _hubContext.Clients.Group($"user_{utilisateurDestinataireId.Value}").SendAsync("ReceiveNotification", dtoPrincipale);
            }
            else if (roleDestinataire.HasValue)
            {
                await _hubContext.Clients.Group(roleDestinataire.Value.ToString()).SendAsync("ReceiveNotification", dtoPrincipale);

                if (roleDestinataire.Value == RoleUtilisateur.RESPONSABLE_STOCK_PRODUCTION)
                {
                    var notificationAdmin = new Notification
                    {
                        Type = type,
                        Message = message,
                        UrlCible = urlCible,
                        DateCreation = DateTime.Now,
                        Lue = false,
                        RoleDestinataire = RoleUtilisateur.ADMIN
                    };

                    _context.Notifications.Add(notificationAdmin);
                    await _context.SaveChangesAsync();

                    var dtoAdmin = new
                    {
                        notificationAdmin.Id,
                        Type = notificationAdmin.Type.ToString(),
                        notificationAdmin.Message,
                        notificationAdmin.UrlCible,
                        notificationAdmin.DateCreation,
                        notificationAdmin.Lue,
                        RoleDestinataire = notificationAdmin.RoleDestinataire?.ToString(),
                        UtilisateurDestinataireId = notificationAdmin.UtilisateurDestinataireId
                    };

                    await _hubContext.Clients.Group(RoleUtilisateur.ADMIN.ToString()).SendAsync("ReceiveNotification", dtoAdmin);
                }
            }
            else
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", dtoPrincipale);
            }

            return notificationPrincipale;
        }
    }
}
