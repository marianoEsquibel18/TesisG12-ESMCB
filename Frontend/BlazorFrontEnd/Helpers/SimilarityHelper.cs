using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorFrontEnd.Helpers
{
    public static class SimilarityHelper
    {
        /// <summary>
        /// Determina si dos cadenas de caracteres representan nombres similares
        /// bajo criterios de coincidencia exacta, subcadenas o distancia de edición (Levenshtein).
        /// </summary>
        public static bool AreSimilar(string val1, string val2)
        {
            if (string.IsNullOrWhiteSpace(val1) || string.IsNullOrWhiteSpace(val2))
                return false;

            string n1 = NormalizeString(val1);
            string n2 = NormalizeString(val2);

            if (n1 == n2) return true;

            // Verificación de subcadenas si tienen un largo mínimo
            if (n1.Length >= 4 && n2.Contains(n1)) return true;
            if (n2.Length >= 4 && n1.Contains(n2)) return true;

            // Tokenizar por palabras
            var words1 = Tokenize(val1);
            var words2 = Tokenize(val2);

            if (!words1.Any() || !words2.Any()) return false;

            int matchCount = 0;
            foreach (var w1 in words1)
            {
                foreach (var w2 in words2)
                {
                    if (AreWordsFuzzyMatch(w1, w2))
                    {
                        matchCount++;
                        break; // Contar cada palabra de words1 como máximo una vez
                    }
                }
            }

            int minWords = Math.Min(words1.Count, words2.Count);
            // Considerar similares si al menos el 50% de las palabras del más corto coinciden,
            // con un mínimo de 1 si tiene 1 sola palabra, o 2 si tiene 2 o más palabras.
            int requiredMatches = minWords == 1 ? 1 : Math.Max(2, (int)Math.Ceiling(minWords * 0.5));
            return matchCount >= requiredMatches;
        }

        private static List<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            return text.Split(new[] { ' ', '-', '_', '/' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(NormalizeString)
                       .Where(w => w.Length >= 2) // Ignorar palabras cortas como conectores (y, de, la, etc.)
                       .ToList();
        }

        private static string NormalizeString(string val)
        {
            if (val == null) return string.Empty;
            var normalized = val.ToLowerInvariant().Trim();
            // Normalizar acentos en español
            normalized = normalized
                .Replace('á', 'a')
                .Replace('é', 'e')
                .Replace('í', 'i')
                .Replace('ó', 'o')
                .Replace('ú', 'u')
                .Replace('ü', 'u')
                .Replace('ñ', 'n');
            return new string(normalized.Where(c => char.IsLetterOrDigit(c)).ToArray());
        }

        private static bool AreWordsFuzzyMatch(string w1, string w2)
        {
            if (w1 == w2) return true;
            int distance = LevenshteinDistance(w1, w2);
            int maxAllowedDistance = Math.Max(w1.Length, w2.Length) >= 6 ? 2 : 1;
            return distance <= maxAllowedDistance;
        }

        private static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
    }
}
