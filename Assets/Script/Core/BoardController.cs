using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

namespace PlinkoGame
{
    /// <summary>
    /// FIXED: Catcher POSITION aligned in gaps starting from peg[0]-peg[1]
    /// - Catchers positioned in gaps between pegs
    /// - Count: currentRows + 1 (UNCHANGED)
    /// - First catcher: gap between peg[0] and peg[1]
    /// - Size logic: UNCHANGED
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
        }

        public void UpdateCatcherMultipliers(List<double> multipliers)
        {
            int catchersToUse = currentRows + 1; // Number of catchers = rows + 1
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
                StartCoroutine(RebuildWithCanvasRefresh());
            }
        }

        private IEnumerator RebuildWithCanvasRefresh()
        {
            isRebuilding = true;

            Rebuild();
            yield return new WaitForEndOfFrame();

            if (mainCanvas != null)
            {
                Canvas.ForceUpdateCanvases();
                mainCanvas.enabled = false;
                mainCanvas.enabled = true;
                LayoutRebuilder.ForceRebuildLayoutImmediate(mainCanvas.GetComponent<RectTransform>());
            }

            if (ballLauncher != null)
                ballLauncher.OnBoardRebuilt();

            UpdateAllPegAnimationScales();
            isRebuilding = false;
        }

        private void Rebuild()
        {
            if (!pegPrefab || !topAnchor || !bottomAnchor || catchers.Count == 0)
                return;

            DisableAllPegs();
            GeneratePyramidAndCatchers();
            UpdateAllPegAnimationScales();
        }

        private void DisableAllPegs()
        {
            foreach (var peg in pegPool)
                peg.SetActive(false);
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
                    peg.transform.localPosition = new Vector2(startX + i * xSpacing, y);
                    peg.transform.localScale = Vector3.one * pegSize;
                    peg.SetActive(true);
                }
            }

            // Position catchers BETWEEN pegs (within boundaries)
            AlignCatchersInGaps(lastRowPegCount, center.x, xSpacing);
        }

        /// <summary>
        /// FIXED POSITION LOGIC: Catchers positioned in gaps starting from peg[0]-peg[1]
        /// - Count: currentRows + 1 (UNCHANGED)
        /// - First catcher: between peg[0] and peg[1]
        /// - Size logic: UNCHANGED
        /// </summary>
        private void AlignCatchersInGaps(int lastRowPegCount, float centerX, float xSpacing)
        {
            int catchersToUse = currentRows + 1; // Keep original count
            int totalCatchers = catchers.Count;

            // Disable all catchers first
            foreach (var c in catchers)
                c.gameObject.SetActive(false);

            int startIndex = (totalCatchers - catchersToUse) / 2;

            // FIX: Calculate first peg position, then place first catcher in gap
            float lastRowWidth = (lastRowPegCount - 1) * xSpacing;
            float leftMostPegX = centerX - lastRowWidth / 2f;

            // CORRECTED: Add spacing to start from gap between peg[0] and peg[1]
            float firstCatcherX = leftMostPegX + (xSpacing / 2f);

            // Size calculations (UNCHANGED from original)
            float t = Mathf.InverseLerp(8, 16, currentRows);
            float catcherXScale = Mathf.Lerp(catcherXScaleAt8Rows, catcherXScaleAt16Rows, t);
            float catcherYScale = Mathf.Lerp(catcherYScaleAt8Rows, catcherYScaleAt16Rows, t);
            float dynamicYOffset = Mathf.Lerp(baseYOffsetAt8Rows, yOffsetAt16Rows, t);

            float bottomY = bottomAnchor.localPosition.y;

            // Position catchers in gaps BETWEEN pegs
            for (int i = 0; i < catchersToUse; i++)
            {
                int index = startIndex + i;
                if (index < 0 || index >= totalCatchers) continue;

                Transform box = catchers[index];
                box.gameObject.SetActive(true);

                // POSITION: Starting from gap between peg[0] and peg[1]
                float x = firstCatcherX + (i * xSpacing);
                float y = bottomY + dynamicYOffset;

                box.localPosition = new Vector2(x, y);

                // Size (UNCHANGED)
                box.localScale = new Vector3(catcherXScale, catcherYScale, 1f);
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
            foreach (var peg in pegPool)
            {
                if (peg.activeSelf)
                {
                    PegHitAnimation anim = peg.GetComponent<PegHitAnimation>();
                    if (anim != null)
                    {
                        anim.UpdateOriginalPosition();
                        anim.UpdateOriginalScale();
                    }
                }
            }
        }

        private void OnDisable()
        {
            DisableAllPegs();
        }

        private void OnDestroy()
        {
            foreach (var peg in pegPool)
                if (peg) Destroy(peg);

            pegPool.Clear();
        }
    }
}