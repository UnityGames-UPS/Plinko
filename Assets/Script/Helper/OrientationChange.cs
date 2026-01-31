using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace PlinkoGame
{
    /// <summary>
    /// Enhanced orientation handler with SMOOTH ball state mirroring
    /// FIXED: Proper event handler cleanup to prevent duplicate ball results
    /// - Calculates ball row position in old layout
    /// - Mirrors to same/nearby row in new layout
    /// - PROPERLY CLEANS UP old layout (stops/deactivates all balls)
    /// - FIXES: Event handler duplication bug
    /// - Prevents ghost balls from re-appearing on layout switch
    /// </summary>
    public class OrientationChange : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private CanvasScaler canvasScaler;

        [Header("Horizontal Layout")]
        [SerializeField] private BoardController horizontalBoardController;
        [SerializeField] private BallLauncher horizontalBallLauncher;

        [Header("Vertical Layout")]
        [SerializeField] private BoardController verticalBoardController;
        [SerializeField] private BallLauncher verticalBallLauncher;

        [Header("Animation")]
        [SerializeField] private float transitionDuration = 0.3f;
        [SerializeField] private float debounceDelay = 0.5f;
        [SerializeField] private float ballTransitionDelay = 0.1f;

        [Header("Ball Mirroring")]
        [SerializeField] private int maxRowVariance = 1; // ±1 row variance allowed

        [Header("Layout Logic")]
        [SerializeField] private bool swapLayoutLogic = false;

        [Header("Portrait Match Values - Common Resolutions")]
        [SerializeField] private float match_portrait_9_16 = 0.38f;
        [SerializeField] private float match_portrait_1080_1920 = 0.42f;
        [SerializeField] private float match_portrait_1080_2340 = 0.46f;
        [SerializeField] private float match_portrait_1080_2400 = 0.48f;
        [SerializeField] private float match_portrait_1440_2960 = 0.46f;
        [SerializeField] private float match_portrait_1440_3200 = 0.50f;

        [Header("Portrait Match Values - iPad/Tablets")]
        [SerializeField] private float match_portrait_ipad = 0.22f;
        [SerializeField] private float match_portrait_ipad_pro = 0.25f;

        [Header("Portrait Match Values - Aspect Ratio Fallbacks")]
        [SerializeField] private float match_portrait_4_3 = 0.22f;
        [SerializeField] private float match_portrait_3_2 = 0.28f;
        [SerializeField] private float match_portrait_16_10 = 0.32f;
        [SerializeField] private float match_portrait_16_9 = 0.38f;
        [SerializeField] private float match_portrait_18_9 = 0.42f;
        [SerializeField] private float match_portrait_19_9 = 0.46f;
        [SerializeField] private float match_portrait_21_9 = 0.50f;
        [SerializeField] private float match_portrait_ultra = 0.55f;

        [Header("Landscape Match Values")]
        [SerializeField] private float match_landscape_tablet = 0.75f;
        [SerializeField] private float match_landscape_standard = 0.85f;
        [SerializeField] private float match_landscape_wide = 1.0f;

        // Enhanced ball state capture structure
        private class BallState
        {
            public string targetCatcherName;
            public int targetCatcherIndex;
            public int estimatedCurrentRow;
            public Vector2 worldPosition;
            public GameObject ballObject;

            // ANTI-CHEAT: Store the row/risk settings this ball was dropped with
            public int rowCountWhenDropped;
            public string riskLevelWhenDropped;
        }

        private Vector2 referenceAspect;
        private Tween matchTween;
        private Coroutine orientationRoutine;
        private bool isLandscape;
        private string currentDevice = "MB";

        private BoardController activeBoard;
        private BallLauncher activeLauncher;

        private float lastOrientationChangeTime = -999f;
        private string pendingDimensions = "";
        private bool isTransitioning = false;

        private int lastWidth = 0;
        private int lastHeight = 0;

        // Captured ball states
        private List<BallState> capturedBallStates = new List<BallState>();

        private void Awake()
        {
            if (canvasScaler != null)
            {
                referenceAspect = canvasScaler.referenceResolution;
            }

            activeBoard = horizontalBoardController;
            activeLauncher = horizontalBallLauncher;
        }

        void DeviceCheck(string device)
        {
            Debug.Log($"[OrientationChange] Device detected: {device}");
            currentDevice = device;
            ApplyDeviceSpecificSettings();
        }

        void SwitchDisplay(string dimensions)
        {
            string[] parts = dimensions.Split(',');
            if (parts.Length != 2) return;

            if (!int.TryParse(parts[0], out int width) || !int.TryParse(parts[1], out int height))
                return;

            if (width == lastWidth && height == lastHeight)
            {
                Debug.Log("[OrientationChange] Ignoring duplicate dimension event");
                return;
            }

            float timeSinceLastChange = Time.time - lastOrientationChangeTime;
            if (timeSinceLastChange < debounceDelay && isTransitioning)
            {
                Debug.Log($"[OrientationChange] Debouncing ({timeSinceLastChange:F2}s)");
                pendingDimensions = dimensions;
                return;
            }

            pendingDimensions = "";
            lastOrientationChangeTime = Time.time;

            Debug.Log($"[OrientationChange] ========== NEW ORIENTATION: {width}x{height} ==========");

            if (orientationRoutine != null)
            {
                StopCoroutine(orientationRoutine);
            }

            orientationRoutine = StartCoroutine(HandleOrientationChange(width, height));
        }

        private IEnumerator HandleOrientationChange(int width, int height)
        {
            isTransitioning = true;

            // ANTI-CHEAT: Lock settings immediately
            if (gameManager != null)
            {
                gameManager.LockSettingsDuringOrientationChange(true);
            }

            bool isWidthGreater = width > height;
            bool newIsLandscape = swapLayoutLogic ? !isWidthGreater : isWidthGreater;

            float aspectRatio;
            if (newIsLandscape)
            {
                aspectRatio = (float)width / height;
            }
            else
            {
                aspectRatio = (float)height / width;
            }

            Debug.Log($"[OrientationChange] ========================================");
            Debug.Log($"[OrientationChange] INPUT: {width}x{height}");
            Debug.Log($"[OrientationChange] Width > Height: {isWidthGreater}");
            Debug.Log($"[OrientationChange] Swap Logic: {swapLayoutLogic}");
            Debug.Log($"[OrientationChange] Final IsLandscape: {newIsLandscape}");
            Debug.Log($"[OrientationChange] Aspect Ratio: {aspectRatio:F3}");
            Debug.Log($"[OrientationChange] Device: {currentDevice}");
            Debug.Log($"[OrientationChange] ========================================");

            BoardController nextBoard = newIsLandscape ? horizontalBoardController : verticalBoardController;
            BallLauncher nextLauncher = newIsLandscape ? horizontalBallLauncher : verticalBallLauncher;

            // === STEP 1: CAPTURE BALL STATES (with row position calculation) ===
            CaptureBallStatesWithRowInfo(activeBoard, activeLauncher);

            // === STEP 1.5: CAPTURE CURRENT SETTINGS BEFORE CLEANUP ===
            int capturedRowCount = 8; // Default fallback
            string capturedRiskLevel = "LOW"; // Default fallback

            if (capturedBallStates.Count > 0)
            {
                // Use settings from captured balls (they should all match)
                capturedRowCount = capturedBallStates[0].rowCountWhenDropped;
                capturedRiskLevel = capturedBallStates[0].riskLevelWhenDropped;
                Debug.Log($"[OrientationChange] Using captured settings from balls: {capturedRowCount} rows, {capturedRiskLevel} risk");
            }
            else if (gameManager != null)
            {
                // No balls in flight - use current GameManager settings
                capturedRowCount = gameManager.GetCurrentRowCount();
                capturedRiskLevel = gameManager.GetCurrentRiskLevel();
                Debug.Log($"[OrientationChange] Using current GameManager settings: {capturedRowCount} rows, {capturedRiskLevel} risk");
            }

            // === STEP 2: AGGRESSIVELY CLEANUP OLD LAYOUT ===
            yield return StartCoroutine(AggressiveCleanup(activeBoard, activeLauncher, nextBoard, nextLauncher));

            // === STEP 3: ANIMATE CANVAS TRANSITION ===
            float targetMatch = CalculateMatchValue(width, height, aspectRatio, newIsLandscape);
            Debug.Log($"[OrientationChange] >>> MATCH VALUE SELECTED: {targetMatch:F3} <<<");

            if (canvasScaler != null)
            {
                if (matchTween != null && matchTween.IsActive())
                {
                    matchTween.Kill();
                }

                matchTween = DOTween.To(
                    () => canvasScaler.matchWidthOrHeight,
                    x => canvasScaler.matchWidthOrHeight = x,
                    targetMatch,
                    transitionDuration
                ).SetEase(Ease.InOutQuad);

                yield return matchTween.WaitForCompletion();
            }

            // === STEP 4: NOTIFY UI MANAGER ===
            if (uiManager != null)
            {
                int sendWidth = swapLayoutLogic ? height : width;
                int sendHeight = swapLayoutLogic ? width : height;

                uiManager.OnOrientationChanged(sendWidth, sendHeight);
            }

            // === STEP 5: SWITCH ACTIVE REFERENCES ===
            activeBoard = nextBoard;
            activeLauncher = nextLauncher;

            // === STEP 6: ENFORCE SETTINGS ON NEW BOARD BEFORE REBUILD ===
            if (nextBoard != null)
            {
                Debug.Log($"[OrientationChange] >>> ENFORCING {capturedRowCount} rows on new board BEFORE rebuild <<<");
                nextBoard.SetRows(capturedRowCount);
            }

            if (gameManager != null)
            {
                Debug.Log($"[OrientationChange] >>> ENFORCING {capturedRowCount} rows + {capturedRiskLevel} risk in GameManager <<<");
                gameManager.ForceRowAndRiskSettings(capturedRowCount, capturedRiskLevel);
            }

            yield return new WaitForEndOfFrame();

            // === STEP 7: REBUILD NEW LAYOUT WITH CORRECT SETTINGS ===
            yield return new WaitForEndOfFrame();
            ForceCanvasUpdate();
            yield return new WaitForEndOfFrame();

            if (activeBoard != null)
            {
                Debug.Log($"[OrientationChange] Building FRESH pyramid on {(newIsLandscape ? "HORIZONTAL" : "VERTICAL")} board with {capturedRowCount} rows...");
                activeBoard.StartCompleteFreshRebuild();
            }

            yield return new WaitForEndOfFrame();
            ForceCanvasUpdate();

            // === STEP 8: NOTIFY GAME MANAGER ===
            if (gameManager != null)
            {
                gameManager.OnLayoutSwitched(newIsLandscape);
            }

            if (activeLauncher != null)
            {
                activeLauncher.OnBoardRebuilt();
            }

            yield return new WaitForEndOfFrame();

            // === STEP 8.5: SYNC AUTOPLAY STATE BETWEEN LAYOUTS ===
            if (uiManager != null)
            {
                Debug.Log("[OrientationChange] Syncing autoplay state after layout switch");
                uiManager.SyncAutoplayStateAfterOrientationChange();
            }

            yield return new WaitForEndOfFrame();

            // === STEP 9: RESTORE BALL STATES (with smart row mirroring) ===
            yield return StartCoroutine(RestoreBallStatesWithRowMirroring(activeBoard, activeLauncher));

            // === STEP 10: FINAL UPDATES ===
            lastWidth = width;
            lastHeight = height;
            isLandscape = newIsLandscape;

            Debug.Log($"[OrientationChange] ========== ORIENTATION CHANGE COMPLETE ==========");
            Debug.Log($"[OrientationChange] Active Board: {(newIsLandscape ? "HORIZONTAL" : "VERTICAL")}");
            Debug.Log($"[OrientationChange] Balls Restored: {capturedBallStates.Count}");

            isTransitioning = false;

            // === STEP 10.5: WAIT BEFORE UNLOCKING CONTROLS ===
            Debug.Log("[OrientationChange] Waiting before unlocking controls...");
            yield return new WaitForSeconds(0.8f);

            // ANTI-CHEAT: Unlock settings after transition completes + delay
            if (gameManager != null)
            {
                gameManager.LockSettingsDuringOrientationChange(false);
                Debug.Log("[OrientationChange] Controls unlocked after delay");
            }

            // Handle pending orientation change
            if (!string.IsNullOrEmpty(pendingDimensions))
            {
                string pending = pendingDimensions;
                pendingDimensions = "";
                yield return new WaitForSeconds(0.1f);
                SwitchDisplay(pending);
            }
        }

        /// <summary>
        /// IMPROVED: Captures ball states WITH row position calculation
        /// ANTI-CHEAT: Also stores row/risk settings to prevent multiplier loophole
        /// Determines which row each ball is currently near/passing
        /// </summary>
        private void CaptureBallStatesWithRowInfo(BoardController currentBoard, BallLauncher currentLauncher)
        {
            capturedBallStates.Clear();

            if (currentLauncher == null || currentBoard == null)
            {
                Debug.Log("[OrientationChange] Missing launcher or board");
                return;
            }

            // Get current game settings from GameManager
            int currentRowCount = 8;
            string currentRiskLevel = "LOW";

            if (gameManager != null)
            {
                currentRowCount = currentBoard.GetCurrentRows();
                currentRiskLevel = gameManager.GetCurrentRiskLevel();
            }

            // Get peg rows for position calculation
            List<List<Vector2>> pegRows = GetPegRowsFromBoard(currentBoard);
            if (pegRows == null || pegRows.Count == 0)
            {
                Debug.LogWarning("[OrientationChange] No peg rows found");
                return;
            }

            List<GameObject> ballPool = GetBallPoolFromLauncher(currentLauncher);
            if (ballPool == null || ballPool.Count == 0)
            {
                Debug.Log("[OrientationChange] No ball pool found");
                return;
            }

            int capturedCount = 0;

            foreach (GameObject ball in ballPool)
            {
                if (ball == null || !ball.activeSelf) continue;

                BallController controller = ball.GetComponent<BallController>();
                if (controller == null) continue;

                string targetCatcherName = controller.GetTargetCatcherName();
                if (string.IsNullOrEmpty(targetCatcherName)) continue;

                int catcherIndex = ParseCatcherIndex(targetCatcherName);
                if (catcherIndex < 0) continue;

                // Calculate which row the ball is currently near
                Vector2 ballWorldPos = ball.transform.position;
                Vector2 ballLocalPos = currentBoard.transform.InverseTransformPoint(ballWorldPos);
                int estimatedRow = CalculateCurrentRow(ballLocalPos, pegRows);

                BallState state = new BallState
                {
                    targetCatcherName = targetCatcherName,
                    targetCatcherIndex = catcherIndex,
                    estimatedCurrentRow = estimatedRow,
                    worldPosition = ballWorldPos,
                    ballObject = ball,

                    // ANTI-CHEAT: Store settings this ball was dropped with
                    rowCountWhenDropped = currentRowCount,
                    riskLevelWhenDropped = currentRiskLevel
                };

                capturedBallStates.Add(state);
                capturedCount++;

                Debug.Log($"[OrientationChange] Captured ball: Catcher={catcherIndex}, Row={estimatedRow}/{pegRows.Count}, Settings={currentRowCount}rows+{currentRiskLevel}");
            }

            Debug.Log($"[OrientationChange] Total balls captured: {capturedCount}");
        }

        /// <summary>
        /// Calculates which row (0-based) the ball is currently near/passing
        /// </summary>
        private int CalculateCurrentRow(Vector2 ballLocalPos, List<List<Vector2>> pegRows)
        {
            if (pegRows.Count == 0) return 0;

            for (int i = 0; i < pegRows.Count; i++)
            {
                if (pegRows[i].Count == 0) continue;

                float rowY = pegRows[i][0].y;

                if (ballLocalPos.y >= rowY)
                {
                    return i;
                }
            }

            return pegRows.Count;
        }

        /// <summary>
        /// IMPROVED: Restores balls with smart row mirroring
        /// </summary>
        private IEnumerator RestoreBallStatesWithRowMirroring(BoardController newBoard, BallLauncher newLauncher)
        {
            if (capturedBallStates.Count == 0)
            {
                Debug.Log("[OrientationChange] No balls to restore");
                yield break;
            }

            Debug.Log($"[OrientationChange] Restoring {capturedBallStates.Count} balls with row mirroring...");

            List<Transform> catchers = newBoard.GetCatchers();
            if (catchers == null || catchers.Count == 0)
            {
                Debug.LogWarning("[OrientationChange] No catchers in new layout");
                capturedBallStates.Clear();
                yield break;
            }

            int oldRowCount = capturedBallStates[0].rowCountWhenDropped;
            int newRowCount = newBoard.GetCurrentRows();

            if (oldRowCount != newRowCount)
            {
                Debug.LogWarning($"[OrientationChange] Settings mismatch! Old={oldRowCount}, New={newRowCount}. This should not happen!");
            }

            Debug.Log($"[OrientationChange] Old layout: {oldRowCount} rows, New layout: {newRowCount} rows");

            int restoredCount = 0;

            foreach (BallState state in capturedBallStates)
            {
                if (state.targetCatcherIndex < 0 || state.targetCatcherIndex >= catchers.Count)
                {
                    Debug.LogWarning($"[OrientationChange] Catcher {state.targetCatcherIndex} out of range, skipping");
                    continue;
                }

                float oldProgress = (float)state.estimatedCurrentRow / Mathf.Max(oldRowCount, 1);
                int newRow = Mathf.RoundToInt(oldProgress * newRowCount);

                int variance = Random.Range(-maxRowVariance, maxRowVariance + 1);
                newRow = Mathf.Clamp(newRow + variance, 0, newRowCount);

                Debug.Log($"[OrientationChange] Ball mirror: OldRow={state.estimatedCurrentRow}/{oldRowCount} → NewRow={newRow}/{newRowCount} (progress={oldProgress:F2}, settings={state.rowCountWhenDropped}rows+{state.riskLevelWhenDropped})");

                if (newLauncher != null)
                {
                    newLauncher.DropBallFromRow(state.targetCatcherIndex, newRow, newRowCount);
                    restoredCount++;

                    yield return new WaitForSeconds(ballTransitionDelay);
                }
            }

            Debug.Log($"[OrientationChange] Successfully restored {restoredCount} balls with enforced settings");
            capturedBallStates.Clear();
        }

        /// <summary>
        /// FIXED: AGGRESSIVE CLEANUP with proper event handler removal
        /// Prevents duplicate event subscriptions that cause wrong results
        /// </summary>
        private IEnumerator AggressiveCleanup(BoardController oldBoard, BallLauncher oldLauncher,
                                              BoardController nextBoard, BallLauncher nextLauncher)
        {
            Debug.Log("[OrientationChange] === STARTING AGGRESSIVE CLEANUP ===");

            // 1. Stop and deactivate ALL balls in old launcher
            if (oldLauncher != null)
            {
                List<GameObject> oldBallPool = GetBallPoolFromLauncher(oldLauncher);
                if (oldBallPool != null)
                {
                    Debug.Log($"[OrientationChange] Cleaning up {oldBallPool.Count} balls from old layout");

                    // ✅ FIX: Cache the BallLauncher component ONCE before the loop
                    // This ensures we're using the SAME instance reference for unsubscription
                    // Previously, GetComponent<BallLauncher>() inside the loop created new references
                    // which prevented proper event unsubscription, causing duplicate result processing
                    BallLauncher launcherComponent = oldLauncher.GetComponent<BallLauncher>();

                    if (launcherComponent == null)
                    {
                        Debug.LogWarning("[OrientationChange] Could not get BallLauncher component from oldLauncher!");
                    }

                    foreach (GameObject ball in oldBallPool)
                    {
                        if (ball == null) continue;

                        // Stop physics
                        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
                        if (rb != null)
                        {
                            rb.simulated = false;
                            rb.linearVelocity = Vector2.zero;
                            rb.angularVelocity = 0;
                        }

                        // ✅ CRITICAL FIX: Properly unsubscribe using cached launcher reference
                        // This prevents duplicate event handlers that cause wrong/delayed results
                        BallController controller = ball.GetComponent<BallController>();
                        if (controller != null && launcherComponent != null)
                        {
                            // Remove the event handler using the CORRECT instance reference
                            controller.OnBallCaught -= launcherComponent.OnBallLanded;
                            Debug.Log($"[OrientationChange] Unsubscribed ball {ball.name} from launcher events");
                        }

                        // Deactivate ball
                        if (ball.activeSelf)
                        {
                            ball.SetActive(false);
                        }
                    }

                    Debug.Log("[OrientationChange] All old balls stopped and deactivated with proper event cleanup");
                }
            }

            // 2. Clean up old board states
            if (oldBoard != null && oldBoard != nextBoard)
            {
                Debug.Log("[OrientationChange] Cleaning up old board");
                oldBoard.CleanupCatcherStates();
                oldBoard.gameObject.SetActive(false);
            }

            // 3. Activate new layout
            if (nextBoard != null && !nextBoard.gameObject.activeSelf)
            {
                nextBoard.gameObject.SetActive(true);
            }

            if (nextLauncher != null && !nextLauncher.gameObject.activeSelf)
            {
                nextLauncher.gameObject.SetActive(true);
            }

            yield return new WaitForEndOfFrame();

            Debug.Log("[OrientationChange] === CLEANUP COMPLETE ===");
        }

        /// <summary>
        /// Gets peg rows from board in local space
        /// </summary>
        private List<List<Vector2>> GetPegRowsFromBoard(BoardController board)
        {
            List<List<Vector2>> pegRows = new List<List<Vector2>>();
            Transform boardTransform = board.transform;
            Dictionary<float, List<Vector2>> rowDict = new Dictionary<float, List<Vector2>>();

            for (int i = 0; i < boardTransform.childCount; i++)
            {
                Transform child = boardTransform.GetChild(i);

                if (child.gameObject.activeSelf && child.CompareTag("Peg"))
                {
                    Vector2 localPos = child.localPosition;
                    float yPos = Mathf.Round(localPos.y * 100f) / 100f;

                    if (!rowDict.ContainsKey(yPos))
                    {
                        rowDict[yPos] = new List<Vector2>();
                    }

                    rowDict[yPos].Add(localPos);
                }
            }

            List<float> sortedY = new List<float>(rowDict.Keys);
            sortedY.Sort((a, b) => b.CompareTo(a));

            foreach (float y in sortedY)
            {
                pegRows.Add(rowDict[y]);
            }

            return pegRows;
        }

        /// <summary>
        /// Gets the ball pool from a ball launcher via reflection
        /// </summary>
        private List<GameObject> GetBallPoolFromLauncher(BallLauncher launcher)
        {
            if (launcher == null) return null;

            var field = launcher.GetType().GetField("ballPool",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                return field.GetValue(launcher) as List<GameObject>;
            }

            Debug.LogWarning("[OrientationChange] Could not access ballPool via reflection");
            return null;
        }

        /// <summary>
        /// Parses catcher index from name
        /// </summary>
        private int ParseCatcherIndex(string catcherName)
        {
            if (string.IsNullOrEmpty(catcherName)) return -1;

            string numStr = catcherName.Replace("Catchers", "").Replace("Catcher", "").Trim();
            if (int.TryParse(numStr, out int index))
            {
                return index;
            }

            return -1;
        }

        private float CalculateMatchValue(int width, int height, float aspectRatio, bool isLandscape)
        {
            if (isLandscape)
            {
                return CalculateLandscapeMatch(aspectRatio);
            }
            else
            {
                return CalculatePortraitMatch(width, height, aspectRatio);
            }
        }

        private float CalculatePortraitMatch(int width, int height, float aspectRatio)
        {
            // iPad detection
            if (currentDevice == "IP" || aspectRatio < 1.42f)
            {
                if (aspectRatio < 1.38f)
                {
                    Debug.Log("[OrientationChange] Detected: iPad 4:3");
                    return match_portrait_ipad;
                }
                else
                {
                    Debug.Log("[OrientationChange] Detected: iPad Pro");
                    return match_portrait_ipad_pro;
                }
            }

            // Full HD portrait
            if ((width == 1080 && height == 1920) || (width == 1920 && height == 1080))
            {
                Debug.Log("[OrientationChange] Detected: Full HD 1080x1920");
                return match_portrait_1080_1920;
            }

            // 1080x2340
            if ((width == 1080 && height == 2340) || (width == 2340 && height == 1080))
            {
                Debug.Log("[OrientationChange] Detected: 1080x2340");
                return match_portrait_1080_2340;
            }

            // 1080x2400
            if ((width == 1080 && height == 2400) || (width == 2400 && height == 1080))
            {
                Debug.Log("[OrientationChange] Detected: 1080x2400");
                return match_portrait_1080_2400;
            }

            // 1440x2960
            if ((width == 1440 && height == 2960) || (width == 2960 && height == 1440))
            {
                Debug.Log("[OrientationChange] Detected: 1440x2960 (QHD+)");
                return match_portrait_1440_2960;
            }

            // 1440x3200
            if ((width == 1440 && height == 3200) || (width == 3200 && height == 1440))
            {
                Debug.Log("[OrientationChange] Detected: 1440x3200 (QHD+)");
                return match_portrait_1440_3200;
            }

            // 9:16 ratio
            if (Mathf.Abs(aspectRatio - 1.778f) < 0.05f)
            {
                if (height < 1800)
                {
                    Debug.Log("[OrientationChange] Detected: 9:16 ratio (small screen)");
                    return match_portrait_9_16;
                }
            }

            // Aspect ratio fallback
            if (aspectRatio < 1.42f)
            {
                Debug.Log("[OrientationChange] Aspect ratio fallback: 4:3");
                return match_portrait_4_3;
            }
            else if (aspectRatio < 1.55f)
            {
                Debug.Log("[OrientationChange] Aspect ratio fallback: 3:2");
                return match_portrait_3_2;
            }
            else if (aspectRatio < 1.69f)
            {
                Debug.Log("[OrientationChange] Aspect ratio fallback: 16:10");
                return match_portrait_16_10;
            }
            else if (aspectRatio < 1.89f)
            {
                Debug.Log("[OrientationChange] Aspect ratio fallback: 16:9");
                return match_portrait_16_9;
            }
            else if (aspectRatio < 2.08f)
            {
                Debug.Log("[OrientationChange] Aspect ratio fallback: 18:9");
                return match_portrait_18_9;
            }
            else if (aspectRatio < 2.25f)
            {
                Debug.Log("[OrientationChange] Aspect ratio fallback: 19:9");
                return match_portrait_19_9;
            }
            else if (aspectRatio < 2.40f)
            {
                Debug.Log("[OrientationChange] Aspect ratio fallback: 21:9");
                return match_portrait_21_9;
            }
            else
            {
                Debug.Log("[OrientationChange] Aspect ratio fallback: Ultra-wide");
                return match_portrait_ultra;
            }
        }

        private float CalculateLandscapeMatch(float aspectRatio)
        {
            if (currentDevice == "IP" || aspectRatio < 1.5f)
            {
                Debug.Log("[OrientationChange] Landscape: Tablet");
                return match_landscape_tablet;
            }
            else if (aspectRatio < 2.0f)
            {
                Debug.Log("[OrientationChange] Landscape: Standard");
                return match_landscape_standard;
            }
            else
            {
                Debug.Log("[OrientationChange] Landscape: Wide");
                return match_landscape_wide;
            }
        }

        private void ApplyDeviceSpecificSettings()
        {
            if (canvasScaler == null) return;

            float initialMatch = currentDevice == "IP" ? match_portrait_ipad : match_portrait_16_9;
            canvasScaler.matchWidthOrHeight = initialMatch;

            Debug.Log($"[OrientationChange] Device: {currentDevice}, Initial Match: {initialMatch}");
        }

        private void ForceCanvasUpdate()
        {
            if (canvasScaler != null)
            {
                Canvas canvas = canvasScaler.GetComponent<Canvas>();
                if (canvas != null)
                {
                    Canvas.ForceUpdateCanvases();

                    RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                    if (canvasRect != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);
                    }
                }
            }
        }

        public BoardController GetActiveBoard() => activeBoard;
        public BallLauncher GetActiveLauncher() => activeLauncher;

