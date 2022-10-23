using System;
using System.Linq;

namespace rickhelper
{
    public class GameComparer
    {
        public static CompareResult Compare(GameList gameList, string game)
        {
            var result = gameList.Games.FirstOrDefault(g => string.Compare(game.Trim(), g.Name.Trim(), true) == 0);
            if (result != null) return new CompareResult { Game = game, Rate = 100 };

            CompareResult maxGame = null;

            foreach(var currentGame in gameList.Games)
            {
                var rate = CalculateSimilarity(game, currentGame.Name);
                if(maxGame==null || rate > maxGame.Rate)
                {
                    maxGame = new CompareResult { Game = currentGame.Name, Rate = (int)rate };
                    continue;
                }
            }

            return maxGame;
        }

        private static double CalculateSimilarity(string source, string target)
        {
            if ((source == null) || (target == null)) return 0.0;
            if ((source.Length == 0) || (target.Length == 0)) return 0.0;
            if (source == target) return 1.0;

            int stepsToSame = Compute(source, target);
            return (1.0 - ((double)stepsToSame / (double)Math.Max(source.Length, target.Length)))*100;
        }

        private static int Compute(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    }
}
