namespace WicStock_.Models.Dtos
{
    public class ProfilDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telephone { get; set; }
        public string? PhotoUrl { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    public class ModifierProfilDto
    {
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telephone { get; set; }
    }

    public class ChangerMotDePasseDto
    {
        public string AncienMotDePasse { get; set; } = string.Empty;
        public string NouveauMotDePasse { get; set; } = string.Empty;
        public string ConfirmationMotDePasse { get; set; } = string.Empty;
    }

    public class PhotoResponseDto
    {
        public string? PhotoUrl { get; set; }
    }
}
