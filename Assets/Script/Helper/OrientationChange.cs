using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

namespace PlinkoGame
{
    /// <summary>
    /// Detects screen dimension changes and notifies UIManager
    /// Handles Canvas Scaler matching for proper scaling across different aspect ratios
    /// NO rotation logic - UIManager handles layout switching
    /// </summary>
    public class OrientationChange : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private CanvasScaler canvasScaler;

        [Header("Match Settings")]
        [SerializeField] private float matchWidth = 0f;
        [SerializeField] private float matchHeight = 1f;
        [SerializeField] private float portraitMatchHeight = 1f;

        [Header("Animation")]
        [SerializeField] private float transitionDuration = 0.2f;
        [SerializeField] private float waitForRotation = 0.2f;

        private Vector2 referenceAspect;
        private Tween matchTween;
        private Coroutine rotationRoutine;
        private bool isLandscape;

        private void Awake()
        {
            if (canvasScaler != null)
            {
                referenceAspect = canvasScaler.referenceResolution;
            }
        }

        /// <summary>
        /// Called from JavaScript when dimensions change
        /// </summary>
        void SwitchDisplay(string dimensions)
        {
            if (rotationRoutine != null) StopCoroutine(rotationRoutine);
            rotationRoutine = StartCoroutine(DimensionChangeCoroutine(dimensions));
        }

        IEnumerator DimensionChangeCoroutine(string dimensions)
        {
            yield return new WaitForSecondsRealtime(waitForRotation);

            string[] parts = dimensions.Split(',');
            if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height) && width > 0 && height > 0)
            {
                Debug.Log($"[OrientationChange] Dimensions - Width: {width}, Height: {height}");

                isLandscape = width > height;

                // Calculate aspect ratio and match value
                float currentAspectRatio = isLandscape ? (float)width / height : (float)height / width;
                float referenceAspectRatio = referenceAspect.x / referenceAspect.y;

                Debug.Log($"[OrientationChange] Current Aspect Ratio: {currentAspectRatio}");

                float targetMatch;

                if (isLandscape)
                {
                    // Horizontal/Landscape mode
                    targetMatch = currentAspectRatio > referenceAspectRatio ? matchHeight : matchWidth;
                }
                else
                {
                    // Vertical/Portrait mode - adjusted match values for different aspect ratios
                    if (currentAspectRatio >= 1.3f && currentAspectRatio < 1.4f)
                        targetMatch = 0.27f;   // ~1.3
                    else if (currentAspectRatio >= 1.4f && currentAspectRatio < 1.5f)
                        targetMatch = 0.32f;   // ~1.4
                    else if (currentAspectRatio >= 1.5f && currentAspectRatio < 1.6f)
                        targetMatch = 0.34f;   // ~1.5
                    else if (currentAspectRatio >= 1.6f && currentAspectRatio < 1.85f)
                        targetMatch = 0.42f;   // ~1.6-1.8 range
                    else if (currentAspectRatio >= 1.85f && currentAspectRatio < 2.4f)
                        targetMatch = 0.5f;    // ~1.85-2.4 range
                    else
                        targetMatch = portraitMatchHeight;
                }

                // Animate canvas scaler match value
                if (canvasScaler != null)
                {
                    if (matchTween != null && matchTween.IsActive()) matchTween.Kill();
                    matchTween = DOTween.To(
                        () => canvasScaler.matchWidthOrHeight,
                        x => canvasScaler.matchWidthOrHeight = x,
                        targetMatch,
                        transitionDuration
                    ).SetEase(Ease.InOutQuad);

                    Debug.Log($"[OrientationChange] matchWidthOrHeight set to: {targetMatch}");
                }

                // Notify UIManager to handle layout switching
                if (uiManager != null)
                {
                    uiManager.OnOrientationChanged(width, height);
                }
            }
            else
            {
                Debug.LogWarning("[OrientationChange] Invalid format received");
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            // Test dimension change in editor with spacebar
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SwitchDisplay(Screen.width + "," + Screen.height);
            }
        }
#endif
    }
}