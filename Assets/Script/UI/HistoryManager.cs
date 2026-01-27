using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

namespace PlinkoGame
{
    /// <summary>
    /// Manages history panel animations and color matching
    /// NOW SUPPORTS DUAL LAYOUT SYSTEM
    /// Logic is shared, but UI elements can be rebound to active layout
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

        // Active layout references (updated on layout switch)
        private Transform activeHistoryContainer;
        private TextMeshProUGUI activeEmptyText;
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
            BindToLayout(null, null);
        }

        /// <summary>
        /// Called by UIManager when layout switches
        /// Rebinds to active layout's UI elements
        /// </summary>
        public void BindToLayout(Transform historyContainer, TextMeshProUGUI emptyText)
        {
            activeHistoryContainer = historyContainer;
            activeEmptyText = emptyText;

            // Determine which layout based on container
            bool isHorizontal = (historyContainer != null && historyContainer.name.Contains("Horizontal")) || historyContainer == null;

            if (isHorizontal)
            {
                BindToHorizontalLayout();
                activeBoardController = horizontalBoardController;
            }
            else
            {
                BindToVerticalLayout();
                activeBoardController = verticalBoardController;
            }

            StoreOriginalPositions();
            HideAllCards();

            // Rebuild visible cards with existing data
            RebuildHistoryDisplay();

            Debug.Log($"[HistoryManager] Bound to {(isHorizontal ? "HORIZONTAL" : "VERTICAL")} layout");
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
                pos3_centerSprite, pos3_nearCenter1Sprite, pos3_nearCenter2Sprite, pos3_midSprite,
                pos3_nearEdge2Sprite, pos3_nearEdge1Sprite, pos3_edgeSprite
            };
        }

        private void StoreOriginalPositions()
        {
            originalPositions = new List<Vector3>();
            foreach (RectTransform card in historyCards)
            {
                originalPositions.Add(card != null ? card.localPosition : Vector3.zero);
            }
        }

        private void HideAllCards()
        {
            foreach (RectTransform card in historyCards)
            {
                if (card != null)
                {
                    card.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Rebuilds history display when switching layouts
        /// Shows existing entries without animation
        /// </summary>
        private void RebuildHistoryDisplay()
        {
            for (int i = 0; i < 5; i++)
            {
                if (i < historyDataList.Count && historyCards[i] != null)
                {
                    historyCards[i].gameObject.SetActive(true);
                    historyCards[i].localPosition = originalPositions[i];

                    if (historyTexts[i] != null)
                    {
                        historyTexts[i].text = historyDataList[i].text;
                    }

                    UpdateCardSpriteForPosition(i, historyDataList[i].distanceFromCenter, false);
                }
                else if (historyCards[i] != null)
                {
                    historyCards[i].gameObject.SetActive(false);
                }
            }
        }

        public void AddEntry(double multiplier, double winAmount, int catcherIndex)
        {
            // Store data
            HistoryData newData = new HistoryData
            {
                text = FormatMultiplier(multiplier),
                distanceFromCenter = GetCatcherDistanceFromCenter(catcherIndex)
            };

            historyDataList.Insert(0, newData);

            // Keep only 4 entries (5th is overflow for animation)
            if (historyDataList.Count > 4)
            {
                historyDataList.RemoveAt(4);
            }

            AddHistoryEntry(multiplier, catcherIndex);
        }

        private void AddHistoryEntry(double multiplier, int catcherIndex)
        {
            // Kill any ongoing animations
            foreach (RectTransform card in historyCards)
            {
                if (card != null) card.DOKill();
            }

            int distanceFromCenter = GetCatcherDistanceFromCenter(catcherIndex);

            // Determine card height for offset
            float cardHeight = historyCards[0] != null ? historyCards[0].rect.height + entryOffset : entryOffset;

            // Track if position 4 needs restoration
            bool needsPosition4Restoration = (historyDataList.Count == 5);

            // STEP 1: Setup all cards at PREVIOUS positions (one up), including card 5 which will slide out
            for (int i = 0; i < 5; i++)
            {
                if (i < historyDataList.Count && historyCards[i] != null)
                {
                    historyCards[i].gameObject.SetActive(true);

                    // Set text
                    if (historyTexts[i] != null)
                    {
                        historyTexts[i].text = historyDataList[i].text;
                    }

                    // SPECIAL: Use transition sprite for card 4 when it's about to slide to position 5
                    if (i == 3 && needsPosition4Restoration)
                    {
                        // Card 4 will slide out, use transition sprite
                        UpdateCardSpriteForPosition(i, historyDataList[i].distanceFromCenter, true);
                    }
                    else
                    {
                        // Normal sprite
                        UpdateCardSpriteForPosition(i, historyDataList[i].distanceFromCenter, false);
                    }

                    if (i == 0)
                    {
                        // New card starts above position 1
                        Vector3 startPos = originalPositions[0];
                        startPos.y += cardHeight;
                        historyCards[0].localPosition = startPos;
                    }
                    else
                    {
                        // Existing cards start at PREVIOUS position (one up)
                        historyCards[i].localPosition = originalPositions[i - 1];
                    }
                }
                else if (historyCards[i] != null)
                {
                    historyCards[i].gameObject.SetActive(false);
                }
            }

            // STEP 2: Animate ALL cards sliding down to their final positions SIMULTANEOUSLY
            int animationsCompleted = 0;
            int totalAnimations = Mathf.Min(historyDataList.Count, 5);

            for (int i = 0; i < 5; i++)
            {
                if (i < historyDataList.Count && historyCards[i] != null)
                {
                    int cardIndex = i; // Capture for closure
                    historyCards[i].DOLocalMove(originalPositions[i], animationDuration)
                        .SetEase(animationEase)
                        .OnComplete(() =>
                        {
                            animationsCompleted++;

                            // When all animations complete
                            if (animationsCompleted >= totalAnimations)
                            {
                                // Restore position 4's original sprite
                                if (needsPosition4Restoration && historyCards[3] != null && historyDataList.Count >= 4)
                                {
                                    UpdateCardSpriteForPosition(3, historyDataList[3].distanceFromCenter, false);
                                }
                            }
                        });
                }
            }
        }

        private void UpdateCardSpriteForPosition(int positionIndex, int distanceFromCenter, bool useTransitionSprite = false)
        {
            if (historyBackgrounds[positionIndex] == null) return;
            if (positionIndex < 0 || positionIndex >= positionSprites.Count) return;
            if (distanceFromCenter < 0) return;

            Sprite selectedSprite;

            // Use transition sprite for position 4 when sliding out
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
        }

        private int GetCatcherDistanceFromCenter(int catcherIndex)
        {
            if (activeBoardController == null) return 0;

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

        private string FormatMultiplier(double multiplier)
        {
            if (multiplier >= 1000)
            {
                return $"{(multiplier / 1000):F1}K";
            }
            else if (multiplier >= 100)
            {
                return $"{multiplier:F0}x";
            }
            else if (multiplier % 1 == 0)
            {
                return $"{multiplier:F0}x";
            }
            else
            {
                return $"{multiplier:F1}x";
            }
        }

        public void ClearHistory()
        {
            foreach (RectTransform card in historyCards)
            {
                if (card != null)
                {
                    card.DOKill();
                    card.gameObject.SetActive(false);
                }
            }
            historyDataList.Clear();
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

