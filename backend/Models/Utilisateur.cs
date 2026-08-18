using static WicStock_.Models.Enums;

namespace WicStock_.Models
{
    public class Utilisateur
    {
        public int Id { get; set; }

        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string? Telephone { get; set; }
        public string? Adresse { get; set; }
        public string? CodePostal { get; set; }
        public string? Ville { get; set; }
        public string? PhotoUrl { get; set; }
        public string MotDePasseHash { get; set; } = string.Empty;
        public RoleUtilisateur Role { get; set; }

        // Navigation
        public List<Alerte> AlertesTraitees { get; set; } = new();
        public List<ActionRecommandee> ActionsValidees { get; set; } = new();

        // Sera implémenté plus tard avec JWT
        public string SeConnecter()
        {
            throw new NotImplementedException();
        }
    }
}
