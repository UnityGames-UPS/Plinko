using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

namespace PlinkoGame
{
    /// <summary>
    /// Manages history panel animations and color matching
    /// FIXED: Now correctly selects horizontal/vertical objects based on active layout
    /// FIXED: Uses exact multiplier values from backend (e.g., 0.48, 0.96)
    /// </summary>
    public class HistoryManager : MonoBehaviour
    {
        [Header("Horizontal Layout - History Card References")]
        [SerializeField] private RectTransform h_historyCard1;
        [SerializeField] private RectTransform h_historyCard2;
        [SerializeField] private RectTransform h_historyCard3;
        [SerializeField] private RectTransform h_historyCard4;
        [SerializeField] private RectTransform h_historyCard5;

        [Header("Horizontal Layout - Background Images")]
        [SerializeField] private Image h_historyBackground1;
        [SerializeField] private Image h_historyBackground2;
        [SerializeField] private Image h_historyBackground3;
        [SerializeField] private Image h_historyBackground4;
        [SerializeField] private Image h_historyBackground5;

        [Header("Horizontal Layout - Multiplier Texts")]
        [SerializeField] private TextMeshProUGUI h_historyText1;
        [SerializeField] private TextMeshProUGUI h_historyText2;
        [SerializeField] private TextMeshProUGUI h_historyText3;
        [SerializeField] private TextMeshProUGUI h_historyText4;
        [SerializeField] private TextMeshProUGUI h_historyText5;

        [Header("Vertical Layout - History Card References")]
        [SerializeField] private RectTransform v_historyCard1;
        [SerializeField] private RectTransform v_historyCard2;
        [SerializeField] private RectTransform v_historyCard3;
        [SerializeField] private RectTransform v_historyCard4;
        [SerializeField] private RectTransform v_historyCard5;

        [Header("Vertical Layout - Background Images")]
        [SerializeField] private Image v_historyBackground1;
        [SerializeField] private Image v_historyBackground2;
        [SerializeField] private Image v_historyBackground3;
        [SerializeField] private Image v_historyBackground4;
        [SerializeField] private Image v_historyBackground5;

        [Header("Vertical Layout - Multiplier Texts")]
        [SerializeField] private TextMeshProUGUI v_historyText1;
        [SerializeField] private TextMeshProUGUI v_historyText2;
        [SerializeField] private TextMeshProUGUI v_historyText3;
        [SerializeField] private TextMeshProUGUI v_historyText4;
        [SerializeField] private TextMeshProUGUI v_historyText5;

        [Header("Position 1 Sprites (Center to Edge)")]
        [SerializeField] private Sprite pos1_centerSprite;
        [SerializeField] private Sprite pos1_nearCenter1Sprite;
        [SerializeField] private Sprite pos1_nearCenter2Sprite;
        [SerializeField] private Sprite pos1_midSprite;
        [SerializeField] private Sprite pos1_nearEdge2Sprite;
        [SerializeField] private Sprite pos1_nearEdge1Sprite;
        [SerializeField] private Sprite pos1_edgeSprite;

        [Header("Position 2 Sprites (Center to Edge)")]
        [SerializeField] private Sprite pos2_centerSprite;
        [SerializeField] private Sprite pos2_nearCenter1Sprite;
        [SerializeField] private Sprite pos2_nearCenter2Sprite;
        [SerializeField] private Sprite pos2_midSprite;
        [SerializeField] private Sprite pos2_nearEdge2Sprite;
        [SerializeField] private Sprite pos2_nearEdge1Sprite;
        [SerializeField] private Sprite pos2_edgeSprite;

        [Header("Position 3 Sprites (Center to Edge)")]
        [SerializeField] private Sprite pos3_centerSprite;
        [SerializeField] private Sprite pos3_nearCenter1Sprite;
        [SerializeField] private Sprite pos3_nearCenter2Sprite;
        [SerializeField] private Sprite pos3_midSprite;
        [SerializeField] private Sprite pos3_nearEdge2Sprite;
        [SerializeField] private Sprite pos3_nearEdge1Sprite;
        [SerializeField] private Sprite pos3_edgeSprite;

        [Header("Position 4 Sprites (Center to Edge)")]
        [SerializeField] private Sprite pos4_centerSprite;
        [SerializeField] private Sprite pos4_nearCenter1Sprite;
        [SerializeField] private Sprite pos4_nearCenter2Sprite;
        [SerializeField] private Sprite pos4_midSprite;
        [SerializeField] private Sprite pos4_nearEdge2Sprite;
        [SerializeField] private Sprite pos4_nearEdge1Sprite;
        [SerializeField] private Sprite pos4_edgeSprite;

