using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace PlinkoGame
{
    /// <summary>
    /// Enhanced BoardController with proper resource cleanup
    /// - Cleans up pegs and tweens during layout changes
    /// - Prevents memory leaks
    /// - Handles rapid layout changes gracefully
    /// </summary>
    public class BoardController : MonoBehaviour
    {
        [Header("Pyramid Settings")]
        [SerializeField] private int startPegCount = 3;
        private const int MAX_ROWS = 16;

        [Header("Peg Settings")]
        [SerializeField] private GameObject pegPrefab;
        [SerializeField] private float basePegSize = 0.25f;
        [SerializeField] private float baseXSpacing = 1.2f;
        [SerializeField] private float topPadding = 0.5f;

        [Header("Anchors")]
        [SerializeField] private Transform topAnchor;
        [SerializeField] private Transform bottomAnchor;
        [SerializeField] private BoxCollider2D fitArea;

        [Header("Catchers (17 objects)")]
        [SerializeField] private List<Transform> catchers;

        [Header("Catcher Scaling (Row-Based)")]
        [SerializeField] private float catcherXScaleAt8Rows = 1f;
        [SerializeField] private float catcherXScaleAt16Rows = 0.55f;
        [SerializeField] private float catcherYScaleAt8Rows = 1f;
        [SerializeField] private float catcherYScaleAt16Rows = 0.7f;

        [Header("Catcher Y Offset Scaling")]
        [SerializeField] private float baseYOffsetAt8Rows = -0.69f;
        [SerializeField] private float yOffsetAt16Rows = -0.2f;

        [Header("UI")]
        [SerializeField] private Canvas mainCanvas;

        [Header("Limits")]
        [SerializeField] private float minScale = 0.4f;

        [Header("Ref")]
        [SerializeField] private BallLauncher ballLauncher;

        private List<GameObject> pegPool = new List<GameObject>();
        private int currentRows = 8;
        private bool isRebuilding;
        private Services.MultiplierService multiplierService;
        private float firstPegRowLocalY = 0f;

        // Cleanup tracking
        private List<PegHitAnimation> activePegAnimations = new List<PegHitAnimation>();
        private Coroutine rebuildCoroutine;

        private void Start()
        {
            multiplierService = new Services.MultiplierService();
            InitializePegPool();
            currentRows = 8;
            Rebuild();
        }

        private void InitializePegPool()
        {
            if (!pegPrefab) return;

            int totalPegsNeeded = 0;
            for (int row = 0; row < MAX_ROWS; row++)
                totalPegsNeeded += startPegCount + row;

            for (int i = 0; i < totalPegsNeeded; i++)
            {
                GameObject peg = Instantiate(pegPrefab, transform);
                peg.SetActive(false);
                pegPool.Add(peg);
            }

            Debug.Log($"[BoardController] Initialized {totalPegsNeeded} pegs in pool");
        }

        public void UpdateCatcherMultipliers(List<double> multipliers)
        {
            int catchersToUse = currentRows + 1;
            int startIndex = (catchers.Count - catchersToUse) / 2;

            for (int i = 0; i < catchersToUse; i++)
            {
                int catcherIndex = startIndex + i;
                if (catcherIndex < 0 || catcherIndex >= catchers.Count) continue;

                BallCatcher catcherScript = catchers[catcherIndex].GetComponent<BallCatcher>();
                if (catcherScript != null && i < multipliers.Count)
                {
                    catcherScript.SetMultiplier((float)multipliers[i]);
                }
            }
        }

        public void SetRows(int rows)
        {
            if (isRebuilding) return;
            currentRows = Mathf.Clamp(rows, 8, MAX_ROWS);
            StartCoroutine(RebuildWithCanvasRefresh());
        }

        public int GetCurrentRows() => currentRows;
        public float GetFirstPegRowLocalY() => firstPegRowLocalY;
        public List<Transform> GetCatchers() => catchers;

        public void OnOrientationChanged()
        {
            if (!isRebuilding)
            {
                if (rebuildCoroutine != null)
                {
                    StopCoroutine(rebuildCoroutine);
                }

                Debug.Log("[BoardController] === STARTING FRESH PYRAMID BUILD ===");
                rebuildCoroutine = StartCoroutine(CompleteFreshRebuild());
            }
        }

        /// <summary>
        /// Complete fresh rebuild - destroys old pyramid completely
        /// </summary>
        private IEnumerator CompleteFreshRebuild()
        {
            if (isRebuilding)
            {
                Debug.Log("[BoardController] Already rebuilding, skipping");
                yield break;
            }

            isRebuilding = true;

            // Step 1: DESTROY everything old
            DestroyOldPyramid();
            yield return new WaitForEndOfFrame();

            // Step 2: Rebuild fresh
            Rebuild();
            yield return new WaitForEndOfFrame();

            // Step 3: Force canvas update
            if (mainCanvas != null)
            {
                Canvas.ForceUpdateCanvases();
                mainCanvas.enabled = false;
                yield return null;
                mainCanvas.enabled = true;
                LayoutRebuilder.ForceRebuildLayoutImmediate(mainCanvas.GetComponent<RectTransform>());
            }

            yield return new WaitForEndOfFrame();

            // Step 4: Update peg animations with new positions
            UpdateAllPegAnimationScales();

            isRebuilding = false;
            Debug.Log("[BoardController] === FRESH PYRAMID BUILD COMPLETE ===");
        }

        /// <summary>
        /// Completely destroy old pyramid setup
        /// </summary>
        private void DestroyOldPyramid()
        {
            Debug.Log("[BoardController] Destroying old pyramid...");

            // Kill all tweens
            DOTween.Kill(transform);
            foreach (var catcher in catchers)
            {
                if (catcher != null)
                {
                    DOTween.Kill(catcher);
                }
            }

            // Disable all pegs and reset them
            foreach (var peg in pegPool)
            {
                if (peg != null && peg.activeSelf)
                {
                    PegHitAnimation anim = peg.GetComponent<PegHitAnimation>();
                    if (anim != null)
                    {
                        DOTween.Kill(anim.transform);
                    }

                    peg.SetActive(false);
                }
            }

            // Reset all catchers completely
            foreach (var catcher in catchers)
            {
                if (catcher == null) continue;

                BallCatcher catcherScript = catcher.GetComponent<BallCatcher>();
                if (catcherScript != null)
                {
                    catcherScript.ResetState();
                }

                catcher.gameObject.SetActive(false);
            }

            // Clear cached lists
            activePegAnimations.Clear();

            Debug.Log("[BoardController] Old pyramid destroyed");
        }

        private IEnumerator RebuildWithCanvasRefresh()
        {
            if (isRebuilding)
            {
                Debug.Log("[BoardController] Already rebuilding, skipping");
                yield break;
            }

            isRebuilding = true;

            // Step 1: Clean up all resources
            CleanupAllResources();

            // Wait a frame for cleanup
            yield return new WaitForEndOfFrame();

            // Step 2: Rebuild the board
            Rebuild();

            // Step 3: Force canvas update
            yield return new WaitForEndOfFrame();

            if (mainCanvas != null)
            {
                Canvas.ForceUpdateCanvases();
                mainCanvas.enabled = false;
                mainCanvas.enabled = true;
                LayoutRebuilder.ForceRebuildLayoutImmediate(mainCanvas.GetComponent<RectTransform>());
            }

            // Step 4: Notify ball launcher
            if (ballLauncher != null)
            {
                ballLauncher.OnBoardRebuilt();
            }

            // Step 5: Update peg animations
            UpdateAllPegAnimationScales();

            isRebuilding = false;

            Debug.Log("[BoardController] Rebuild complete");
        }

        /// <summary>
        /// Comprehensive cleanup of all board resources
        /// </summary>
        private void CleanupAllResources()
        {
            Debug.Log("[BoardController] Starting cleanup...");

            // Kill all DOTween animations
            DOTween.Kill(transform);

            foreach (var catcher in catchers)
            {
                if (catcher != null)
                {
                    DOTween.Kill(catcher);
                }
            }

            // Cleanup peg animations
            CleanupPegAnimations();

            // Cleanup catcher states
            CleanupCatcherStates();

            // Disable all pegs
            DisableAllPegs();

            // Clear cached lists
            activePegAnimations.Clear();

            Debug.Log("[BoardController] Cleanup complete");
        }

        /// <summary>
        /// Clean up all peg animations and tweens
        /// </summary>
        private void CleanupPegAnimations()
        {
            foreach (var peg in pegPool)
            {
                if (peg == null || !peg.activeSelf) continue;

                // Kill peg tweens
                DOTween.Kill(peg.transform);

                PegHitAnimation anim = peg.GetComponent<PegHitAnimation>();
                if (anim != null)
                {
                    // Kill any active animations
                    DOTween.Kill(anim.transform);

                    // Reset peg position and scale
                    RectTransform pegRect = peg.GetComponent<RectTransform>();
                    if (pegRect != null)
                    {
                        pegRect.localScale = Vector3.one;
                    }
                }
            }
        }

        /// <summary>
        /// Clean up all catcher states
        /// </summary>
        private void CleanupCatcherStates()
        {
            foreach (var catcher in catchers)
            {
                if (catcher == null) continue;

                BallCatcher catcherScript = catcher.GetComponent<BallCatcher>();
                if (catcherScript != null)
                {
                    catcherScript.ResetState();
                }
            }
        }

        private void Rebuild()
        {
            if (!pegPrefab || !topAnchor || !bottomAnchor || catchers.Count == 0)
            {
                Debug.LogError("[BoardController] Missing required references!");
                return;
            }

            DisableAllPegs();
            GeneratePyramidAndCatchers();
        }

        private void DisableAllPegs()
        {
            foreach (var peg in pegPool)
            {
                if (peg != null && peg.activeSelf)
                {
                    peg.SetActive(false);
                }
            }
        }

        private void GeneratePyramidAndCatchers()
        {
            RectTransform fitRect = fitArea.GetComponent<RectTransform>();
            float maxWidth = fitRect.rect.width * 0.95f;

            int lastRowPegCount = startPegCount + currentRows - 1;
            float requiredWidth = lastRowPegCount * baseXSpacing;
            float scale = Mathf.Max(maxWidth / requiredWidth, minScale);

            float pegSize = basePegSize * scale;
            float xSpacing = baseXSpacing * scale;

            float topY = topAnchor.localPosition.y - topPadding;
            float bottomY = bottomAnchor.localPosition.y;
            float usableHeight = Mathf.Abs(topY - bottomY);
            float ySpacing = usableHeight / (currentRows - 1);

            Vector2 center = new Vector2(topAnchor.localPosition.x, topY);

            int poolIndex = 0;
            activePegAnimations.Clear();

            // Generate pegs
            for (int row = 0; row < currentRows; row++)
            {
                int pegCount = startPegCount + row;
                float rowWidth = (pegCount - 1) * xSpacing;
                float startX = center.x - rowWidth / 2f;
                float y = center.y - row * ySpacing;

                if (row == 0)
                    firstPegRowLocalY = y;

                for (int i = 0; i < pegCount; i++)
                {
                    if (poolIndex >= pegPool.Count) break;

                    GameObject peg = pegPool[poolIndex++];

                    // Reset peg state completely
                    RectTransform pegRect = peg.GetComponent<RectTransform>();
                    if (pegRect != null)
                    {
                        pegRect.localPosition = new Vector2(startX + i * xSpacing, y);
                        pegRect.localScale = Vector3.one * pegSize;
                        pegRect.localRotation = Quaternion.identity;
                    }

                    // Track animation component
                    PegHitAnimation anim = peg.GetComponent<PegHitAnimation>();
                    if (anim != null)
                    {
                        activePegAnimations.Add(anim);
                    }

                    peg.SetActive(true);
                }
            }

            // Position catchers
            AlignCatchersInGaps(lastRowPegCount, center.x, xSpacing);

            Debug.Log($"[BoardController] Generated {poolIndex} pegs, {activePegAnimations.Count} animations");
        }

        private void AlignCatchersInGaps(int lastRowPegCount, float centerX, float xSpacing)
        {
            int catchersToUse = currentRows + 1;
            int totalCatchers = catchers.Count;

            // Disable all catchers first
            foreach (var c in catchers)
            {
                if (c != null)
                {
                    c.gameObject.SetActive(false);
                }
            }

            int startIndex = (totalCatchers - catchersToUse) / 2;

            float lastRowWidth = (lastRowPegCount - 1) * xSpacing;
            float leftMostPegX = centerX - lastRowWidth / 2f;
            float firstCatcherX = leftMostPegX + (xSpacing / 2f);

            float t = Mathf.InverseLerp(8, 16, currentRows);
            float catcherXScale = Mathf.Lerp(catcherXScaleAt8Rows, catcherXScaleAt16Rows, t);
            float catcherYScale = Mathf.Lerp(catcherYScaleAt8Rows, catcherYScaleAt16Rows, t);
            float dynamicYOffset = Mathf.Lerp(baseYOffsetAt8Rows, yOffsetAt16Rows, t);

            float bottomY = bottomAnchor.localPosition.y;

            for (int i = 0; i < catchersToUse; i++)
            {
                int index = startIndex + i;
                if (index < 0 || index >= totalCatchers) continue;

                Transform box = catchers[index];
                if (box == null) continue;

                box.gameObject.SetActive(true);

                float x = firstCatcherX + (i * xSpacing);
                float y = bottomY + dynamicYOffset;

                RectTransform catcherRect = box.GetComponent<RectTransform>();
                if (catcherRect != null)
                {
                    catcherRect.localPosition = new Vector2(x, y);
                    catcherRect.localScale = new Vector3(catcherXScale, catcherYScale, 1f);
                    catcherRect.localRotation = Quaternion.identity;
                }

                box.name = $"Catcher{i}";

                BallCatcher catcher = box.GetComponent<BallCatcher>();
                if (catcher != null)
                {
                    catcher.UpdateOriginalState();
                    catcher.SetCatcherPositionIndex(i, catchersToUse);
                }
            }
        }

        private void UpdateAllPegAnimationScales()
        {
            foreach (var anim in activePegAnimations)
            {
                if (anim != null)
                {
                    anim.UpdateOriginalPosition();
                    anim.UpdateOriginalScale();
                }
            }
        }

        private void OnDisable()
        {
            CleanupAllResources();
        }

        private void OnDestroy()
        {
            // Stop any ongoing rebuild
            if (rebuildCoroutine != null)
            {
                StopCoroutine(rebuildCoroutine);
            }

            // Kill all tweens
            DOTween.Kill(transform);
            foreach (var catcher in catchers)
            {
                if (catcher != null)
                {
                    DOTween.Kill(catcher);
                }
            }

            // Destroy all pegs
            foreach (var peg in pegPool)
            {
                if (peg != null)
                {
                    Destroy(peg);
                }
            }

            pegPool.Clear();
            activePegAnimations.Clear();
        }
    }
}