#if UNITY_EDITOR
        [Header("Editor Testing")]
        [SerializeField] private bool enableEditorTesting = true;

        private void Update()
        {
            if (!enableEditorTesting) return;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                SwitchDisplay($"{Screen.height},{Screen.width}");
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                string[] devices = { "MB", "IP", "PC" };
                int idx = System.Array.IndexOf(devices, currentDevice);
                idx = (idx + 1) % devices.Length;
                DeviceCheck(devices[idx]);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                swapLayoutLogic = !swapLayoutLogic;
                Debug.Log($"[OrientationChange] Swap: {swapLayoutLogic}");
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SwitchDisplay("290,516");
                Debug.Log("[Editor Test] 9:16 portrait (290x516)");
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SwitchDisplay("1080,1920");
                Debug.Log("[Editor Test] Full HD portrait (1080x1920)");
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SwitchDisplay("1080,2340");
                Debug.Log("[Editor Test] 19.5:9 portrait (1080x2340)");
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SwitchDisplay("768,1024");
                Debug.Log("[Editor Test] iPad portrait (768x1024)");
            }
        }
#endif

        private void OnDestroy()
        {
            if (matchTween != null && matchTween.IsActive())
            {
                matchTween.Kill();
            }

            if (orientationRoutine != null)
            {
                StopCoroutine(orientationRoutine);
            }

            capturedBallStates.Clear();
        }
    }
}