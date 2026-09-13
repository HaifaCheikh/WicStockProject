namespace WicStock_.Models.Dtos
{
    /// <summary>
    /// DTO reçu par PUT api/Produit/{id}.
    /// Évite les conflits de tracking EF en ne recevant que les champs éditables,
    /// sans navigation properties ni propriétés calculées.
    /// </summary>
    public class ProduitUpdateDto
    {
        public int Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string TypeTissu { get; set; } = string.Empty;
        public string Categorie { get; set; } = string.Empty;
        public string? Genre { get; set; }
        public string? Taille { get; set; }
        public string? Couleur { get; set; }
        public string CycleDeVie { get; set; } = string.Empty;
        public decimal PrixUnitaire { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageBase64 { get; set; }
        public bool DisponibleSurCommande { get; set; }
        public int? RemisePourcentage { get; set; }
        public DateTime? DateFinPromotion { get; set; }

        public StockUpdateDto? Stock { get; set; }
        public List<VarianteCreateUpdateDto> Variantes { get; set; } = new();
    }

    public class StockUpdateDto
    {
        public int Id { get; set; }
        public int QuantiteActuelle { get; set; }
        public int SeuilAlerte { get; set; } = 10;
        public string Emplacement { get; set; } = "Magasin principal";
    }
}
