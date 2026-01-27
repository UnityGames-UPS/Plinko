using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;

namespace PlinkoGame
{
    /// <summary>
    /// Handles individual catcher behavior, animation, and hover interactions
    /// FIXED: Animation cooldown prevents multiple simultaneous animations
    /// </summary>
    public class BallCatcher : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Catch Impact Animation")]
        [SerializeField] private float dipAmount = 0.12f;
        [SerializeField] private float impactDuration = 0.10f;
        [SerializeField] private float settleDuration = 0.16f;
        [SerializeField] private float reversePopScale = 0.92f;
        [SerializeField] private float animationCooldown = 0.3f; // Cooldown between animations

        [Header("Multiplier Display")]
        [SerializeField] private TextMeshProUGUI multiplierText;

        [Header("Catcher Sprites (Center to Edge)")]
        [SerializeField] private Sprite centerSprite;
        [SerializeField] private Sprite nearCenter1Sprite;
        [SerializeField] private Sprite nearCenter2Sprite;
        [SerializeField] private Sprite midSprite;
        [SerializeField] private Sprite nearEdge2Sprite;
        [SerializeField] private Sprite nearEdge1Sprite;
        [SerializeField] private Sprite edgeSprite;

        [Header("References")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private GameManager gameManager;

        private RectTransform rectTransform;
        private Image catcherImage;
        private float multiplierValue = 1f;
        private bool isAnimating = false;
        private float lastAnimationTime = -999f; // Track last animation time
        private int ballsInCooldown = 0; // Count balls caught during cooldown
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private int catcherIndexInRow = -1;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            catcherImage = GetComponent<Image>();
        }

        private void Start()
        {
            if (rectTransform != null)
            {
                originalPosition = rectTransform.localPosition;
                originalScale = rectTransform.localScale;
            }
        }

        private int GetCurrentCatcherIndex()
        {
            string catcherName = gameObject.name;
            string numberPart = catcherName.Replace("Catchers", "").Replace("Catcher", "").Trim();

            if (int.TryParse(numberPart, out int index))
            {
                return index;
            }

            return -1;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Ball")) return;

            collision.gameObject.SetActive(false);

            // AUDIO: Play ball catch sound
            AudioManager.Instance?.PlayBallCatch();

            float timeSinceLastAnimation = Time.time - lastAnimationTime;

            // Only play animation if cooldown has passed
            if (timeSinceLastAnimation >= animationCooldown && !isAnimating)
            {
                PlayCatchAnimation();
                ballsInCooldown = 0; // Reset counter
            }
            else
            {
                // Count balls caught during cooldown (for debugging)
                ballsInCooldown++;
            }
        }

