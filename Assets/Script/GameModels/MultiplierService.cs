using System.Collections.Generic;
using UnityEngine;
using PlinkoGame.Data;

namespace PlinkoGame.Services
{
    /// <summary>
    /// Handles multiplier mirroring logic as per specification
    /// FIXED: Shows exact multiplier values (up to 2 decimals)
    /// FIXED: Shows probability with 5 decimal places
    /// </summary>
    public class MultiplierService
    {
        private Dictionary<string, MultiplierMapping> cachedMappings = new Dictionary<string, MultiplierMapping>();

        /// <summary>
        /// Generates full multiplier array from backend's half multipliers
        /// </summary>
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

            bool isOddCatcherCount = (catcherCount % 2 == 1);

            if (isOddCatcherCount)
            {
                // ODD CATCHER COUNT (e.g., 9 catchers for row=8)
                int center = catcherCount / 2;

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
                        mapping.fullMultipliers.Add(1.0);
                        mapping.backendIndices.Add(0);
                    }
                }
            }
            else
            {
                // EVEN CATCHER COUNT (e.g., 10 catchers for row=9)
                int leftCenter = catcherCount / 2 - 1;
                int rightCenter = catcherCount / 2;

                for (int i = 0; i < catcherCount; i++)
                {
                    int backendIndex;

                    if (i <= leftCenter)
                    {
                        backendIndex = leftCenter - i;
                    }
                    else
                    {
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

            cachedMappings[key] = mapping;

            Debug.Log($"Generated multiplier mapping: Row={rowCount}, Risk={riskLevel}, Catchers={catcherCount}");
            return mapping;
        }

        /// <summary>
        /// Resolves which catcher(s) match the backend multiplier index
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
                possibleCatchers.Add(0);
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
        /// FIXED: Format multiplier to show EXACT values from backend
        /// Shows up to 2 decimal places (e.g., 0.48, 0.96, 1.06)
        /// </summary>
        public string FormatMultiplier(double multiplier)
        {
            if (multiplier >= 1000)
            {
                // Format as K (thousands)
                double thousands = multiplier / 1000.0;
                // Remove trailing zeros
                string formatted = thousands.ToString("0.##");
                return $"{formatted}K";
            }
            else
            {
                // Show up to 2 decimal places, removing trailing zeros
                string formatted = multiplier.ToString("0.##");
                return $"{formatted}x";
            }
        }

        /// <summary>
        /// FIXED: Format probability with up to 5 decimal places
        /// Examples: 0.27344%, 0.00391%, 24.60938%
        /// </summary>
        public string FormatProbability(double probability)
        {
            // Convert to percentage (multiply by 100)
            double percentage = probability * 100.0;

            // Show up to 5 decimal places, removing trailing zeros
            string formatted = percentage.ToString("0.#####");
            return $"{formatted}%";
        }

        /// <summary>
        /// Clears cached mappings (call on game exit or disconnection)
        /// </summary>
        internal void ClearCache()
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