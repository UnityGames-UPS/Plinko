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


        [Header("Vertical Game Area Adjustment")]
        [SerializeField] private bool enableDynamicBoardAdjustment = true;
        [SerializeField] private RectTransform verticalBoardLayoutRoot;
        [SerializeField] private Vector2 verticalBoardPadding = new Vector2(40f, 60f);
        [SerializeField] private Vector2 verticalBoardAnchorMin = new Vector2(0f, 0.45f);
        [SerializeField] private Vector2 verticalBoardAnchorMax = new Vector2(1f, 1f);

        [Header("Vertical Bottom Panel Adjustment")]
        [SerializeField] private RectTransform verticalBottomPanelRoot;
        [SerializeField] private Vector2 verticalBottomPadding = new Vector2(0f, 0f);
        [SerializeField] private Vector2 verticalBottomAnchorMin = new Vector2(0f, 0f);
        [SerializeField] private Vector2 verticalBottomAnchorMax = new Vector2(1f, 0.45f);

        [Header("General Vertical Settings")]
        [SerializeField] private float maxVerticalBoardWidth = 1000f;
        [SerializeField] private float verticalBoardYOffset = -50f;
        [SerializeField] private float tabletVerticalPaddingMultiplier = 0.6f;
        [SerializeField] private float tallnessPaddingSensitivity = 1.0f;

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

        private void Start()
        {
            // Trigger initial orientation setup
            // We force a refresh even if dimensions match initial values
            lastWidth = -1;
            lastHeight = -1;
            SwitchDisplay($"{Screen.width},{Screen.height}");
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
            if (Application.isPlaying)
            {
                CaptureBallStatesWithRowInfo(activeBoard, activeLauncher);
            }

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
            if (Application.isPlaying)
            {
                yield return StartCoroutine(AggressiveCleanup(activeBoard, activeLauncher, nextBoard, nextLauncher));
            }
            else
            {
                // In Editor mode, just switch visibility
                if (activeBoard != null) activeBoard.gameObject.SetActive(false);
                if (nextBoard != null) nextBoard.gameObject.SetActive(true);
            }

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

            // === STEP 3.5: ADJUST GAME AREA & BOTTOM PANEL LAYOUT ===
            if (enableDynamicBoardAdjustment)
            {
                AdjustVerticalLayout(newIsLandscape, aspectRatio);
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
            if (Application.isPlaying)
            {
                yield return StartCoroutine(RestoreBallStatesWithRowMirroring(activeBoard, activeLauncher));
            }

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

            // 2. Clean up old board states (if different from next)
            if (oldBoard != null && oldBoard != nextBoard)
            {
                oldBoard.CleanupAllResources();
                oldBoard.gameObject.SetActive(false);
            }

            if (oldLauncher != null && oldLauncher != nextLauncher)
            {
                oldLauncher.gameObject.SetActive(false);
            }

            // 3. Activate new layout
            if (nextBoard != null)
            {
                nextBoard.gameObject.SetActive(true);
                // Ensure it's active for coroutines
                if (!nextBoard.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning("[OrientationChange] Board GameObject is active but parent is inactive!");
                }
            }
            if (nextLauncher != null) nextLauncher.gameObject.SetActive(true);

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
            if (width <= 0 || height <= 0) return 0.5f;

            Vector2 refRes = (canvasScaler != null) ? canvasScaler.referenceResolution : referenceAspect;
            if (refRes.x <= 0 || refRes.y <= 0)
            {
                refRes = new Vector2(2340f, 1080f);
            }

            float refW = refRes.x;
            float refH = refRes.y;

            float widthScale = (float)width / refW;
            float heightScale = (float)height / refH;

            float targetScale;
            if (isLandscape)
            {
                targetScale = Mathf.Min(widthScale, heightScale);
            }
            else
            {
                float maxRef = Mathf.Max(refW, refH);
                float minRef = Mathf.Min(refW, refH);
                float portraitWidthScale = (float)height / maxRef;
                float portraitHeightScale = (float)width / minRef;
                targetScale = Mathf.Min(portraitWidthScale, portraitHeightScale);
            }

            if (Mathf.Abs(heightScale - widthScale) < 0.0001f)
            {
                return 0.5f;
            }

            float logRatio = Mathf.Log(heightScale / widthScale);
            if (Mathf.Abs(logRatio) < 0.0001f)
            {
                return 0.5f;
            }

            float targetMatch = Mathf.Log(targetScale / widthScale) / logRatio;
            targetMatch = Mathf.Clamp01(targetMatch);

            Debug.Log($"[OrientationChange] Dynamic target match calculated: {targetMatch:F3} for viewport {width}x{height} (isLandscape: {isLandscape})");

            return targetMatch;
        }

        private void ApplyDeviceSpecificSettings()
        {
            if (canvasScaler == null) return;

            int w = lastWidth > 0 ? lastWidth : Screen.width;
            int h = lastHeight > 0 ? lastHeight : Screen.height;
            float aspect = w > h ? (float)w / h : (float)h / w;
            bool isLand = w > h;

            float initialMatch = CalculateMatchValue(w, h, aspect, isLand);
            canvasScaler.matchWidthOrHeight = initialMatch;

            Debug.Log($"[OrientationChange] Device: {currentDevice}, Dynamic Initial Match: {initialMatch:F3}");
        }

        /// <summary>
        /// Dynamically adjusts the game area (BoardController) and Bottom Panel 
        /// to split the screen correctly in vertical mode.
        /// </summary>
        private void AdjustVerticalLayout(bool isLandscape, float aspectRatio)
        {
            if (verticalBoardController == null) return;

            // 1. ADJUST TOP BOARD AREA
            RectTransform boardRect = verticalBoardLayoutRoot;
            if (boardRect == null) boardRect = verticalBoardController.FitAreaParent;
            if (boardRect == null) boardRect = verticalBoardController.GetComponent<RectTransform>();

            if (boardRect != null)
            {
                if (isLandscape)
                {
                    ResetRectTransform(boardRect);
                }
                else
                {
                    ApplyLayoutToRect(boardRect, verticalBoardAnchorMin, verticalBoardAnchorMax, verticalBoardPadding, verticalBoardYOffset, aspectRatio);
                }
            }

            // 2. ADJUST BOTTOM PANEL AREA
            if (verticalBottomPanelRoot != null)
            {
                if (isLandscape)
                {
                    ResetRectTransform(verticalBottomPanelRoot);
                }
                else
                {
                    ApplyLayoutToRect(verticalBottomPanelRoot, verticalBottomAnchorMin, verticalBottomAnchorMax, verticalBottomPadding, 0f, aspectRatio);
                }
            }

            Debug.Log($"[OrientationChange] Adjusted Vertical Layout: Board in {verticalBoardAnchorMin.y}-{verticalBoardAnchorMax.y}, Bottom in {verticalBottomAnchorMin.y}-{verticalBottomAnchorMax.y}");
        }

        private void ApplyLayoutToRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 padding, float yOffset, float aspectRatio)
        {
            if (referenceAspect.x <= 0 || referenceAspect.y <= 0) referenceAspect = new Vector2(1080, 1920);

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);

            float paddingX = padding.x;
            float paddingY = padding.y;

            // Tablet specific padding adjustment
            if (currentDevice == "IP" || aspectRatio < 1.5f)
            {
                paddingX *= tabletVerticalPaddingMultiplier;
                paddingY *= tabletVerticalPaddingMultiplier;
            }

            // Dynamic Y padding for tall phones (Aspect Ratio > 16:9)
            // Goal: Keep the board height consistent with a 16:9 reference resolution
            float refAspectVal = 1.777f; // 9:16
            if (aspectRatio > refAspectVal)
            {
                // Calculate what the height of this area WOULD be on a 16:9 screen
                float refWidth = Mathf.Min(referenceAspect.x, referenceAspect.y);
                float refCanvasHeight = refWidth * refAspectVal;
                float currentCanvasHeight = refWidth * aspectRatio;
                
                float areaHeightRange = (anchorMax.y - anchorMin.y);
                float refAreaHeight = refCanvasHeight * areaHeightRange;
                float currentAreaHeight = currentCanvasHeight * areaHeightRange;
                
                // Add padding to consume the extra height, modulated by sensitivity
                float extraHeight = currentAreaHeight - refAreaHeight;
                paddingY += (extraHeight / 2f) * tallnessPaddingSensitivity;
            }
            
            // Explicitly set Top and Bottom values via offsetMin/offsetMax
            float left = paddingX;
            float right = paddingX;
            
            // If the panel is snapped to the bottom (anchorMin.y == 0), keep bottom offset at 0 or use fixed padding
            // Otherwise, apply dynamic padding to help center/squish the content
            float bottom = (anchorMin.y <= 0.01f) ? padding.y : paddingY + yOffset;
            float top = (anchorMax.y >= 0.99f) ? padding.y : paddingY - yOffset;

            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);

            Debug.Log($"[OrientationChange] {rect.name} Layout: Top={top:F1}, Bottom={bottom:F1}, Aspect={aspectRatio:F3}");
        }

        private void ResetRectTransform(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
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
            // Auto-detect resolution changes in Editor or Play mode
            if (Screen.width != lastWidth || Screen.height != lastHeight)
            {
                if (lastWidth != 0 && lastHeight != 0) // Avoid initial jump
                {
                    SwitchDisplay($"{Screen.width},{Screen.height}");
                }
                else
                {
                    lastWidth = Screen.width;
                    lastHeight = Screen.height;
                }
            }

            if (!enableEditorTesting) return;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                SwitchDisplay($"{Screen.width},{Screen.height}");
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