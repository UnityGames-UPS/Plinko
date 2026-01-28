using Best.SocketIO;
using PlinkoGame.Data;
using PlinkoGame.Network;
using PlinkoGame.Services;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace PlinkoGame
{
    /// <summary>
    /// Main game controller
    /// NOW SUPPORTS DUAL LAYOUT SYSTEM
    /// References active BoardController and BallLauncher based on orientation
    /// FIXED: Only shows popup on insufficient balance, overlay only active during autoplay
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private SocketIOManager socketManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private InfoPanelManager infoPanelManager;

        [Header("Horizontal Layout")]
        [SerializeField] private BoardController horizontalBoardController;
        [SerializeField] private BallLauncher horizontalBallLauncher;

        [Header("Vertical Layout")]
        [SerializeField] private BoardController verticalBoardController;
        [SerializeField] private BallLauncher verticalBallLauncher;

        [Header("Settings")]
        [SerializeField] private int maxParallelBalls = 10;
        [SerializeField] private float autoplayDelay = 1.5f;

        [Header("Blocker")]
        [SerializeField] private GameObject raycastBlocker;

        // Active references (updated on orientation change)
        private BoardController activeBoardController;
        private BallLauncher activeBallLauncher;

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

            // Start with horizontal layout as default
            activeBoardController = horizontalBoardController;
            activeBallLauncher = horizontalBallLauncher;
        }

        /// <summary>
        /// Called by UIManager when layout switches
        /// Updates active board and ball launcher references
        /// </summary>
        internal void OnLayoutSwitched(bool isHorizontal)
        {
            activeBoardController = isHorizontal ? horizontalBoardController : verticalBoardController;
            activeBallLauncher = isHorizontal ? horizontalBallLauncher : verticalBallLauncher;

            // Update the active board with current settings
            if (activeBoardController != null)
            {
                int currentRows = GetRowCountFromIndex(currentRowIndex);
                activeBoardController.SetRows(currentRows);
                UpdateCatcherMultipliers(currentRows, currentRiskName);
            }

            // Update ball launcher sprites
            if (activeBallLauncher != null)
            {
                activeBallLauncher.UpdateBallSprites(currentRiskName);
            }

            Debug.Log($"[GameManager] Layout switched to {(isHorizontal ? "HORIZONTAL" : "VERTICAL")}");
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

            // Initialize BOTH boards (only active one will be visible)
            if (horizontalBoardController != null)
                horizontalBoardController.SetRows(defaultRows);
            if (verticalBoardController != null)
                verticalBoardController.SetRows(defaultRows);

            uiManager.InitializeFromGameData(
                socketManager.InitialData,
                socketManager.PlayerData.balance
            );

            // Generate info panel text
            if (infoPanelManager != null)
            {
                infoPanelManager.GenerateInfoText(socketManager.InitialData);
            }

            UpdateCatcherMultipliers(defaultRows, currentRiskName);

            // Update sprites for both launchers
            if (horizontalBallLauncher != null)
                horizontalBallLauncher.UpdateBallSprites(currentRiskName);
            if (verticalBallLauncher != null)
                verticalBallLauncher.UpdateBallSprites(currentRiskName);
        }

        internal void OnDataRefreshed()
        {
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

            PendingResult pendingResult = new PendingResult
            {
                winAmount = result.winAmount,
                multiplier = result.multiplier,
                newBalance = newBalance,
                targetCatcherIndex = targetCatcherIndex
            };
            pendingResults.Enqueue(pendingResult);

            // Drop ball using ACTIVE ball launcher
            if (activeBallLauncher != null)
            {
                activeBallLauncher.DropBallToTarget(targetCatcherIndex);
            }

            activeBallCount++;

            isProcessingBet = false;
            isWaitingForResult = false;

            socketManager.ConsumeResult();

            UpdateBetButtonState();
            UpdateRiskRowOverlays();

            if (isAutoplayActive)
            {
                if (!isInfiniteAutoplay)
                {
                    autoplayRoundsRemaining--;
                    uiManager.UpdateAutoplayRounds(autoplayRoundsRemaining);

                    if (autoplayRoundsRemaining <= 0)
                    {
                        StopAutoplay();
                    }
                }
            }
        }

        /// <summary>
        /// FIXED: Only shows popup when balance insufficient, never activates overlay in manual mode
        /// </summary>
        internal void OnBetButtonClicked()
        {
            // Check ball count and processing state
            if (activeBallCount >= maxParallelBalls || isProcessingBet || isWaitingForResult)
            {
                return;
            }

            // Check balance before placing bet - only show popup, don't disable controls
            if (!CanPlaceBet())
            {
                uiManager.ShowErrorPopup("Insufficient balance");
                return;
            }

            PlaceBet();
        }

        /// <summary>
        /// FIXED: Check balance before placing bet but don't disable controls
        /// </summary>
        private void PlaceBet()
        {
            if (isProcessingBet || isWaitingForResult) return;

            // Double-check balance right before placing bet
            if (!CanPlaceBet())
            {
                uiManager.ShowErrorPopup("Insufficient balance");
                return;
            }

            double betAmount = socketManager.InitialData.bets[currentBetIndex];
            double currentBalance = socketManager.PlayerData.balance;
            double newBalance = currentBalance - betAmount;

            uiManager.UpdateBalance(newBalance);

            socketManager.SendBetRequest(currentBetIndex, currentRiskIndex, currentRowIndex);

            isProcessingBet = true;
            isWaitingForResult = true;
            UpdateBetButtonState();
        }

        private bool CanPlaceBet()
        {
            if (socketManager == null || socketManager.InitialData == null || socketManager.PlayerData == null)
            {
                return false;
            }

            if (currentBetIndex < 0 || currentBetIndex >= socketManager.InitialData.bets.Count)
            {
                return false;
            }

            double betAmount = socketManager.InitialData.bets[currentBetIndex];
            return socketManager.PlayerData.balance >= betAmount;
        }

        internal bool CanBetNow()
        {
            return (activeBallCount < maxParallelBalls) &&
                   CanPlaceBet() &&
                   !isProcessingBet &&
                   !isWaitingForResult;
        }

        /// <summary>
        /// FIXED: Check balance before starting autoplay and show popup if insufficient
        /// </summary>
        internal void StartAutoplay(int rounds)
        {
            if (isAutoplayActive) return;

            // Check balance before starting autoplay - show popup if insufficient
            if (!CanPlaceBet())
            {
                uiManager.ShowErrorPopup("Insufficient balance to start autoplay");
                return;
            }

            isAutoplayActive = true;
            isInfiniteAutoplay = (rounds == 0);
            autoplayRoundsRemaining = rounds;

            uiManager.OnAutoplayStarted(isInfiniteAutoplay);

            if (!isInfiniteAutoplay)
            {
                uiManager.UpdateAutoplayRounds(autoplayRoundsRemaining);
            }

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

        /// <summary>
        /// FIXED: Check balance at every autoplay round start, show popup and stop if insufficient
        /// </summary>
        private IEnumerator AutoplayLoop()
        {
            while (isAutoplayActive)
            {
                // Wait for current bets to complete
                while (isProcessingBet || isWaitingForResult || activeBallCount >= maxParallelBalls)
                {
                    yield return null;
                }

                // Check balance before each autoplay bet
                if (!CanPlaceBet())
                {
                    // Show popup and stop autoplay
                    uiManager.ShowErrorPopup("Insufficient balance - autoplay stopped");
                    StopAutoplay();
                    yield break;
                }

                PlaceBet();

                yield return new WaitForSeconds(autoplayDelay);

                // Wait for balls to land before next bet
                while (activeBallCount >= maxParallelBalls)
                {
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

            UpdateBetButtonState();
        }

        internal void OnRiskChanged(int riskIndex)
        {
            currentRiskIndex = riskIndex;
            currentRiskName = socketManager.InitialData.risks[riskIndex].name;

            int currentRows = GetRowCountFromIndex(currentRowIndex);
            UpdateCatcherMultipliers(currentRows, currentRiskName);

            // Update BOTH ball launchers (only active one visible)
            if (horizontalBallLauncher != null)
                horizontalBallLauncher.UpdateBallSprites(currentRiskName);
            if (verticalBallLauncher != null)
                verticalBallLauncher.UpdateBallSprites(currentRiskName);

            uiManager.RefreshHoverData(GetCurrentBetAmount());
        }

        internal void OnRowChanged(int rowIndex)
        {
            currentRowIndex = rowIndex;
            int rowCount = GetRowCountFromIndex(rowIndex);

            // Update ACTIVE board controller only
            if (activeBoardController != null)
            {
                activeBoardController.SetRows(rowCount);
            }

            UpdateCatcherMultipliers(rowCount, currentRiskName);

            uiManager.RefreshHoverData(GetCurrentBetAmount());
        }

        internal void OnBallLanded(int catcherIndex)
        {
            activeBallCount--;
            UpdateBetButtonState();
            UpdateRiskRowOverlays();

            if (pendingResults.Count > 0)
            {
                PendingResult result = pendingResults.Dequeue();

                uiManager.UpdateBalance(result.newBalance);

                if (result.multiplier > 1.0)
                {
                    uiManager.ShowWinPopup(result.winAmount, result.multiplier);
                }

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

                // Update BOTH boards (only active one visible)
                if (horizontalBoardController != null)
                    horizontalBoardController.UpdateCatcherMultipliers(mapping.fullMultipliers);
                if (verticalBoardController != null)
                    verticalBoardController.UpdateCatcherMultipliers(mapping.fullMultipliers);
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

        /// <summary>
        /// FIXED: Only disable bet button when balls are at max or bet is processing
        /// Never disable based on balance check (that only shows popup)
        /// </summary>
        private void UpdateBetButtonState()
        {
            bool canBet = (activeBallCount < maxParallelBalls) && !isProcessingBet && !isWaitingForResult;
            uiManager.UpdateBetButtonState(canBet, isAutoplayActive);
        }

        /// <summary>
        /// Updates risk and row overlays based on active ball count
        /// Prevents players from changing settings while balls are dropping
        /// </summary>
        private void UpdateRiskRowOverlays()
        {
            bool hasBallsActive = activeBallCount > 0;
            uiManager.UpdateRiskRowOverlays(hasBallsActive);
        }

        internal void OnExitGame()
        {
            if (raycastBlocker != null)
            {
                raycastBlocker.SetActive(true);
            }

            DOTween.KillAll();

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

            DOTween.KillAll();
        }
    }
}