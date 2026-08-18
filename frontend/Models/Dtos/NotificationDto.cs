namespace WicStock.Web.Models.Dtos
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? UrlCible { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.Now;
        public bool Lue { get; set; }
        public string? RoleDestinataire { get; set; }
        public int? UtilisateurDestinataireId { get; set; }
    }
}