        [Header("Position 5 Sprites (Hidden Overflow - Center to Edge)")]
        [SerializeField] private Sprite pos5_centerSprite;
        [SerializeField] private Sprite pos5_nearCenter1Sprite;
        [SerializeField] private Sprite pos5_nearCenter2Sprite;
        [SerializeField] private Sprite pos5_midSprite;
        [SerializeField] private Sprite pos5_nearEdge2Sprite;
        [SerializeField] private Sprite pos5_nearEdge1Sprite;
        [SerializeField] private Sprite pos5_edgeSprite;

        [Header("Board References")]
        [SerializeField] private BoardController horizontalBoardController;
        [SerializeField] private BoardController verticalBoardController;

        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.5f;
        [SerializeField] private float entryOffset = 100f;
        [SerializeField] private Ease animationEase = Ease.OutCubic;

        // Active layout tracking
        private bool isHorizontalLayout = true;
        private BoardController activeBoardController;

        // Active lists for current layout
        private List<RectTransform> historyCards;
        private List<Image> historyBackgrounds;
        private List<TextMeshProUGUI> historyTexts;
        private List<Vector3> originalPositions;
        private List<List<Sprite>> positionSprites;
        private List<Sprite> position4TransitionSprites;

        // Store history data separately (shared between layouts)
        private class HistoryData
        {
            public string text;
            public int distanceFromCenter;
        }
        private List<HistoryData> historyDataList = new List<HistoryData>();

        private void Awake()
        {
            InitializeSpriteArrays();

            // Start with horizontal layout
            BindToLayout(true);

            Debug.Log("[HistoryManager] Awake complete");
        }

        /// <summary>
        /// Called by UIManager when layout switches
        /// Rebinds to active layout's UI elements based on isHorizontal parameter
        /// </summary>
        public void BindToLayout(bool isHorizontal)
        {
            isHorizontalLayout = isHorizontal;

            Debug.Log($"[HistoryManager] BindToLayout called - isHorizontal: {isHorizontal}");

            if (isHorizontal)
            {
                BindToHorizontalLayout();
                activeBoardController = horizontalBoardController;
                Debug.Log("[HistoryManager] Bound to HORIZONTAL layout");
            }
            else
            {
                BindToVerticalLayout();
                activeBoardController = verticalBoardController;
                Debug.Log("[HistoryManager] Bound to VERTICAL layout");
            }

            StoreOriginalPositions();
            HideAllCards();

            // Rebuild visible cards with existing data
            RebuildHistoryDisplay();

            Debug.Log($"[HistoryManager] Layout binding complete with {historyCards.Count} cards");
        }

        private void BindToHorizontalLayout()
        {
            historyCards = new List<RectTransform>
            {
                h_historyCard1, h_historyCard2, h_historyCard3, h_historyCard4, h_historyCard5
            };

            historyBackgrounds = new List<Image>
            {
                h_historyBackground1, h_historyBackground2, h_historyBackground3, h_historyBackground4, h_historyBackground5
            };

            historyTexts = new List<TextMeshProUGUI>
            {
                h_historyText1, h_historyText2, h_historyText3, h_historyText4, h_historyText5
            };

            Debug.Log("[HistoryManager] Horizontal objects bound");
        }

        private void BindToVerticalLayout()
        {
            historyCards = new List<RectTransform>
            {
                v_historyCard1, v_historyCard2, v_historyCard3, v_historyCard4, v_historyCard5
            };

            historyBackgrounds = new List<Image>
            {
                v_historyBackground1, v_historyBackground2, v_historyBackground3, v_historyBackground4, v_historyBackground5
            };

            historyTexts = new List<TextMeshProUGUI>
            {
                v_historyText1, v_historyText2, v_historyText3, v_historyText4, v_historyText5
            };

            Debug.Log("[HistoryManager] Vertical objects bound");
        }

