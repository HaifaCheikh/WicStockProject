namespace WicStock_.Models.Dtos
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
        public int SeuilAlerte { get; set; }
        public decimal? PrixOverride { get; set; }
        public decimal PrixEffectif { get; set; }
    }

    public class VarianteCreateUpdateDto
    {
        public int? Id { get; set; }
        public string? Reference { get; set; }
        public string? Genre { get; set; }
        public string? Taille { get; set; }
        public string? Couleur { get; set; }
        public int QuantiteActuelle { get; set; } = 0;
        public int SeuilAlerte { get; set; } = 10;
        public decimal? PrixOverride { get; set; }
    }
}
