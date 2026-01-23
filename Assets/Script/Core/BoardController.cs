using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

namespace PlinkoGame
{
    /// <summary>
    /// Controls the Plinko board generation and catcher management
    /// Updated with proper encapsulation and multiplier display
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
        [SerializeField] private List<Transform> catchers; // 17 catcher objects

        [Header("Catcher Y Offset Scaling")]
        [SerializeField] private float baseYOffsetAt8Rows = -0.69f;
        [SerializeField] private float yOffsetStepPerRow = 0.09f;
        [SerializeField] private float baseCatcherHeight = 1f;
        [SerializeField] private float heightScaleFactor = 0.92f;

        [Header("UI")]
        [SerializeField] private Canvas mainCanvas;

        [Header("Limits")]
        [SerializeField] private float minScale = 0.4f;

        [Header("Ref")]
        [SerializeField] private BallLauncher ballLauncher;

        // Object Pool
        private List<GameObject> pegPool = new List<GameObject>();
        private int currentRows = 8;
        private bool isRebuilding;

        // Multiplier service reference
        private Services.MultiplierService multiplierService;

        private void Start()
        {
            multiplierService = new Services.MultiplierService();
            InitializePegPool();
            currentRows = 8;
            Rebuild();
        }

        // ============================================
        // INITIALIZATION
        // ============================================

        private void InitializePegPool()
        {
            if (!pegPrefab) return;

            // Calculate total pegs needed for 16 rows
            int totalPegsNeeded = 0;
            for (int row = 0; row < MAX_ROWS; row++)
            {
                totalPegsNeeded += startPegCount + row;
            }

            // Create all pegs and add to pool
            for (int i = 0; i < totalPegsNeeded; i++)
            {
                GameObject peg = Instantiate(pegPrefab, transform);
                peg.SetActive(false);
                pegPool.Add(peg);
            }

            Debug.Log($"Peg pool initialized with {totalPegsNeeded} pegs");
        }

        // ============================================
        // PUBLIC API
        // ============================================

        /// <summary>
        /// Set number of rows and rebuild board
        /// </summary>
        public void SetRows(int rows)
        {
            if (isRebuilding) return;

            currentRows = Mathf.Clamp(rows, 8, MAX_ROWS);
            StartCoroutine(RebuildWithCanvasRefresh());
        }

        /// <summary>
        /// Update catcher multiplier displays
        /// </summary>
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

            Debug.Log($"Updated {catchersToUse} catchers with multipliers");
        }

        public List<Transform> GetCatchers()
        {
            return catchers;
        }

        public int GetCurrentRows()
        {
            return currentRows;
        }

        // ============================================
        // BOARD GENERATION
        // ============================================

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
            // Notify ball launcher that board was rebuilt
            if (ballLauncher != null)
            {
                ballLauncher.OnBoardRebuilt();
            }
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
            foreach (GameObject peg in pegPool)
            {
                peg.SetActive(false);
            }
        }

        private void GeneratePyramidAndCatchers()
        {
            // Calculate width fit
            Bounds bounds = fitArea.bounds;
            float maxWidth = bounds.size.x * 0.95f;
            int lastRowPegCount = startPegCount + currentRows - 1;
            float requiredWidth = lastRowPegCount * baseXSpacing;
            float scale = Mathf.Max(maxWidth / requiredWidth, minScale);

            float pegSize = basePegSize * scale;
            float xSpacing = baseXSpacing * scale;

            // Calculate height fit
            float topY = topAnchor.position.y - topPadding;
            float bottomY = bottomAnchor.position.y;
            float usableHeight = Mathf.Abs(topY - bottomY);
            float ySpacing = usableHeight / (currentRows - 1);

            Vector2 center = new Vector2(topAnchor.position.x, topY);

            // Generate pegs from pool
            int poolIndex = 0;
            for (int row = 0; row < currentRows; row++)
            {
                int pegCount = startPegCount + row;
                float rowWidth = (pegCount - 1) * xSpacing;
                float startX = center.x - rowWidth / 2f;
                float y = center.y - row * ySpacing;

                for (int i = 0; i < pegCount; i++)
                {
                    if (poolIndex >= pegPool.Count)
                    {
                        Debug.LogWarning("Ran out of pegs in pool!");
                        break;
                    }

                    GameObject peg = pegPool[poolIndex];
                    poolIndex++;

                    Vector2 pos = new Vector2(startX + i * xSpacing, y);
                    peg.transform.position = pos;
                    peg.transform.localScale = Vector3.one * pegSize;
                    peg.SetActive(true);
                }
            }

            // Align catchers
            AlignCatchers(lastRowPegCount, center.x, xSpacing, pegSize);
        }

        private void AlignCatchers(int lastRowPegCount, float centerX, float xSpacing, float pegSize)
        {
            int needed = currentRows + 1;
            int total = catchers.Count;

            // Disable all catchers first
            for (int i = 0; i < total; i++)
            {
                catchers[i].gameObject.SetActive(false);
            }

            // Calculate active catcher range
            int startIndex = (total - needed) / 2;

            // Calculate catcher positions
            float pegRowSpan = (lastRowPegCount - 1) * xSpacing;
            float leftmostPegX = centerX - pegRowSpan / 2f;
            float rightmostPegX = centerX + pegRowSpan / 2f;

            float leftEdge = leftmostPegX;
            float rightEdge = rightmostPegX;
            float totalWidth = rightEdge - leftEdge;
            float catcherWidth = totalWidth / needed;

            // Calculate dynamic height and Y offset
            int rowsAboveBase = Mathf.Max(0, currentRows - 8);
            float dynamicHeight = baseCatcherHeight * Mathf.Pow(heightScaleFactor, rowsAboveBase);
            float dynamicYOffset = baseYOffsetAt8Rows + (rowsAboveBase * yOffsetStepPerRow);

            // Position active catchers
            for (int i = 0; i < needed; i++)
            {
                int index = startIndex + i;
                if (index < 0 || index >= total) continue;

                Transform box = catchers[index];
                box.gameObject.SetActive(true);

                float x = leftEdge + (i * catcherWidth) + (catcherWidth / 2f);
                float y = bottomAnchor.position.y + dynamicYOffset;

                box.position = new Vector2(x, y);
                box.localScale = new Vector3(catcherWidth * 0.88f, dynamicHeight, 1f);

                // Update catcher name for ball targeting
                box.name = $"Catcher{i}";

                // Update catcher's stored original state after positioning
                BallCatcher catcherScript = box.GetComponent<BallCatcher>();
                if (catcherScript != null)
                {
                    catcherScript.UpdateOriginalState();

                    // Set the catcher's position index for sprite updating
                    catcherScript.SetCatcherPositionIndex(i, needed);
                }
            }
        }

        private void UpdateAllPegAnimationScales()
        {
            foreach (GameObject peg in pegPool)
            {
                if (peg != null && peg.activeSelf)
                {
                    PegHitAnimation animation = peg.GetComponent<PegHitAnimation>();
                    if (animation != null)
                    {
                        animation.UpdateOriginalPosition();
                        animation.UpdateOriginalScale();
                    }
                }
            }

            Debug.Log("Updated original scales for all active pegs");
        }

        // ============================================
        // CLEANUP
        // ============================================

        private void OnDestroy()
        {
            foreach (GameObject peg in pegPool)
            {
                if (peg != null)
                {
                    Destroy(peg);
                }
            }
            pegPool.Clear();

            Debug.Log("Peg pool destroyed and cleared");
        }

        private void OnDisable()
        {
            DisableAllPegs();
        }
    }
}