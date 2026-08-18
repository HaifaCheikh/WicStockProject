namespace WicStock.Web.Models.Dtos
{
    public class UtilisateurDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telephone { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    public class ChangerRoleDto
    {
        public string NouveauRole { get; set; } = string.Empty;
    }

    public class ModifierUtilisateurDto
    {
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telephone { get; set; }
    }
}
