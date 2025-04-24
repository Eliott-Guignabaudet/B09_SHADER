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
            Color[] orderedColors = new Color[a_colors.Length];

            // Tableaux pour stocker les distances entre chaque point et chaque coin
            float[,] distances = new float[a_points.Length, a_corner.Length];

            // Calcul des distances entre chaque point et chaque coin
            for (int i = 0; i < a_points.Length; i++)
            {
                for (int j = 0; j < a_corner.Length; j++)
                {
                    distances[i, j] = Vector2.Distance(a_points[i], a_corner[j]);
                }
            }
            
            // Générer toutes les permutations des coins
            List<List<int>> permutations = GeneratePermutations(a_corner.Length);

            // Variables pour suivre la meilleure permutation (celle avec la distance minimale)
            float minTotalDistance = float.MaxValue;
            List<int> bestPermutation = null;

            foreach (var permutation in permutations)
            {
                float totalDistance = 0;

                for (int i = 0; i < a_points.Length; i++)
                {
                    int coinIndex = permutation[i];
                    totalDistance += distances[i, coinIndex];
                }

                // Si cette permutation est meilleure (c'est-à-dire avec une distance plus petite), on la garde
                if (totalDistance < minTotalDistance)
                {
                    minTotalDistance = totalDistance;
                    bestPermutation = permutation;
                }
                
            }
            for (int i = 0; i < a_points.Length; i++)
            {
                int coinIndex = bestPermutation[i];
                orderedColors[coinIndex] = a_colors[i];
            }
            
            
            return orderedColors;
        }
        
        
    }
}