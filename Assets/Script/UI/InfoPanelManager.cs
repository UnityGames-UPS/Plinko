using UnityEngine;
using TMPro;
using PlinkoGame.Data;
using System.Text;
using System.Collections.Generic;

namespace PlinkoGame
{
    /// <summary>
    /// Dynamically generates and updates the game info panel text
    /// based on received init data
    /// </summary>
    public class InfoPanelManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI infoPanelText;

        /// <summary>
        /// Generates the complete info panel text from init data
        /// </summary>
        public void GenerateInfoText(PlinkoGameData gameData)
        {
            if (infoPanelText == null || gameData == null)
            {
                Debug.LogError("[InfoPanel] Missing references");
                return;
            }

            StringBuilder sb = new StringBuilder();

            // Header
            sb.AppendLine("<b><size=36>How to Play Plinko?</size></b>");
            sb.AppendLine();
            sb.AppendLine("Plinko is a classic game of chance where the player drops a ball in a multi-row pin pyramid, with the ball bouncing between pins in a random route until it reaches a destination at the bottom of the pyramid.");
            sb.AppendLine();
            sb.AppendLine("The location where the ball lands determines the payout, with large payouts towards the edges of the pin pyramid, whilst the centre of the pyramid provides lower payouts and losses.");
            sb.AppendLine();
            sb.AppendLine("The strategy to succeed in Plinko is all about bankroll control and surviving the lows until variance turns in your favour.");
            sb.AppendLine();
            sb.AppendLine();

            // Gameplay Options
            sb.AppendLine("<b><size=32>Gameplay Options</size></b>");
            sb.AppendLine();
            sb.AppendLine("<b>Risk Level:</b> Control the volatility of payouts");

            // Generate risk level descriptions dynamically
            if (gameData.risks != null)
            {
                foreach (var risk in gameData.risks)
                {
                    sb.AppendLine($"   <b>{risk.name}</b> - {risk.description}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("<b>Rows:</b> Select between 8 to 16 rows");
            sb.AppendLine("   More rows = More pins = Altered payouts");
            sb.AppendLine();
            sb.AppendLine();

            // Generate risk tables dynamically
            if (gameData.risks != null && gameData.rows != null)
            {
                foreach (var risk in gameData.risks)
                {
                    GenerateRiskTable(sb, gameData, risk);
                }
            }

            // Strategy Tips
            sb.AppendLine("<b><size=32>Strategy Tips</size></b>");
            sb.AppendLine();
            sb.AppendLine("<b>Bankroll Management</b>");
            sb.AppendLine("   Control betting sizes to survive variance");
            sb.AppendLine();
            sb.AppendLine("<b>Risk Selection</b>");
            sb.AppendLine("   Match risk level to your playing style");
            sb.AppendLine();
            sb.AppendLine("<b>Row Configuration</b>");
            sb.AppendLine("   More rows = More destinations");
            sb.AppendLine();
            sb.AppendLine("<b>Patience</b>");
            sb.AppendLine("   Wait for variance to turn in your favour");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("<size=20><b>Good luck and play responsibly!</b></size>");

            infoPanelText.text = sb.ToString();
            Debug.Log("[InfoPanel] Info text generated successfully");
        }

        /// <summary>
        /// Generates a risk level table (LOW/MEDIUM/HIGH)
        /// Uses minimal spacing for compact alignment
        /// </summary>
        private void GenerateRiskTable(StringBuilder sb, PlinkoGameData gameData, RiskLevel risk)
        {
            sb.AppendLine($"<b><size=32>{GetRiskDisplayName(risk.name)} Options</size></b>");
            sb.AppendLine();

            // Table header with minimal spacing (4-5 spaces between columns)
            sb.AppendLine("<b>Rows     Destinations      Min    Max</b>");

            double maxMultiplier = 0;
            double maxProbability = 0;

            foreach (var row in gameData.rows)
            {
                int rowCount = int.Parse(row.id);
                int destinations = rowCount + 1;

                // Find the risk data for this row
                PlinkoRisk riskData = GetRiskDataForRow(row, risk.id);
                if (riskData == null || riskData.multipliers == null || riskData.multipliers.Count == 0)
                    continue;

                double minMultiplier = FindMinMultiplier(riskData.multipliers);
                double rowMaxMultiplier = FindMaxMultiplier(riskData.multipliers);

                // Track overall max
                if (rowMaxMultiplier > maxMultiplier)
                {
                    maxMultiplier = rowMaxMultiplier;
                    // Find probability of max multiplier
                    int maxIndex = riskData.multipliers.IndexOf(rowMaxMultiplier);
                    if (maxIndex >= 0 && maxIndex < riskData.probability.Count)
                    {
                        maxProbability = riskData.probability[maxIndex];
                    }
                }

                string minStr = FormatMultiplierForTable(minMultiplier);
                string maxStr = FormatMultiplierForTable(rowMaxMultiplier);

                // Format each column with compact spacing (more space for max column)
                string rowCol = PadCenter(rowCount.ToString(), 9);
                string destCol = PadCenter(destinations.ToString(), 17);
                string minCol = PadCenter(minStr, 12);

                // Highlight the max multiplier row (16 rows)
                if (rowCount == 16)
                {
                    sb.AppendLine($"{rowCol}{destCol}{minCol}<b>{maxStr}</b>");
                }
                else
                {
                    sb.AppendLine($"{rowCol}{destCol}{minCol}{maxStr}");
                }
            }

            // Add max payout info with proper formatting
            sb.AppendLine();
            string maxPayoutStr = FormatMultiplierForTable(maxMultiplier);
            sb.AppendLine($"<b>Max Payout: {maxPayoutStr} ({maxProbability:F2}% chance)</b>");
            sb.AppendLine();
            sb.AppendLine();
        }

        /// <summary>
        /// Pads string to center it within given width
        /// </summary>
        private string PadCenter(string text, int width)
        {
            if (text.Length >= width) return text;
            int totalPadding = width - text.Length;
            int leftPadding = totalPadding / 2;
            int rightPadding = totalPadding - leftPadding;
            return new string(' ', leftPadding) + text + new string(' ', rightPadding);
        }

        /// <summary>
        /// Pads string to left (for right alignment in monospace)
        /// </summary>
        private string PadLeft(string text, int width)
        {
            if (text.Length >= width) return text;
            int spaces = width - text.Length;
            return new string(' ', spaces) + text;
        }

        /// <summary>
        /// Gets risk data for a specific row and risk level
        /// </summary>
        private PlinkoRisk GetRiskDataForRow(PlinkoRow row, int riskId)
        {
            if (row.risks != null && riskId < row.risks.Count)
            {
                return row.risks[riskId];
            }
            return null;
        }

        /// <summary>
        /// Finds minimum multiplier in list
        /// </summary>
        private double FindMinMultiplier(List<double> multipliers)
        {
            double min = double.MaxValue;
            foreach (double mult in multipliers)
            {
                if (mult < min) min = mult;
            }
            return min;
        }

        /// <summary>
        /// Finds maximum multiplier in list
        /// </summary>
        private double FindMaxMultiplier(List<double> multipliers)
        {
            double max = 0;
            foreach (double mult in multipliers)
            {
                if (mult > max) max = mult;
            }
            return max;
        }

        /// <summary>
        /// Formats multiplier for table display with consistent width
        /// </summary>
        private string FormatMultiplierForTable(double multiplier)
        {
            if (multiplier >= 1000)
            {
                // Format as 1K, 2K, etc - no decimal if whole number
                if (multiplier % 1000 == 0)
                {
                    return $"{(multiplier / 1000):F0}K";
                }
                else
                {
                    return $"{(multiplier / 1000):F1}K";
                }
            }
            else if (multiplier >= 100)
            {
                // Format as whole number with 'x'
                return $"{multiplier:F0}x";
            }
            else if (multiplier >= 10)
            {
                // Format with one decimal if needed
                if (multiplier % 1 == 0)
                {
                    return $"{multiplier:F0}x";
                }
                else
                {
                    return $"{multiplier:F1}x";
                }
            }
            else
            {
                // Always show one decimal for small numbers
                return $"{multiplier:F1}x";
            }
        }

        /// <summary>
        /// Gets display name for risk level
        /// </summary>
        private string GetRiskDisplayName(string riskName)
        {
            switch (riskName)
            {
                case "LOW":
                    return "Low-Risk";
                case "MEDIUM":
                    return "Medium-Risk";
                case "HIGH":
                    return "High-Risk";
                default:
                    return riskName;
            }
        }
    }
}