        private void PlayCatchAnimation()
        {
            if (rectTransform == null) return;

            rectTransform.DOKill();
            rectTransform.localPosition = originalPosition;
            rectTransform.localScale = originalScale;

            isAnimating = true;
            lastAnimationTime = Time.time;

            Sequence seq = DOTween.Sequence();

            seq.Append(
                rectTransform.DOLocalMoveY(originalPosition.y - dipAmount, impactDuration)
                    .SetEase(Ease.OutQuad)
            );

            seq.Join(
                rectTransform.DOScale(originalScale * reversePopScale, impactDuration)
                    .SetEase(Ease.OutQuad)
            );

            seq.Append(
                rectTransform.DOLocalMoveY(originalPosition.y, settleDuration)
                    .SetEase(Ease.OutQuad)
            );

            seq.Join(
                rectTransform.DOScale(originalScale, settleDuration)
                    .SetEase(Ease.OutQuad)
            );

            seq.OnComplete(() =>
            {
                rectTransform.localPosition = originalPosition;
                rectTransform.localScale = originalScale;
                isAnimating = false;
            });
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsMobilePlatform() && gameObject.activeSelf)
            {
                AudioManager.Instance?.PlayHoverSound();
                ShowHoverPopup();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsMobilePlatform() && gameObject.activeSelf)
            {
                if (uiManager != null)
                {
                    uiManager.ToggleHoverPopup(transform.position, GetProfit(), GetProbability());
                }
            }
        }

        private void ShowHoverPopup()
        {
            if (uiManager == null || gameManager == null) return;

            int catcherIndex = GetCurrentCatcherIndex();
            if (catcherIndex < 0) return;

            double profit = GetProfit();
            double probability = GetProbability();

            uiManager.ShowHoverPopup(transform.position, profit, probability);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!IsMobilePlatform())
            {
                HideHoverPopup();
            }
        }

        private void HideHoverPopup()
        {
            if (uiManager != null)
            {
                uiManager.HideHoverPopup();
            }
        }

        private double GetProfit()
        {
            if (gameManager == null) return 0;

            int catcherIndex = GetCurrentCatcherIndex();
            if (catcherIndex < 0) return 0;

            double betAmount = gameManager.GetCurrentBetAmount();
            double multiplier = gameManager.GetMultiplierForCatcher(catcherIndex);
            double winAmount = betAmount * multiplier;
            double profit = winAmount - betAmount;

            return profit;
        }

        private double GetProbability()
        {
            if (gameManager == null) return 0;

            int catcherIndex = GetCurrentCatcherIndex();
            if (catcherIndex < 0) return 0;

            return gameManager.GetProbabilityForCatcher(catcherIndex);
        }

        private bool IsMobilePlatform()
        {
#if UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
            return Input.touchSupported;
#else
            return false;
#endif
        }

        internal void SetMultiplier(float value)
        {
            multiplierValue = value;

            if (multiplierText != null)
            {
                multiplierText.text = FormatMultiplierExact(value);
            }
        }

        internal float GetMultiplier()
        {
            return multiplierValue;
        }

        /// <summary>
        /// Format multiplier to show exact values from backend
        /// Shows up to 2 decimal places
        /// </summary>
        private string FormatMultiplierExact(float value)
        {
            if (value >= 1000)
            {
                double thousands = value / 1000.0;
                string formatted = thousands.ToString("0.##");
                return $"{formatted}K";
            }
            else
            {
                string formatted = value.ToString("0.##");
                return $"{formatted}x";
            }
        }

        internal void SetCatcherPositionIndex(int indexInRow, int totalCatchers)
        {
            catcherIndexInRow = indexInRow;
            UpdateSpriteBasedOnPosition(totalCatchers);
        }

        private void UpdateSpriteBasedOnPosition(int totalCatchers)
        {
            if (catcherImage == null) return;

            int distanceFromCenter;

            if (totalCatchers % 2 == 1)
            {
                int centerIndex = totalCatchers / 2;
                distanceFromCenter = Mathf.Abs(catcherIndexInRow - centerIndex);
            }
            else
            {
                int leftCenterIndex = totalCatchers / 2 - 1;
                int rightCenterIndex = totalCatchers / 2;

                if (catcherIndexInRow <= leftCenterIndex)
                {
                    distanceFromCenter = leftCenterIndex - catcherIndexInRow;
                }
                else
                {
                    distanceFromCenter = catcherIndexInRow - rightCenterIndex;
                }
            }

            Sprite newSprite = GetSpriteForDistance(distanceFromCenter);

            if (newSprite != null)
            {
                catcherImage.sprite = newSprite;
            }
        }

        private Sprite GetSpriteForDistance(int distance)
        {
            switch (distance)
            {
                case 0:
                    return centerSprite;
                case 1:
                    return nearCenter1Sprite != null ? nearCenter1Sprite : centerSprite;
                case 2:
                    return nearCenter2Sprite != null ? nearCenter2Sprite : centerSprite;
                case 3:
                    return midSprite != null ? midSprite : centerSprite;
                case 4:
                    return nearEdge2Sprite != null ? nearEdge2Sprite : centerSprite;
                case 5:
                    return nearEdge1Sprite != null ? nearEdge1Sprite : centerSprite;
                default:
                    return edgeSprite != null ? edgeSprite : centerSprite;
            }
        }

        internal void ResetState()
        {
            rectTransform?.DOKill();

            if (rectTransform != null)
            {
                rectTransform.localPosition = originalPosition;
                rectTransform.localScale = originalScale;
            }

            isAnimating = false;
            ballsInCooldown = 0;
            lastAnimationTime = -999f;
        }

        internal void UpdateOriginalState()
        {
            if (rectTransform != null)
            {
                originalPosition = rectTransform.localPosition;
                originalScale = rectTransform.localScale;
            }
        }

        private void OnDestroy()
        {
            rectTransform?.DOKill();
        }
    }
}