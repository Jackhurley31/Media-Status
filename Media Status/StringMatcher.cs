using System;
using System.Collections.Generic;

namespace MediaControlApp.Helpers // Or MediaControlApp.Models depending on your namespace
{
    public static class StringMatcher
    {
        public static string MatchOrCreateSeriesName(string candidateName, IEnumerable<string> existingSeries)
        {
            if (string.IsNullOrWhiteSpace(candidateName)) return candidateName;

            foreach (var existing in existingSeries)
            {
                // If character edit distance is 2 or fewer (e.g., Fraisher vs Frasier), group under existing name
                if (ComputeLevenshteinDistance(candidateName.ToLower(), existing.ToLower()) <= 2)
                {
                    return existing;
                }
            }
            return candidateName;
        }

        private static int ComputeLevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int[] v0 = new int[t.Length + 1];
            int[] v1 = new int[t.Length + 1];

            for (int i = 0; i < v0.Length; i++) v0[i] = i;

            for (int i = 0; i < s.Length; i++)
            {
                v1[0] = i + 1;
                for (int j = 0; j < t.Length; j++)
                {
                    int cost = (s[i] == t[j]) ? 0 : 1;
                    v1[j + 1] = Math.Min(Math.Min(v1[j] + 1, v0[j + 1] + 1), v0[j] + cost);
                }
                Array.Copy(v1, v0, v0.Length);
            }

            return v0[t.Length];
        }
    }
}