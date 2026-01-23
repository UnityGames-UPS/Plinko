using System.Collections.Generic;
using UnityEngine;
using PlinkoGame.Data;

namespace PlinkoGame.Services
{
    /// <summary>
    /// Handles multiplier mirroring logic as per specification:
    /// Backend sends HALF multipliers, frontend must mirror them symmetrically
    /// </summary>
    public class MultiplierService
    {
        private Dictionary<string, MultiplierMapping> cachedMappings = new Dictionary<string, MultiplierMapping>();

        /// <summary>
        /// Generates full multiplier array from backend's half multipliers
        /// </summary>
        /// <param name="rowCount">Number of rows (8-16)</param>
        /// <param name="riskLevel">LOW, MEDIUM, HIGH</param>
        /// <param name="backendMultipliers">Half multipliers from backend</param>
        /// <returns>Complete multiplier mapping for all catchers</returns>
        public MultiplierMapping GenerateMapping(int rowCount, string riskLevel, List<double> backendMultipliers)
        {
            string key = GetCacheKey(rowCount, riskLevel);

            // Check cache first
            if (cachedMappings.ContainsKey(key))
            {
                return cachedMappings[key];
            }

            int catcherCount = rowCount + 1;
            MultiplierMapping mapping = new MultiplierMapping
            {
                rowCount = rowCount,
                riskLevel = riskLevel,
                fullMultipliers = new List<double>(),
                backendIndices = new List<int>()
            };

            bool isOddCatcherCount = (catcherCount % 2 == 1); // 9, 11, 13, 15, 17 catchers

            if (isOddCatcherCount)
            {
                // ODD CATCHER COUNT (e.g., 9 catchers for row=8)
                // Backend: [0, 1, 2, 3, 4]
                // Result:  [4, 3, 2, 1, 0, 1, 2, 3, 4]
                // Pattern: Mirror from edges to center
                int center = catcherCount / 2; // Center index = 4 for 9 catchers

                for (int i = 0; i < catcherCount; i++)
                {
                    int distanceFromCenter = Mathf.Abs(i - center);
                    int backendIndex = distanceFromCenter;

                    if (backendIndex < backendMultipliers.Count)
                    {
                        mapping.fullMultipliers.Add(backendMultipliers[backendIndex]);
                        mapping.backendIndices.Add(backendIndex);
                    }
                    else
                    {
                        Debug.LogError($"Backend multiplier index {backendIndex} out of range!");
                        mapping.fullMultipliers.Add(1.0); // Fallback
                        mapping.backendIndices.Add(0);
                    }
                }
            }
            else
            {
                // EVEN CATCHER COUNT (e.g., 10 catchers for row=9)
                // Backend: [0, 1, 2, 3, 4]
                // Result:  [4, 3, 2, 1, 0, 0, 1, 2, 3, 4]
                // Pattern: Mirror from edges, center has TWO catchers with same index
                int leftCenter = catcherCount / 2 - 1;  // Index 4 for 10 catchers
                int rightCenter = catcherCount / 2;     // Index 5 for 10 catchers

                for (int i = 0; i < catcherCount; i++)
                {
                    int backendIndex;

                    if (i <= leftCenter)
                    {
                        // Left half (reversed): 4, 3, 2, 1, 0
                        backendIndex = leftCenter - i;
                    }
                    else
                    {
                        // Right half (normal): 0, 1, 2, 3, 4
                        backendIndex = i - rightCenter;
                    }

                    if (backendIndex < backendMultipliers.Count)
                    {
                        mapping.fullMultipliers.Add(backendMultipliers[backendIndex]);
                        mapping.backendIndices.Add(backendIndex);
                    }
                    else
                    {
                        Debug.LogError($"Backend multiplier index {backendIndex} out of range!");
                        mapping.fullMultipliers.Add(1.0);
                        mapping.backendIndices.Add(0);
                    }
                }
            }

            // Cache for future use
            cachedMappings[key] = mapping;

            Debug.Log($"Generated multiplier mapping: Row={rowCount}, Risk={riskLevel}, Catchers={catcherCount}");
            return mapping;
        }

        /// <summary>
        /// Resolves which catcher(s) match the backend multiplier index
        /// Returns list of possible catcher indices
        /// </summary>
        public List<int> ResolveCatcherIndices(MultiplierMapping mapping, int backendMultiplierIndex)
        {
            List<int> possibleCatchers = new List<int>();

            for (int i = 0; i < mapping.backendIndices.Count; i++)
            {
                if (mapping.backendIndices[i] == backendMultiplierIndex)
                {
                    possibleCatchers.Add(i);
                }
            }

            if (possibleCatchers.Count == 0)
            {
                Debug.LogError($"No catchers found for backend index {backendMultiplierIndex}!");
                possibleCatchers.Add(0); // Fallback to first catcher
            }

            return possibleCatchers;
        }

        /// <summary>
        /// Chooses one random catcher from possible matches
        /// </summary>
        public int ChooseRandomCatcher(List<int> possibleCatchers)
        {
            if (possibleCatchers.Count == 1)
            {
                return possibleCatchers[0];
            }

            int randomIndex = Random.Range(0, possibleCatchers.Count);
            return possibleCatchers[randomIndex];
        }

        /// <summary>
        /// Formats multiplier for display (2x, 2.5x, 1K, 1.5K)
        /// </summary>
        public string FormatMultiplier(double multiplier)
        {
            if (multiplier >= 1000)
            {
                return $"{(multiplier / 1000):F1}K";
            }
            else if (multiplier >= 100)
            {
                return $"{multiplier:F0}x";
            }
            else if (multiplier % 1 == 0)
            {
                return $"{multiplier:F0}x";
            }
            else
            {
                return $"{multiplier:F1}x";
            }
        }

        /// <summary>
        /// Clears cached mappings (call on game exit or disconnection)
        /// </summary>
        public void ClearCache()
        {
            cachedMappings.Clear();
            Debug.Log("Multiplier cache cleared");
        }

        private string GetCacheKey(int rowCount, string riskLevel)
        {
            return $"{rowCount}_{riskLevel}";
        }
    }
}