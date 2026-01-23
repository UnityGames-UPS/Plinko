using Best.SocketIO;
using PlinkoGame.Data;
using PlinkoGame.Network;
using PlinkoGame.Services;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlinkoGame
{
    /// <summary>
    /// Main game controller
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private SocketIOManager socketManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private BoardController boardController;
        [SerializeField] private BallLauncher ballLauncher;


        [Header("Settings")]
        [SerializeField] private int maxParallelBalls = 10;
        [SerializeField] private float autoplayDelay = 1.5f;

        [Header("Blocker")]
        [SerializeField] private GameObject raycastBlocker;

        private MultiplierService multiplierService;
        private int currentBetIndex;
        private int currentRiskIndex = 0;
        private string currentRiskName = "LOW";
        private int currentRowIndex;
        private int activeBallCount;
        private bool isAutoplayActive;
        private int autoplayRoundsRemaining;
        private bool isInfiniteAutoplay;
        private Dictionary<string, MultiplierMapping> currentMappings = new Dictionary<string, MultiplierMapping>();
        private Coroutine autoplayCoroutine;
        private bool isProcessingBet;
        private bool isWaitingForResult;

        // Store pending result data until ball lands
        private class PendingResult
        {
            public double winAmount;
            public double multiplier;
            public double newBalance;
            public int targetCatcherIndex;
        }
        private Queue<PendingResult> pendingResults = new Queue<PendingResult>();

        private void Awake()
        {
            multiplierService = new MultiplierService();
        }

        internal void OnInitDataReceived()
        {
            if (socketManager.InitialData == null ||
                socketManager.InitialData.risks == null ||
                socketManager.InitialData.risks.Count == 0 ||
                socketManager.InitialData.rows == null ||
                socketManager.InitialData.rows.Count == 0)
            {
                uiManager.ShowErrorPopup("Invalid game data received from server");
                return;
            }

            currentBetIndex = 0;
            currentRiskIndex = 0;
            currentRiskName = socketManager.InitialData.risks[0].name;
            currentRowIndex = 0;

            GenerateAllMultiplierMappings();

            int defaultRows = GetRowCountFromIndex(currentRowIndex);
            boardController.SetRows(defaultRows);

            uiManager.InitializeFromGameData(
                socketManager.InitialData,
                socketManager.PlayerData.balance
            );

            UpdateCatcherMultipliers(defaultRows, currentRiskName);
            ballLauncher.UpdateBallSprites(currentRiskName);
        }

        internal void OnDataRefreshed()
        {
            // Only update balance if no pending results
            if (pendingResults.Count == 0)
            {
                uiManager.UpdateBalance(socketManager.PlayerData.balance);
            }
            GenerateAllMultiplierMappings();
        }

        internal void OnResultReceived()
        {
            PlinkoResultPayload result = socketManager.ResultData;
            double newBalance = socketManager.PlayerData.balance;

            int targetCatcherIndex = ResolveTargetCatcher(result);
            Debug.Log($"[TARGET] Catcher {targetCatcherIndex}");

            // Store result data to be shown when ball lands
            PendingResult pendingResult = new PendingResult
            {
                winAmount = result.winAmount,
                multiplier = result.multiplier,
                newBalance = newBalance,
                targetCatcherIndex = targetCatcherIndex
            };
            pendingResults.Enqueue(pendingResult);

            // Launch the ball
            ballLauncher.DropBallToTarget(targetCatcherIndex);

            activeBallCount++;
            UpdateBetButtonState();

            socketManager.ConsumeResult();
            isProcessingBet = false;
            isWaitingForResult = false;

            // Handle autoplay countdown
            if (isAutoplayActive)
            {
                if (!isInfiniteAutoplay)
                {
                    autoplayRoundsRemaining--;

                    // Update display to show remaining rounds (including 0 on last round)
                    uiManager.UpdateAutoplayRounds(autoplayRoundsRemaining);

                    // Stop autoplay only AFTER showing 0
                    if (autoplayRoundsRemaining <= 0)
                    {
                        StopAutoplay();
                    }
                }
                // If infinite autoplay, don't update counter (keeps showing infinity)
            }
        }

        internal void OnBetButtonClicked()
        {
            if (activeBallCount >= maxParallelBalls || isProcessingBet || isWaitingForResult)
            {
                return;
            }

            if (!CanPlaceBet())
            {
                uiManager.ShowErrorPopup("Insufficient balance");
                return;
            }

            PlaceBet();
        }

        private void PlaceBet()
        {
            if (isProcessingBet || isWaitingForResult) return;

            double betAmount = socketManager.InitialData.bets[currentBetIndex];
            double currentBalance = socketManager.PlayerData.balance;
            double newBalance = currentBalance - betAmount;

            // Update balance immediately when bet is placed (deduct bet amount)
            uiManager.UpdateBalance(newBalance);

            socketManager.SendBetRequest(currentBetIndex, currentRiskIndex, currentRowIndex);

            isProcessingBet = true;
            isWaitingForResult = true;
            UpdateBetButtonState();
        }

        private bool CanPlaceBet()
        {
            double betAmount = socketManager.InitialData.bets[currentBetIndex];
            return socketManager.PlayerData.balance >= betAmount;
        }

        internal void StartAutoplay(int rounds)
        {
            if (isAutoplayActive) return;

            isAutoplayActive = true;
            isInfiniteAutoplay = (rounds == 0);
            autoplayRoundsRemaining = rounds;

            // Notify UI that autoplay started
            uiManager.OnAutoplayStarted(isInfiniteAutoplay);

            // Update counter display only for finite autoplay
            if (!isInfiniteAutoplay)
            {
                uiManager.UpdateAutoplayRounds(autoplayRoundsRemaining);
            }
            // For infinite (rounds == 0), OnAutoplayStarted already shows infinity icon

            autoplayCoroutine = StartCoroutine(AutoplayLoop());
        }

        internal void StopAutoplay()
        {
            isAutoplayActive = false;
            isInfiniteAutoplay = false;
            autoplayRoundsRemaining = 0;

            if (autoplayCoroutine != null)
            {
                StopCoroutine(autoplayCoroutine);
                autoplayCoroutine = null;
            }

            uiManager.OnAutoplayStopped();
        }

        private IEnumerator AutoplayLoop()
        {
            while (isAutoplayActive)
            {
                // Wait until we can place a bet
                while (isProcessingBet || isWaitingForResult || activeBallCount >= maxParallelBalls)
                {
                    yield return null;
                }

                if (!CanPlaceBet())
                {
                    uiManager.ShowErrorPopup("Insufficient balance - autoplay stopped");
                    StopAutoplay();
                    yield break;
                }

                if (!isInfiniteAutoplay && autoplayRoundsRemaining <= 0)
                {
                    StopAutoplay();
                    yield break;
                }

                PlaceBet();

                // Wait for the full delay before next bet
                float elapsedTime = 0f;
                while (elapsedTime < autoplayDelay)
                {
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }
        }

        internal void OnBetChanged(bool increase)
        {
            if (increase)
            {
                currentBetIndex = Mathf.Min(currentBetIndex + 1, socketManager.InitialData.bets.Count - 1);
            }
            else
            {
                currentBetIndex = Mathf.Max(currentBetIndex - 1, 0);
            }

            double newBet = socketManager.InitialData.bets[currentBetIndex];
            uiManager.UpdateBetDisplay(newBet);
            uiManager.RefreshHoverData(GetCurrentBetAmount());
        }

        internal void OnRiskChanged(int riskIndex)
        {
            currentRiskIndex = riskIndex;
            currentRiskName = socketManager.InitialData.risks[riskIndex].name;

            int currentRows = GetRowCountFromIndex(currentRowIndex);
            UpdateCatcherMultipliers(currentRows, currentRiskName);

            ballLauncher.UpdateBallSprites(currentRiskName);

            uiManager.RefreshHoverData(GetCurrentBetAmount());
        }

        internal void OnRowChanged(int rowIndex)
        {
            currentRowIndex = rowIndex;
            int rowCount = GetRowCountFromIndex(rowIndex);

            boardController.SetRows(rowCount);
            UpdateCatcherMultipliers(rowCount, currentRiskName);

            uiManager.RefreshHoverData(GetCurrentBetAmount());
        }

        internal void OnBallLanded(int catcherIndex)
        {
            activeBallCount--;
            UpdateBetButtonState();

            // Process pending result when ball lands
            if (pendingResults.Count > 0)
            {
                PendingResult result = pendingResults.Dequeue();

                // Update balance with actual win amount
                uiManager.UpdateBalance(result.newBalance);

                // Show win popup if won
                if (result.multiplier > 1.0)
                {
                    uiManager.ShowWinPopup(result.winAmount, result.multiplier);
                }

                // Add to history - NOW INCLUDES CATCHER INDEX
                uiManager.AddToHistory(result.multiplier, result.winAmount, catcherIndex);
            }
        }

        private void GenerateAllMultiplierMappings()
        {
            currentMappings.Clear();

            foreach (PlinkoRow row in socketManager.InitialData.rows)
            {
                int rowCount = int.Parse(row.id);

                for (int i = 0; i < row.risks.Count; i++)
                {
                    if (i >= socketManager.InitialData.risks.Count) continue;

                    string risk = socketManager.InitialData.risks[i].name;

                    if (row.risks[i].multipliers == null || row.risks[i].multipliers.Count == 0)
                        continue;

                    List<double> backendMultipliers = row.risks[i].multipliers;

                    MultiplierMapping mapping = multiplierService.GenerateMapping(
                        rowCount,
                        risk,
                        backendMultipliers
                    );

                    string key = $"{rowCount}_{risk}";
                    currentMappings[key] = mapping;
                }
            }
        }

        private void UpdateCatcherMultipliers(int rowCount, string risk)
        {
            string key = $"{rowCount}_{risk}";

            if (currentMappings.ContainsKey(key))
            {
                MultiplierMapping mapping = currentMappings[key];
                boardController.UpdateCatcherMultipliers(mapping.fullMultipliers);
            }
        }

        private int ResolveTargetCatcher(PlinkoResultPayload result)
        {
            int rowCount = GetRowCountFromIndex(result.selectedRowIndex);
            string riskName = GetRiskNameFromIndex(result.selectedRisk);
            string key = $"{rowCount}_{riskName}";

            if (!currentMappings.ContainsKey(key))
            {
                return 0;
            }

            MultiplierMapping mapping = currentMappings[key];
            List<int> possibleCatchers = multiplierService.ResolveCatcherIndices(
                mapping,
                result.generatedMultiplerIndex
            );

            int targetCatcher = multiplierService.ChooseRandomCatcher(possibleCatchers);

            return targetCatcher;
        }

        private string GetRiskNameFromIndex(int riskIndex)
        {
            if (socketManager.InitialData != null &&
                riskIndex >= 0 &&
                riskIndex < socketManager.InitialData.risks.Count)
            {
                return socketManager.InitialData.risks[riskIndex].name;
            }

            return "LOW";
        }

        private int GetRowCountFromIndex(int rowIndex)
        {
            if (socketManager.InitialData != null && rowIndex < socketManager.InitialData.rows.Count)
            {
                return int.Parse(socketManager.InitialData.rows[rowIndex].id);
            }
            return 8;
        }

        internal double GetCurrentBetAmount()
        {
            if (socketManager.InitialData != null && currentBetIndex < socketManager.InitialData.bets.Count)
            {
                return socketManager.InitialData.bets[currentBetIndex];
            }
            return 0;
        }

        internal double GetMultiplierForCatcher(int catcherIndex)
        {
            int rowCount = GetRowCountFromIndex(currentRowIndex);
            string key = $"{rowCount}_{currentRiskName}";

            if (currentMappings.ContainsKey(key))
            {
                MultiplierMapping mapping = currentMappings[key];
                if (catcherIndex >= 0 && catcherIndex < mapping.fullMultipliers.Count)
                {
                    return mapping.fullMultipliers[catcherIndex];
                }
            }

            return 1.0;
        }

        internal double GetProbabilityForCatcher(int catcherIndex)
        {
            int rowCount = GetRowCountFromIndex(currentRowIndex);
            PlinkoRow row = socketManager.InitialData.rows[currentRowIndex];

            if (currentRiskIndex >= row.risks.Count)
            {
                return 0;
            }

            PlinkoRisk riskData = row.risks[currentRiskIndex];

            string key = $"{rowCount}_{currentRiskName}";
            if (currentMappings.ContainsKey(key))
            {
                MultiplierMapping mapping = currentMappings[key];
                if (catcherIndex >= 0 && catcherIndex < mapping.backendIndices.Count)
                {
                    int backendIndex = mapping.backendIndices[catcherIndex];
                    if (backendIndex >= 0 && backendIndex < riskData.probability.Count)
                    {
                        return riskData.probability[backendIndex];
                    }
                }
            }

            return 0;
        }

        private void UpdateBetButtonState()
        {
            bool canBet = (activeBallCount < maxParallelBalls) && CanPlaceBet() && !isProcessingBet && !isWaitingForResult;
            uiManager.UpdateBetButtonState(canBet);
        }

        internal void OnExitGame()
        {
            if (raycastBlocker != null)
            {
                raycastBlocker.SetActive(true);
            }

            multiplierService.ClearCache();
            currentMappings.Clear();
            pendingResults.Clear();

            StartCoroutine(socketManager.CloseSocket());
        }

        private void OnDestroy()
        {
            if (autoplayCoroutine != null)
            {
                StopCoroutine(autoplayCoroutine);
            }
        }
    }
}