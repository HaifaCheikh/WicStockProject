using System.Text.Json.Serialization;

namespace WicStock.Web.Models.Dtos
{
    public class QuestionIARequest
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("question")]
        public string Question { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("utilisateur_id")]
        public int? UtilisateurId { get; set; }

        [JsonPropertyName("produit_id")]
        public int? ProduitId { get; set; }
    }

    public class ChartDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "bar"; // "bar", "donut", "line"

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();

        [JsonPropertyName("series")]
        public List<double> Series { get; set; } = new();
    }

    public class ReponseIA
    {
        [JsonPropertyName("question")]
        public string Question { get; set; } = string.Empty;

        [JsonPropertyName("reponse")]
        public string Reponse { get; set; } = string.Empty;

        [JsonPropertyName("sql_genere")]
        public string? SqlGenere { get; set; }

        [JsonPropertyName("entree_id_catalogue")]
        public string? EntreeIdCatalogue { get; set; }

        [JsonPropertyName("score_similarite")]
        public double? ScoreSimilarite { get; set; }

        [JsonPropertyName("chart")]
        public ChartDto? Chart { get; set; }

        [JsonPropertyName("resultats")]
        public System.Text.Json.JsonElement? Resultats { get; set; }
    }
}