        private void InitializeSpriteArrays()
        {
            positionSprites = new List<List<Sprite>>
            {
                new List<Sprite> { pos1_centerSprite, pos1_nearCenter1Sprite, pos1_nearCenter2Sprite, pos1_midSprite, pos1_nearEdge2Sprite, pos1_nearEdge1Sprite, pos1_edgeSprite },
                new List<Sprite> { pos2_centerSprite, pos2_nearCenter1Sprite, pos2_nearCenter2Sprite, pos2_midSprite, pos2_nearEdge2Sprite, pos2_nearEdge1Sprite, pos2_edgeSprite },
                new List<Sprite> { pos3_centerSprite, pos3_nearCenter1Sprite, pos3_nearCenter2Sprite, pos3_midSprite, pos3_nearEdge2Sprite, pos3_nearEdge1Sprite, pos3_edgeSprite },
                new List<Sprite> { pos4_centerSprite, pos4_nearCenter1Sprite, pos4_nearCenter2Sprite, pos4_midSprite, pos4_nearEdge2Sprite, pos4_nearEdge1Sprite, pos4_edgeSprite },
                new List<Sprite> { pos5_centerSprite, pos5_nearCenter1Sprite, pos5_nearCenter2Sprite, pos5_midSprite, pos5_nearEdge2Sprite, pos5_nearEdge1Sprite, pos5_edgeSprite }
            };

            position4TransitionSprites = new List<Sprite>
            {
                pos5_centerSprite, pos5_nearCenter1Sprite, pos5_nearCenter2Sprite, pos5_midSprite, pos5_nearEdge2Sprite, pos5_nearEdge1Sprite, pos5_edgeSprite
            };
        }

        private void StoreOriginalPositions()
        {
            if (historyCards == null || historyCards.Count == 0) return;

            originalPositions = new List<Vector3>();

            foreach (RectTransform card in historyCards)
            {
                if (card != null)
                {
                    originalPositions.Add(card.localPosition);
                }
                else
                {
                    originalPositions.Add(Vector3.zero);
                }
            }

            Debug.Log($"[HistoryManager] Stored {originalPositions.Count} original positions");
        }

        private void HideAllCards()
        {
            if (historyCards == null) return;

            foreach (RectTransform card in historyCards)
            {
                if (card != null)
                {
                    card.DOKill();
                    card.gameObject.SetActive(false);
                }
            }
        }

        private void RebuildHistoryDisplay()
        {
            if (historyDataList.Count == 0) return;

            int visibleCount = Mathf.Min(4, historyDataList.Count);

            for (int i = 0; i < visibleCount; i++)
            {
                HistoryData data = historyDataList[i];

                if (i < historyCards.Count && historyCards[i] != null)
                {
                    historyCards[i].gameObject.SetActive(true);
                    historyCards[i].localPosition = originalPositions[i];

                    if (i < historyTexts.Count && historyTexts[i] != null)
                    {
                        historyTexts[i].text = data.text;
                    }

                    UpdateCardSpriteForPosition(i, data.distanceFromCenter);
                }
            }

            Debug.Log($"[HistoryManager] Rebuilt {visibleCount} visible cards");
        }

        internal void AddToHistory(double multiplier, double winAmount, int catcherIndex)
        {
            if (historyCards == null || historyCards.Count == 0)
            {
                Debug.LogWarning("[HistoryManager] History cards not initialized");
                return;
            }

            int distanceFromCenter = GetCatcherDistanceFromCenter(catcherIndex);
            string multiplierText = FormatMultiplierExact(multiplier);

            HistoryData newData = new HistoryData
            {
                text = multiplierText,
                distanceFromCenter = distanceFromCenter
            };

            historyDataList.Insert(0, newData);

            if (historyDataList.Count > 5)
            {
                historyDataList.RemoveAt(5);
            }

            AnimateHistoryUpdate();

            Debug.Log($"[HistoryManager] Added to history: {multiplierText}, Layout: {(isHorizontalLayout ? "HORIZONTAL" : "VERTICAL")}");
        }

