using static WicStock_.Models.Enums;

namespace WicStock_.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public TypeNotification Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? UrlCible { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.Now;
        public bool Lue { get; set; } = false;
        public RoleUtilisateur? RoleDestinataire { get; set; }
        public int? UtilisateurDestinataireId { get; set; }
    }
}
