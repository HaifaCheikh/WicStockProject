namespace WicStock.Web.Models.Dtos
{
    public class AvisDto
    {
        public int Id { get; set; }
        public int CommandeId { get; set; }
        public int ProduitId { get; set; }
        public string? ProduitNom { get; set; }
        public string? ProduitReference { get; set; }
        public string? ProduitImageUrl { get; set; }
        public int ClientId { get; set; }
        public string? ClientNom { get; set; }
        public int Note { get; set; }
        public string? Commentaire { get; set; }
        public DateTime DateCreation { get; set; }
        public string Statut { get; set; } = string.Empty;
        public bool EstMasque { get; set; }
    }

    public class CreerModifierAvisDto
    {
        public int CommandeId { get; set; }
        public int Note { get; set; }
        public string? Commentaire { get; set; }
    }

    public class ModererAvisDto
    {
        public string Statut { get; set; } = "PUBLIE";
        public bool EstMasque { get; set; }
    }

    public class ReclamationDto
    {
        public int Id { get; set; }
        public int CommandeId { get; set; }
        public int ProduitId { get; set; }
        public string? ProduitNom { get; set; }
        public string? ProduitReference { get; set; }
        public string? ProduitImageUrl { get; set; }
        public int ClientId { get; set; }
        public string? ClientNom { get; set; }
        public string? ClientEmail { get; set; }
        public string Motif { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? PhotosUrls { get; set; }
        public DateTime DateCreation { get; set; }
        public string Statut { get; set; } = string.Empty;
        public string? ReponseAdmin { get; set; }
        public DateTime? DateReponse { get; set; }
    }

    public class CreerReclamationDto
    {
        public int CommandeId { get; set; }
        public string Motif { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? PhotosUrls { get; set; }
    }

    public class TraiterReclamationDto
    {
        public string Statut { get; set; } = "RESOLUE";
        public string ReponseAdmin { get; set; } = string.Empty;
    }
}
