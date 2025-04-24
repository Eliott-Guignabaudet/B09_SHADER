using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Complete
{
    public static class TankCornerAssociator
    {
        
        // Fonction pour générer toutes les permutations d'une liste de coins
        private static List<List<int>> GeneratePermutations(int n)
        {
            List<List<int>> permutations = new List<List<int>>();
            Permute(0, new List<int>(), new bool[n], permutations);
            return permutations;
        }

        // Fonction récursive pour générer les permutations
        private static void Permute(int index, List<int> current, bool[] used, List<List<int>> permutations)
        {
            if (index == used.Length)
            {
                permutations.Add(new List<int>(current));
                return;
            }

            for (int i = 0; i < used.Length; i++)
            {
                if (!used[i])
                {
                    used[i] = true;
                    current.Add(i);
                    Permute(index + 1, current, used, permutations);
                    current.RemoveAt(current.Count - 1);
                    used[i] = false;
                }
            }
        }
        
        public static Color[] GetOrderedColorArray(Vector2[] a_points, Color[] a_colors, Vector2[] a_corner)
        {
            // Create a new array to hold the ordered colors
            Color[] orderedColors = new Color[a_corner.Length];

            List<int> bestPermutation = null;

            if (a_points.Length == 1)
            {
                bestPermutation = new List<int>() { 0, 0, 0, 0 };
            }
            else if (a_points.Length == 2)
            {
                bestPermutation = GetOrderedColorFor2Tanks(a_points, a_corner);
            }
            else if (a_points.Length == 3)
            {
                bestPermutation = AssocierTroisTanks(a_points, a_corner);
            }
            else if (a_points.Length == 4)
            {
                bestPermutation = AssocierQuatreTanks(a_points, a_corner);
            }
            else
            {
                Debug.LogError("Invalid number of tanks. Expected 2, 3, or 4 tanks.");
                return orderedColors;
            }

            
            
            for (int i = 0; i < a_points.Length; i++)
            {
                int coinIndex = bestPermutation[i];
                orderedColors[coinIndex] = a_colors[i];
            }

            for (int i = 0; i < bestPermutation.Count; i++)
            {
                orderedColors[i] = a_colors[bestPermutation[i]];
            }
            return orderedColors;
        }

        private static List<int> GetOrderedColorFor2Tanks(Vector2[] tanks, Vector2[] coins)
        {
            float[,] distances = new float[tanks.Length, coins.Length];
            for (int i = 0; i < tanks.Length; i++)
            for (int j = 0; j < coins.Length; j++)
                distances[i, j] = Vector2.Distance(tanks[i], coins[j]);

            var permutations = new List<List<int>>();
            Permute(0, new List<int>(), new bool[4], permutations);

            float minTotal = float.MaxValue;
            List<int> bestAssign = null;

            foreach (var perm in permutations)
            {
                float total = 0;
                total += distances[0, perm[0]] + distances[0, perm[1]];
                total += distances[1, perm[2]] + distances[1, perm[3]];

                if (total < minTotal)
                {
                    minTotal = total;
                    var assign = new int[4];
                    assign[perm[0]] = 0;
                    assign[perm[1]] = 0;
                    assign[perm[2]] = 1;
                    assign[perm[3]] = 1;
                    bestAssign = new List<int>(assign);
                }
            }

            return bestAssign;
        }
        private static List<int> AssocierTroisTanks(Vector2[] tanks, Vector2[] coins)
        {
            float[,] distances = new float[tanks.Length, coins.Length];
            for (int i = 0; i < tanks.Length; i++)
                for (int j = 0; j < coins.Length; j++)
                    distances[i, j] = Vector2.Distance(tanks[i], coins[j]);

            float minTotalDistance = float.MaxValue;
            List<int> bestAssignment = null;

            // Tank principal qui aura 2 coins
            for (int mainTank = 0; mainTank < tanks.Length; mainTank++)
            {
                // Choisir toutes les paires de coins possibles
                for (int i = 0; i < coins.Length; i++)
                {
                    for (int j = i + 1; j < coins.Length; j++)
                    {
                        List<int> usedCoins = new List<int> { i, j };

                        // Coins restants
                        List<int> remainingCoins = new List<int>();
                        for (int c = 0; c < 4; c++)
                            if (!usedCoins.Contains(c)) remainingCoins.Add(c);

                        // Tanks restants
                        List<int> otherTanks = new List<int>();
                        for (int t = 0; t < 3; t++)
                            if (t != mainTank) otherTanks.Add(t);

                        // Tester les 2 permutations des coins restants
                        for (int p = 0; p < 2; p++)
                        {
                            int coinA = remainingCoins[p];
                            int coinB = remainingCoins[1 - p];
                            int tankA = otherTanks[0];
                            int tankB = otherTanks[1];

                            float totalDistance =
                                distances[mainTank, i] + distances[mainTank, j] +
                                distances[tankA, coinA] + distances[tankB, coinB];

                            if (totalDistance < minTotalDistance)
                            {
                                minTotalDistance = totalDistance;

                                // Génère le mapping [coin0Tank, coin1Tank, coin2Tank, coin3Tank]
                                var assignment = new int[4];
                                assignment[i] = mainTank;
                                assignment[j] = mainTank;
                                assignment[coinA] = tankA;
                                assignment[coinB] = tankB;

                                bestAssignment = new List<int>(assignment);
                            }
                        }
                    }
                }
            }

            return bestAssignment;
        }

        // Fonction pour associer 4 tanks à 4 coins (1 coin par tank)
        private static List<int> AssocierQuatreTanks(Vector2[] tanks, Vector2[] coins)
        {
            float[,] distances = new float[tanks.Length, coins.Length];
            for (int i = 0; i < tanks.Length; i++)
            for (int j = 0; j < coins.Length; j++)
                distances[i, j] = Vector2.Distance(tanks[i], coins[j]);

            var permutations = new List<List<int>>();
            Permute(0, new List<int>(), new bool[4], permutations);

            float minTotal = float.MaxValue;
            List<int> bestAssign = null;

            foreach (var perm in permutations)
            {
                float total = 0;
                for (int i = 0; i < 4; i++)
                    total += distances[i, perm[i]];

                if (total < minTotal)
                {
                    minTotal = total;
                    var assign = new int[4];
                    for (int i = 0; i < 4; i++)
                        assign[perm[i]] = i;
                    bestAssign = new List<int>(assign);
                }
            }

            return bestAssign;

        }
    }
}