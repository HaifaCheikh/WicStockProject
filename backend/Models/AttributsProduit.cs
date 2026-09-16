using System;
using System.Collections.Generic;
using System.Linq;

namespace WicStock_.Models
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
            { "Gris anthracite", "#3F3F46" },
            { "Gris clair", "#E4E4E7" },
            { "Bleu", "#2563EB" },
            { "Bleu indigo", "#4F46E5" },
            { "Bleu marine", "#1E3A8A" },
            { "Bleu ciel", "#38BDF8" },
            { "Rouge", "#DC2626" },
            { "Vert", "#16A34A" },
            { "Vert olive", "#65A30D" },
            { "Beige", "#D4D4D8" },
            { "Marron", "#78350F" },
            { "Rose", "#EC4899" },
            { "Violet", "#8B5CF6" },
            { "Jaune", "#EAB308" },
            { "Orange", "#F97316" },
            { "Denim", "#3B82F6" },
            { "Indigo", "#4F46E5" },
            { "Kaki", "#4D7C0F" },
            { "Bordeaux", "#881337" },
            { "Camel", "#B45309" },
            { "Marine", "#1E3A8A" },
            { "Anthracite", "#3F3F46" }
        };

        public static List<string> Couleurs => MappingCouleursHex.Keys.ToList();

        public const string FallbackHex = "#A1A1AA";

        public static string? GetHex(string? couleur)
        {
            if (string.IsNullOrWhiteSpace(couleur))
                return null;

            var val = couleur.Trim();
            if (MappingCouleursHex.TryGetValue(val, out var hex))
                return hex;

            foreach (var kvp in MappingCouleursHex)
            {
                if (val.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }

            return FallbackHex;
        }

        public static bool EstGenreValide(string? genre)
        {
            if (string.IsNullOrWhiteSpace(genre)) return true;
            return Genres.Any(g => string.Equals(g, genre.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public static bool EstTailleValide(string? taille)
        {
            if (string.IsNullOrWhiteSpace(taille)) return true;
            return ToutesLesTailles.Any(t => string.Equals(t, taille.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public static bool EstCouleurValide(string? couleur)
        {
            if (string.IsNullOrWhiteSpace(couleur)) return true;
            return Couleurs.Any(c => string.Equals(c, couleur.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
