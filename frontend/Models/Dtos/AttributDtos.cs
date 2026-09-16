namespace WicStock.Web.Models.Dtos
{
    public class AttributValeurDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Valeur { get; set; } = string.Empty;
        public string? CodeHex { get; set; }
        public int Ordre { get; set; }
        public bool Actif { get; set; } = true;
    }
}
