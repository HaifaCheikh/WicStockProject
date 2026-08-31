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

        [JsonPropertyName("colors")]
        public string[]? Colors { get; set; }

        [JsonPropertyName("custom_palette")]
        public string[]? CustomPalette { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }
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

    // ============================================================
    // DTOs pour l'endpoint /chat (WicStock AI v2)
    // ============================================================

    /// <summary>Requête vers POST /chat</summary>
    public class ChatRequest
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("utilisateur_id")]
        public int? UtilisateurId { get; set; }

        [JsonPropertyName("produit_id")]
        public int? ProduitId { get; set; }
    }

    /// <summary>Réponse unifiée de POST /chat</summary>
    public class AssistantResponseDto
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("chart")]
        public ChartDto? Chart { get; set; }

        [JsonPropertyName("pending_state")]
        public string? PendingState { get; set; }   // ex: "AWAITING_FORMAT_CHOICE"

        [JsonPropertyName("options")]
        public List<QuickOptionDto>? Options { get; set; }

        [JsonPropertyName("intent")]
        public string? Intent { get; set; }

        [JsonPropertyName("sql_genere")]
        public string? SqlGenere { get; set; }

        [JsonPropertyName("agent_source")]
        public string? AgentSource { get; set; }

        [JsonPropertyName("suggestions")]
        public List<string>? Suggestions { get; set; }
    }

    /// <summary>Bouton quick reply affiché dans la bulle de chat</summary>
    public class QuickOptionDto
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("is_free_text")]
        public bool IsFreeText { get; set; } = false;
    }
}
