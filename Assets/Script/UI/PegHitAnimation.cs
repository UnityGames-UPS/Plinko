using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// Peg impact animation with shake effect
/// </summary>
public class PegHitAnimation : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float shakeStrength = 0.05f;
    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private int shakeVibrato = 10;

    [Header("Pop Effect Settings")]
    [SerializeField] private Image popEffectImage;
    [SerializeField] private float popScaleMultiplier = 1.2f;
    [SerializeField] private float popFadeDuration = 0.4f;
    [SerializeField] private float popScaleDuration = 0.3f;
    [SerializeField] private AnimationCurve popScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private RectTransform rectTransform;
    private RectTransform popRectTransform;
    private Vector3 originalPosition;
    private Vector3 originalScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (popEffectImage != null)
        {
            popRectTransform = popEffectImage.GetComponent<RectTransform>();
        }
    }

    private void Start()
    {
        if (rectTransform != null)
        {
            originalPosition = rectTransform.localPosition;
            originalScale = rectTransform.localScale;
        }

        if (popRectTransform != null)
        {
            popRectTransform.localScale = Vector3.one;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            PlayHitAnimation();
        }
    }

    private void PlayHitAnimation()
    {
        PlayShakeEffect();
        PlayPopEffect();
    }

    private void PlayShakeEffect()
    {
        if (rectTransform == null) return;

        rectTransform.DOKill();

        // Reset to original position before shake
        rectTransform.localPosition = originalPosition;

        // Shake around original position
        rectTransform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, 90f, false, true)
            .OnComplete(() =>
            {
                // Ensure we return to exact original position
                rectTransform.localPosition = originalPosition;
            });
    }

    private void PlayPopEffect()
    {
        if (popEffectImage == null) return;

        popEffectImage.gameObject.SetActive(true);
        popRectTransform.localScale = Vector3.one;

        Color startColor = popEffectImage.color;
        startColor.a = 0.12f;
        popEffectImage.color = startColor;

        popRectTransform.DOKill();
        popEffectImage.DOKill();

        Vector3 targetScale = Vector3.one * popScaleMultiplier;

        Sequence popSequence = DOTween.Sequence();

        popSequence.Join(
            popRectTransform.DOScale(targetScale, popScaleDuration)
                .SetEase(popScaleCurve)
        );

        popSequence.Join(
            popEffectImage.DOFade(0f, popFadeDuration)
                .SetEase(Ease.OutQuad)
        );

        popSequence.OnComplete(() =>
        {
            popEffectImage.gameObject.SetActive(false);
            popRectTransform.localScale = Vector3.one;
        });
    }

    internal void UpdateOriginalPosition()
    {
        if (rectTransform != null)
        {
            originalPosition = rectTransform.localPosition;

            if (popRectTransform != null)
            {
                popRectTransform.localScale = Vector3.one;
            }
        }
    }

    internal void UpdateOriginalScale()
    {
        if (rectTransform != null)
        {
            originalScale = rectTransform.localScale;

            if (popRectTransform != null)
            {
                popRectTransform.localScale = Vector3.one;
            }
        }
    }

    private void OnDestroy()
    {
        rectTransform?.DOKill();
        popRectTransform?.DOKill();
        popEffectImage?.DOKill();
    }
}