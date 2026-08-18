namespace WicStock_.Models.Dtos
{
    public class ProduitCatalogueDto
    {
        public int Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string? TypeTissu { get; set; }
        public string? Categorie { get; set; }
        public decimal PrixUnitaire { get; set; }
        public string? ImageUrl { get; set; }
        public string StatutStock { get; set; } = "DISPONIBLE";
        public int RemisePourcentage { get; set; }
        public DateTime? DateFinPromotion { get; set; }
        public bool EstEnPromotion { get; set; }
        public decimal PrixPromo { get; set; }
        public int QuantiteDisponible { get; set; }
        public bool DisponibleSurCommande { get; set; }
        public bool EstStockFaible { get; set; }
    }
}
