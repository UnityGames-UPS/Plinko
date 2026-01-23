using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

namespace PlinkoGame
{
    /// <summary>
    /// Manages history panel animations and color matching
    /// Separate from UIManager - handles only history display
    /// Now includes 5th overflow card for seamless push animations
    /// Includes transition sprites for position 4 to avoid visual discontinuity
    /// After animation, position 4 returns to its original sprite
    /// </summary>
    public class HistoryManager : MonoBehaviour
    {
        [Header("History Card References")]
        [SerializeField] private RectTransform historyCard1;
        [SerializeField] private RectTransform historyCard2;
        [SerializeField] private RectTransform historyCard3;
        [SerializeField] private RectTransform historyCard4;
        [SerializeField] private RectTransform historyCard5; // Hidden overflow card for smooth animation

        [Header("Background Images")]
        [SerializeField] private Image historyBackground1;
        [SerializeField] private Image historyBackground2;
        [SerializeField] private Image historyBackground3;
        [SerializeField] private Image historyBackground4;
        [SerializeField] private Image historyBackground5; // Hidden overflow image

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

        [Header("Multiplier Texts")]
        [SerializeField] private TextMeshProUGUI historyText1;
        [SerializeField] private TextMeshProUGUI historyText2;
        [SerializeField] private TextMeshProUGUI historyText3;
        [SerializeField] private TextMeshProUGUI historyText4;
        [SerializeField] private TextMeshProUGUI historyText5; // Hidden overflow text

        [Header("Catcher Color References")]
        [SerializeField] private List<Transform> catcherReferences;
        [SerializeField] private BoardController boardController;

        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.5f;
        [SerializeField] private float entryOffset = 100f;
        [SerializeField] private Ease animationEase = Ease.OutCubic;

        // Internal lists for easier management
        private List<RectTransform> historyCards;
        private List<Image> historyBackgrounds;
        private List<TextMeshProUGUI> historyTexts;
        private List<Vector3> originalPositions;
        private List<List<Sprite>> positionSprites;
        private List<Sprite> position4TransitionSprites;

        // Store history data separately
        private class HistoryData
        {
            public string text;
            public int distanceFromCenter;
        }
        private List<HistoryData> historyDataList = new List<HistoryData>();

        private void Awake()
        {
            InitializeLists();
            InitializeSpriteArrays();
            StoreOriginalPositions();
            HideAllCards();
        }

        private void InitializeLists()
        {
            historyCards = new List<RectTransform>
            {
                historyCard1,
                historyCard2,
                historyCard3,
                historyCard4,
                historyCard5 // Include the 5th hidden card
            };

            historyBackgrounds = new List<Image>
            {
                historyBackground1,
                historyBackground2,
                historyBackground3,
                historyBackground4,
                historyBackground5 // Include the 5th hidden background
            };

            historyTexts = new List<TextMeshProUGUI>
            {
                historyText1,
                historyText2,
                historyText3,
                historyText4,
                historyText5 // Include the 5th hidden text
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
            position4TransitionSprites = new List<Sprite> { pos3_centerSprite, pos3_nearCenter1Sprite, pos3_nearCenter2Sprite, pos3_midSprite, pos3_nearEdge2Sprite, pos3_nearEdge1Sprite, pos3_edgeSprite };
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

        public void AddHistoryEntry(double multiplier, int catcherIndex)
        {
            // Kill any ongoing animations
            foreach (RectTransform card in historyCards)
            {
                if (card != null) card.DOKill();
            }

            int distanceFromCenter = GetCatcherDistanceFromCenter(catcherIndex);

            // Add new data at the beginning
            HistoryData newData = new HistoryData
            {
                text = $"{multiplier:F1}x",
                distanceFromCenter = distanceFromCenter
            };
            historyDataList.Insert(0, newData);

            // Keep only 5 entries (4 visible + 1 for smooth animation)
            if (historyDataList.Count > 5)
            {
                historyDataList.RemoveAt(5);
            }

            // Animate slide-in push effect
            AnimateSlideInPush();
        }

        private void AnimateSlideInPush()
        {
            // Calculate the distance between positions (height of one card)
            float cardHeight = 0f;
            if (originalPositions.Count >= 2)
            {
                cardHeight = originalPositions[0].y - originalPositions[1].y;
            }

            // Track if we need to restore position 4 sprite after animation
            bool needsPosition4Restoration = historyDataList.Count > 4;
            int position4Distance = needsPosition4Restoration ? historyDataList[3].distanceFromCenter : 0;

            // STEP 1: Position all 5 cards based on current data BEFORE animation
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
            if (boardController == null) return 0;

            int totalCatchers = boardController.GetCurrentRows() + 1;

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

        public void UpdateCatcherReferences(List<Transform> newCatchers)
        {
            catcherReferences = newCatchers;
        }

        public void SetBoardController(BoardController controller)
        {
            boardController = controller;
        }

        private void OnDestroy()
        {
            foreach (RectTransform card in historyCards)
            {
                if (card != null) card.DOKill();
            }
        }
    }
}