        private void AnimateHistoryUpdate()
        {
            if (historyCards == null || historyCards.Count == 0 || originalPositions.Count == 0)
            {
                Debug.LogWarning("[HistoryManager] Cannot animate - cards or positions not initialized");
                return;
            }

            int visibleCount = Mathf.Min(4, historyDataList.Count);
            bool needsPosition4Restoration = historyDataList.Count > 4;

            int totalAnimations = visibleCount + (needsPosition4Restoration ? 1 : 0);
            int animationsCompleted = 0;

            // Apply transition sprite to position 4 if it needs to slide out
            if (needsPosition4Restoration && historyCards.Count > 3 && historyCards[3] != null)
            {
                if (historyDataList.Count >= 5)
                {
                    UpdateCardSpriteForPosition(3, historyDataList[4].distanceFromCenter, true);
                }
            }

            // Animate positions 1-4
            for (int i = 0; i < visibleCount; i++)
            {
                if (i >= historyCards.Count || historyCards[i] == null) continue;

                RectTransform card = historyCards[i];
                HistoryData data = historyDataList[i];

                card.DOKill();

                if (i == 0)
                {
                    // New card entry from side
                    Vector3 startPos = originalPositions[0];
                    startPos.x -= entryOffset;

                    card.localPosition = startPos;
                    card.gameObject.SetActive(true);

                    if (i < historyTexts.Count && historyTexts[i] != null)
                    {
                        historyTexts[i].text = data.text;
                    }

                    UpdateCardSpriteForPosition(0, data.distanceFromCenter);

                    card.DOLocalMove(originalPositions[0], animationDuration)
                        .SetEase(animationEase)
                        .OnComplete(() =>
                        {
                            animationsCompleted++;
                            Debug.Log($"[HistoryManager] Animation {animationsCompleted}/{totalAnimations} completed");

                            if (animationsCompleted >= totalAnimations)
                            {
                                if (needsPosition4Restoration && historyCards.Count > 3 && historyCards[3] != null && historyDataList.Count >= 4)
                                {
                                    UpdateCardSpriteForPosition(3, historyDataList[3].distanceFromCenter, false);
                                }

                                Debug.Log("[HistoryManager] All animations complete!");
                            }
                        });
                }
                else
                {
                    // Existing cards shift down
                    if (!card.gameObject.activeSelf)
                    {
                        card.gameObject.SetActive(true);
                    }

                    if (i < historyTexts.Count && historyTexts[i] != null)
                    {
                        historyTexts[i].text = data.text;
                    }

                    UpdateCardSpriteForPosition(i, data.distanceFromCenter);

                    card.DOLocalMove(originalPositions[i], animationDuration)
                        .SetEase(animationEase)
                        .OnComplete(() =>
                        {
                            animationsCompleted++;
                            Debug.Log($"[HistoryManager] Animation {animationsCompleted}/{totalAnimations} completed");

                            if (animationsCompleted >= totalAnimations)
                            {
                                if (needsPosition4Restoration && historyCards.Count > 3 && historyCards[3] != null && historyDataList.Count >= 4)
                                {
                                    UpdateCardSpriteForPosition(3, historyDataList[3].distanceFromCenter, false);
                                    Debug.Log("[HistoryManager] Restored position 4 sprite");
                                }

                                Debug.Log("[HistoryManager] All animations complete!");
                            }
                        });
                }
            }

            // Slide out overflow card (position 5)
            if (historyDataList.Count > 4 && historyCards.Count > 4 && historyCards[4] != null)
            {
                RectTransform overflowCard = historyCards[4];
                HistoryData overflowData = historyDataList[4];

                if (!overflowCard.gameObject.activeSelf)
                {
                    overflowCard.gameObject.SetActive(true);
                }

                overflowCard.localPosition = originalPositions[3];

                if (historyTexts.Count > 4 && historyTexts[4] != null)
                {
                    historyTexts[4].text = overflowData.text;
                }

                UpdateCardSpriteForPosition(4, overflowData.distanceFromCenter);

                Vector3 exitPos = originalPositions[4];
                exitPos.x += entryOffset;

                overflowCard.DOLocalMove(exitPos, animationDuration)
                    .SetEase(animationEase)
                    .OnComplete(() =>
                    {
                        overflowCard.gameObject.SetActive(false);
                        animationsCompleted++;
                        Debug.Log($"[HistoryManager] Animation {animationsCompleted}/{totalAnimations} completed");

                        if (animationsCompleted >= totalAnimations)
                        {
                            if (needsPosition4Restoration && historyCards.Count > 3 && historyCards[3] != null && historyDataList.Count >= 4)
                            {
                                UpdateCardSpriteForPosition(3, historyDataList[3].distanceFromCenter, false);
                                Debug.Log("[HistoryManager] Restored position 4 sprite");
                            }

                            Debug.Log("[HistoryManager] All animations complete!");
                        }
                    });
            }
        }

        private void UpdateCardSpriteForPosition(int positionIndex, int distanceFromCenter, bool useTransitionSprite = false)
        {
            if (historyBackgrounds == null || positionIndex >= historyBackgrounds.Count || historyBackgrounds[positionIndex] == null)
            {
                Debug.LogWarning($"[HistoryManager] Cannot update sprite - background {positionIndex} is null");
                return;
            }

            if (positionIndex < 0 || positionIndex >= positionSprites.Count)
            {
                Debug.LogWarning($"[HistoryManager] Invalid position index: {positionIndex}");
                return;
            }

            if (distanceFromCenter < 0)
            {
                Debug.LogWarning($"[HistoryManager] Invalid distance from center: {distanceFromCenter}");
                return;
            }

            Sprite selectedSprite;

            if (positionIndex == 3 && useTransitionSprite)
            {
                selectedSprite = GetTransitionSpriteForDistance(distanceFromCenter);
            }
            else
            {
                selectedSprite = GetSpriteForDistance(positionIndex, distanceFromCenter);
            }

            if (selectedSprite != null)
            {
                historyBackgrounds[positionIndex].sprite = selectedSprite;
            }
            else
            {
                Debug.LogWarning($"[HistoryManager] No sprite found for position {positionIndex}, distance {distanceFromCenter}");
            }
        }

