namespace WicStock.Web.Models.Dtos
{
    public class ProduitCatalogueDto
    {
        public int Id { get; set; }
        public string Reference { get; set; } = "";
        public string Nom { get; set; } = "";
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

    public class CommandeCreateResultDto
    {
        public int Id { get; set; }
        public bool EstSurCommande { get; set; }
        public string? Message { get; set; }
    }

    public class ConfirmerCommandeRequestDto
    {
        public DateTime? DateEstimeePreparation { get; set; }
    }
}
