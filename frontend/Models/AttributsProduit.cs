using System;
using System.Collections.Generic;
using System.Linq;

namespace WicStock.Web.Models
{
    public static class AttributsProduit
    {
        public static readonly List<string> Genres = new()
        {
            "Homme",
            "Femme",
            "Enfant",
            "Unisex"
        };

        public static readonly List<string> TaillesLettres = new()
        {
            "XS", "S", "M", "L", "XL", "XXL"
        };

        public static readonly List<string> TaillesNumeriques = new()
        {
            "36", "38", "40", "42", "44", "46"
        };

        public static readonly List<string> TaillesAutre = new()
        {
            "Standard"
        };

        public static List<string> ToutesLesTailles =>
            TaillesLettres.Concat(TaillesNumeriques).Concat(TaillesAutre).ToList();

        public static readonly Dictionary<string, string> MappingCouleursHex = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Noir", "#18181B" },
            { "Blanc", "#FFFFFF" },
            { "Gris", "#71717A" },
            { "Bleu", "#2563EB" },
            { "Rouge", "#DC2626" },
            { "Vert", "#16A34A" },
            { "Beige", "#D4D4D8" },
            { "Marron", "#78350F" },
            { "Rose", "#EC4899" },
            { "Violet", "#8B5CF6" },
            { "Jaune", "#EAB308" },
            { "Orange", "#F97316" }
        };

        public static List<string> Couleurs => MappingCouleursHex.Keys.ToList();

        public const string FallbackHex = "#A1A1AA";

        public static string GetHex(string? couleur)
        {
            if (string.IsNullOrWhiteSpace(couleur))
                return FallbackHex;

            return MappingCouleursHex.TryGetValue(couleur.Trim(), out var hex)
                ? hex
                : FallbackHex;
        }

        public static string FormatLigneDescription(string? genre, string? taille, string? couleur)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(genre)) parts.Add(genre.Trim());
            if (!string.IsNullOrWhiteSpace(taille)) parts.Add($"Taille {taille.Trim()}");
            if (!string.IsNullOrWhiteSpace(couleur)) parts.Add(couleur.Trim());
            return parts.Count > 0 ? string.Join(" · ", parts) : string.Empty;
        }
    }
}