        private int GetCatcherDistanceFromCenter(int catcherIndex)
        {
            if (activeBoardController == null)
            {
                Debug.LogWarning("[HistoryManager] activeBoardController is null, returning distance 0");
                return 0;
            }

            int totalCatchers = activeBoardController.GetCurrentRows() + 1;

            if (totalCatchers % 2 == 1)
            {
                int centerIndex = totalCatchers / 2;
                return Mathf.Abs(catcherIndex - centerIndex);
            }
            else
            {
                int leftCenterIndex = totalCatchers / 2 - 1;
                int rightCenterIndex = totalCatchers / 2;

                if (catcherIndex <= leftCenterIndex)
                {
                    return leftCenterIndex - catcherIndex;
                }
                else
                {
                    return catcherIndex - rightCenterIndex;
                }
            }
        }

        private Sprite GetSpriteForDistance(int historyPosition, int distance)
        {
            if (historyPosition < 0 || historyPosition >= positionSprites.Count)
                return null;

            List<Sprite> sprites = positionSprites[historyPosition];

            switch (distance)
            {
                case 0: return sprites[0];
                case 1: return sprites[1] != null ? sprites[1] : sprites[0];
                case 2: return sprites[2] != null ? sprites[2] : sprites[0];
                case 3: return sprites[3] != null ? sprites[3] : sprites[0];
                case 4: return sprites[4] != null ? sprites[4] : sprites[0];
                case 5: return sprites[5] != null ? sprites[5] : sprites[0];
                default: return sprites[6] != null ? sprites[6] : sprites[0];
            }
        }

        private Sprite GetTransitionSpriteForDistance(int distance)
        {
            if (position4TransitionSprites == null || position4TransitionSprites.Count == 0)
                return null;

            switch (distance)
            {
                case 0: return position4TransitionSprites[0];
                case 1: return position4TransitionSprites[1] != null ? position4TransitionSprites[1] : position4TransitionSprites[0];
                case 2: return position4TransitionSprites[2] != null ? position4TransitionSprites[2] : position4TransitionSprites[0];
                case 3: return position4TransitionSprites[3] != null ? position4TransitionSprites[3] : position4TransitionSprites[0];
                case 4: return position4TransitionSprites[4] != null ? position4TransitionSprites[4] : position4TransitionSprites[0];
                case 5: return position4TransitionSprites[5] != null ? position4TransitionSprites[5] : position4TransitionSprites[0];
                default: return position4TransitionSprites[6] != null ? position4TransitionSprites[6] : position4TransitionSprites[0];
            }
        }

        /// <summary>
        /// FIXED: Format multiplier to show EXACT values from backend
        /// Shows up to 2 decimal places (e.g., 0.48, 0.96, 1.06)
        /// </summary>
        private string FormatMultiplierExact(double multiplier)
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

        public void ClearHistory()
        {
            if (historyCards == null) return;

            foreach (RectTransform card in historyCards)
            {
                if (card != null)
                {
                    card.DOKill();
                    card.gameObject.SetActive(false);
                }
            }
            historyDataList.Clear();

            Debug.Log("[HistoryManager] History cleared");
        }

        private void OnDestroy()
        {
            // Clean up both layouts
            if (h_historyCard1 != null) h_historyCard1.DOKill();
            if (h_historyCard2 != null) h_historyCard2.DOKill();
            if (h_historyCard3 != null) h_historyCard3.DOKill();
            if (h_historyCard4 != null) h_historyCard4.DOKill();
            if (h_historyCard5 != null) h_historyCard5.DOKill();

            if (v_historyCard1 != null) v_historyCard1.DOKill();
            if (v_historyCard2 != null) v_historyCard2.DOKill();
            if (v_historyCard3 != null) v_historyCard3.DOKill();
            if (v_historyCard4 != null) v_historyCard4.DOKill();
            if (v_historyCard5 != null) v_historyCard5.DOKill();
        }
    }
}