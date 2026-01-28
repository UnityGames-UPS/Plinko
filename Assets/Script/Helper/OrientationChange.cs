using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

namespace PlinkoGame
{
    /// <summary>
    /// Orientation handler with PIXEL-BASED match value detection
    /// Handles specific resolutions like 9:16 vs 1080x1920 differently
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
        [SerializeField] private float cleanupDelay = 0.15f;

        [Header("Layout Logic")]
        [SerializeField] private bool swapLayoutLogic = false;

        [Header("Portrait Match Values - Common Resolutions")]
        [SerializeField] private float match_portrait_9_16 = 0.38f;        // 9:16 (516x290, etc)
        [SerializeField] private float match_portrait_1080_1920 = 0.42f;  // Full HD portrait
        [SerializeField] private float match_portrait_1080_2340 = 0.46f;  // 1080x2340 (19.5:9)
        [SerializeField] private float match_portrait_1080_2400 = 0.48f;  // 1080x2400 (20:9)
        [SerializeField] private float match_portrait_1440_2960 = 0.46f;  // QHD+ (18.5:9)
        [SerializeField] private float match_portrait_1440_3200 = 0.50f;  // QHD+ (20:9)

        [Header("Portrait Match Values - iPad/Tablets")]
        [SerializeField] private float match_portrait_ipad = 0.22f;        // iPad (4:3)
        [SerializeField] private float match_portrait_ipad_pro = 0.25f;   // iPad Pro

        [Header("Portrait Match Values - Aspect Ratio Fallbacks")]
        [SerializeField] private float match_portrait_4_3 = 0.22f;    // ~1.33 (4:3)
        [SerializeField] private float match_portrait_3_2 = 0.28f;    // ~1.50 (3:2)
        [SerializeField] private float match_portrait_16_10 = 0.32f;  // ~1.60 (16:10)
        [SerializeField] private float match_portrait_16_9 = 0.38f;   // ~1.78 (16:9)
        [SerializeField] private float match_portrait_18_9 = 0.42f;   // ~2.00 (18:9)
        [SerializeField] private float match_portrait_19_9 = 0.46f;   // ~2.16 (19.5:9)
        [SerializeField] private float match_portrait_21_9 = 0.50f;   // ~2.33 (21:9)
        [SerializeField] private float match_portrait_ultra = 0.55f;  // > 2.4

        [Header("Landscape Match Values")]
        [SerializeField] private float match_landscape_tablet = 0.75f;
        [SerializeField] private float match_landscape_standard = 0.85f;
        [SerializeField] private float match_landscape_wide = 1.0f;

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

            yield return StartCoroutine(CompleteCleanup(nextBoard, nextLauncher));

            // PIXEL-BASED match value calculation
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

            if (uiManager != null)
            {
                int sendWidth = swapLayoutLogic ? height : width;
                int sendHeight = swapLayoutLogic ? width : height;

                uiManager.OnOrientationChanged(sendWidth, sendHeight);
            }

            activeBoard = nextBoard;
            activeLauncher = nextLauncher;

            yield return new WaitForEndOfFrame();
            ForceCanvasUpdate();
            yield return new WaitForEndOfFrame();

            if (activeBoard != null)
            {
                Debug.Log($"[OrientationChange] Building FRESH pyramid on {(newIsLandscape ? "HORIZONTAL" : "VERTICAL")} board...");
                activeBoard.OnOrientationChanged();
            }

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            if (activeLauncher != null)
            {
                Debug.Log("[OrientationChange] Updating ball launcher...");
                activeLauncher.OnBoardRebuilt();
            }

            isLandscape = newIsLandscape;
            lastWidth = width;
            lastHeight = height;
            isTransitioning = false;

            Debug.Log($"[OrientationChange] ========== COMPLETE: {(newIsLandscape ? "HORIZONTAL" : "VERTICAL")} ==========");

