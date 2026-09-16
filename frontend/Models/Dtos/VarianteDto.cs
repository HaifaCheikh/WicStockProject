namespace WicStock.Web.Models.Dtos
{
    public class VarianteDto
    {
        public int Id { get; set; }
        public int ProduitId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string? Genre { get; set; }
        public string? Taille { get; set; }
        public string? Couleur { get; set; }
        public string? CodeHexCouleur { get; set; }
        public int QuantiteActuelle { get; set; }
        public int SeuilAlerte { get; set; } = 10;
        public decimal? PrixOverride { get; set; }

        public decimal GetPrixEffectif(decimal defaultPrixProduit) => PrixOverride ?? defaultPrixProduit;
    }
}
