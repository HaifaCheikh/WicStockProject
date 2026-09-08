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