            if (!string.IsNullOrEmpty(pendingDimensions))
            {
                string pending = pendingDimensions;
                pendingDimensions = "";
                SwitchDisplay(pending);
            }
        }

        private IEnumerator CompleteCleanup(BoardController nextBoard, BallLauncher nextLauncher)
        {
            Debug.Log("[OrientationChange] === CLEANUP BOTH LAYOUTS ===");

            DOTween.KillAll(false);

            CleanupLayout(horizontalBoardController, horizontalBallLauncher);
            CleanupLayout(verticalBoardController, verticalBallLauncher);

            yield return new WaitForSeconds(cleanupDelay);

            System.GC.Collect();

            Debug.Log("[OrientationChange] === CLEANUP COMPLETE ===");
        }

        private void CleanupLayout(BoardController board, BallLauncher launcher)
        {
            if (board == null) return;

            var pegs = board.transform.GetComponentsInChildren<PegHitAnimation>(true);
            foreach (var peg in pegs)
            {
                if (peg != null)
                {
                    peg.gameObject.SetActive(false);
                }
            }

            var catchers = board.GetCatchers();
            if (catchers != null)
            {
                foreach (var catcher in catchers)
                {
                    if (catcher != null)
                    {
                        var catcherScript = catcher.GetComponent<BallCatcher>();
                        if (catcherScript != null)
                        {
                            catcherScript.ResetState();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// PIXEL-BASED match value calculation
        /// Checks exact resolutions first, then falls back to aspect ratio
        /// </summary>
        private float CalculateMatchValue(int width, int height, float aspectRatio, bool landscape)
        {
            if (landscape)
            {
                return CalculateLandscapeMatch(aspectRatio);
            }
            else
            {
                return CalculatePortraitMatch(width, height, aspectRatio);
            }
        }

        /// <summary>
        /// Portrait match calculation - PIXEL-BASED with aspect ratio fallback
        /// </summary>
        private float CalculatePortraitMatch(int width, int height, float aspectRatio)
        {
            // Check for iPad first (device type)
            if (currentDevice == "IP")
            {
                // iPad specific resolutions
                if (height >= 1024 && height <= 1366 && aspectRatio < 1.4f)
                {
                    Debug.Log("[OrientationChange] Detected: iPad");
                    return match_portrait_ipad; // 0.22 for iPad
                }
                else if (height > 1366 && aspectRatio < 1.4f)
                {
                    Debug.Log("[OrientationChange] Detected: iPad Pro");
                    return match_portrait_ipad_pro; // 0.25 for iPad Pro
                }
            }

            // SPECIFIC RESOLUTION DETECTION (highest priority)

            // 1080x1920 (Full HD Portrait) - MUST check BEFORE general 16:9
            if ((width == 1080 && height == 1920) || (width == 1920 && height == 1080))
            {
                Debug.Log("[OrientationChange] Detected: 1080x1920 (Full HD)");
                return match_portrait_1080_1920; // 0.42
            }

            // 1080x2340 (19.5:9)
            if ((width == 1080 && height == 2340) || (width == 2340 && height == 1080))
            {
                Debug.Log("[OrientationChange] Detected: 1080x2340");
                return match_portrait_1080_2340; // 0.46
            }

            // 1080x2400 (20:9)
            if ((width == 1080 && height == 2400) || (width == 2400 && height == 1080))
            {
                Debug.Log("[OrientationChange] Detected: 1080x2400");
                return match_portrait_1080_2400; // 0.48
            }

            // 1440x2960 (QHD+ 18.5:9)
            if ((width == 1440 && height == 2960) || (width == 2960 && height == 1440))
            {
                Debug.Log("[OrientationChange] Detected: 1440x2960 (QHD+)");
                return match_portrait_1440_2960; // 0.46
            }

            // 1440x3200 (QHD+ 20:9)
            if ((width == 1440 && height == 3200) || (width == 3200 && height == 1440))
            {
                Debug.Log("[OrientationChange] Detected: 1440x3200 (QHD+)");
                return match_portrait_1440_3200; // 0.50
            }

            // 9:16 ratio detection (for editor/small screens like 516x290, 720x1280, etc)
            // This catches 16:9 aspect ratio displays that are NOT Full HD 1080x1920
            if (Mathf.Abs(aspectRatio - 1.778f) < 0.05f) // 16:9 range
            {
                // Small screens or non-Full-HD 16:9
                if (height < 1800) // Less than Full HD height
                {
                    Debug.Log("[OrientationChange] Detected: 9:16 ratio (small screen)");
                    return match_portrait_9_16; // 0.38
                }
            }

            // ASPECT RATIO FALLBACK (if no specific resolution matched)

            if (aspectRatio < 1.42f)
            {
                Debug.Log("[OrientationChange] Aspect ratio fallback: 4:3");
                return match_portrait_4_3; // ~1.33 (4:3)
            }
            else if (aspectRatio < 1.55f)
            {
                Debug.Log("[OrientationChange] Aspect ratio fallback: 3:2");
                return match_portrait_3_2; // ~1.50 (3:2)
            }
            else if (aspectRatio < 1.69f)
            {
                Debug.Log("[OrientationChange] Aspect ratio fallback: 16:10");
                return match_portrait_16_10; // ~1.60 (16:10)
            }
            else if (aspectRatio < 1.89f)
            {
                Debug.Log("[OrientationChange] Aspect ratio fallback: 16:9");
                return match_portrait_16_9; // ~1.78 (16:9) - 0.38
            }
            else if (aspectRatio < 2.08f)
            {
                Debug.Log("[OrientationChange] Aspect ratio fallback: 18:9");
                return match_portrait_18_9; // ~2.00 (18:9) - 0.42
            }
            else if (aspectRatio < 2.25f)
            {
                Debug.Log("[OrientationChange] Aspect ratio fallback: 19:9");
                return match_portrait_19_9; // ~2.16 (19.5:9) - 0.46
            }
            else if (aspectRatio < 2.40f)
            {
                Debug.Log("[OrientationChange] Aspect ratio fallback: 21:9");
                return match_portrait_21_9; // ~2.33 (21:9) - 0.50
            }
            else
            {
                Debug.Log("[OrientationChange] Aspect ratio fallback: Ultra-wide");
                return match_portrait_ultra; // > 2.4 - 0.55
            }
        }

        private float CalculateLandscapeMatch(float aspectRatio)
        {
            if (currentDevice == "IP" || aspectRatio < 1.5f)
            {
                Debug.Log("[OrientationChange] Landscape: Tablet");
                return match_landscape_tablet; // 0.75
            }
            else if (aspectRatio < 2.0f)
            {
                Debug.Log("[OrientationChange] Landscape: Standard");
                return match_landscape_standard; // 0.85
            }
            else
            {
                Debug.Log("[OrientationChange] Landscape: Wide");
                return match_landscape_wide; // 1.0
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

            if (Input.GetKeyDown(KeyCode.A))
            {
                Debug.Log($"[OrientationChange] Active Board: {(activeBoard == horizontalBoardController ? "HORIZONTAL" : "VERTICAL")}");
            }

            // Test specific resolutions
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                // 9:16 ratio - editor test (portrait orientation)
                SwitchDisplay("290,516"); // Portrait: width < height
                Debug.Log("[Editor Test] 9:16 portrait (290x516)");
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                // Full HD portrait
                SwitchDisplay("1080,1920"); // Portrait: width < height
                Debug.Log("[Editor Test] Full HD portrait (1080x1920)");
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                // 19.5:9 portrait
                SwitchDisplay("1080,2340"); // Portrait: width < height
                Debug.Log("[Editor Test] 19.5:9 portrait (1080x2340)");
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                // iPad portrait
                SwitchDisplay("768,1024"); // Portrait: width < height
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
        }
    }
}