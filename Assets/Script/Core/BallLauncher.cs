using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace PlinkoGame
{
    /// <summary>
    /// Ball position calculated in LOCAL space, converted to WORLD for spawn
    /// Works with any orientation/ratio
    /// </summary>
    public class BallLauncher : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private List<GameObject> ballPool;
        [SerializeField] private BoardController boardController;
        [SerializeField] private Transform spawnTransform;
        [SerializeField] private PathCalculator pathCalculator;
        [SerializeField] private GameManager gameManager;

        [Header("Physics")]
        [SerializeField] private float baseGravityScale = 1.5f;

        [Header("Ball Scaling")]
        [SerializeField] private float ballScaleAt8Rows = 1.0f;
        [SerializeField] private float ballScaleAt16Rows = 0.6f;

        [Header("Ball Start Position")]
        [SerializeField] private float startOffsetAt8Rows = 0.5f;
        [SerializeField] private float startOffsetAt16Rows = 0.3f;

        [Header("Multi-Ball Settings")]
        [SerializeField] private int maxActiveBalls = 10;

        [Header("Difficulty Sprites")]
        [SerializeField] private Sprite lowDifficultySprite;
        [SerializeField] private Sprite mediumDifficultySprite;
        [SerializeField] private Sprite highDifficultySprite;

        private List<Transform> activeCatchers = new List<Transform>();
        private Vector3 initialBallWorldPosition;
        private int currentRows = 8;
        private int activeBallCount;
        private HashSet<GameObject> ballsInUse = new HashSet<GameObject>();
        private string currentDifficulty = "LOW";

        private void Start()
        {
            if (pathCalculator == null)
            {
                pathCalculator = gameObject.AddComponent<PathCalculator>();
            }

            ValidateAndInitializeBalls();
            StartCoroutine(InitializeAfterFrame());
        }

        private IEnumerator InitializeAfterFrame()
        {
            yield return new WaitForEndOfFrame();

            UpdateActiveCatchers();
            UpdateBallScale();
            UpdateBallStartPosition();
            UpdateBallSprites(currentDifficulty);

            Debug.Log($"[BallLauncher] ✅ Initialized | Ball World Pos: {initialBallWorldPosition}");
        }

        private void ValidateAndInitializeBalls()
        {
            if (ballPool == null || ballPool.Count == 0) return;

            foreach (GameObject ball in ballPool)
            {
                if (ball == null) continue;

                Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
                BallController controller = ball.GetComponent<BallController>();

                if (rb == null || controller == null) continue;

                rb.simulated = false;
                ball.SetActive(false);
            }
        }

        internal void DropBallToTarget(int targetCatcherIndex)
        {
            UpdateActiveCatchers();

            if (targetCatcherIndex < 0 || targetCatcherIndex >= activeCatchers.Count)
            {
                Debug.LogWarning($"[BallLauncher] Invalid target index: {targetCatcherIndex}");
                return;
            }

            GameObject availableBall = GetAvailableBall();
            if (availableBall == null)
            {
                Debug.LogWarning("[BallLauncher] No available balls");
                return;
            }

            Transform targetCatcher = activeCatchers[targetCatcherIndex];
            DropBallInternal(availableBall, targetCatcher);
        }

        private void DropBallInternal(GameObject ball, Transform targetCatcher)
        {
            AudioManager.Instance?.PlayBallSpawn();

            List<List<Vector2>> pegRows = pathCalculator.GetPegRowsFromBoard(boardController);

            List<Vector2> calculatedPath = pathCalculator.CalculatePath(
                initialBallWorldPosition,
                targetCatcher,
                pegRows
            );

            ball.transform.position = initialBallWorldPosition;
            ball.transform.rotation = Quaternion.identity;
            ball.SetActive(true);

            Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();
            if (ballRb != null)
            {
                ballRb.bodyType = RigidbodyType2D.Dynamic;
                ballRb.constraints = RigidbodyConstraints2D.None;
                ballRb.linearVelocity = Vector2.zero;
                ballRb.angularVelocity = 0;
                ballRb.gravityScale = baseGravityScale;
                ballRb.linearDamping = 0.3f;
                ballRb.angularDamping = 0.5f;
                ballRb.simulated = true;
                ballRb.WakeUp();

                if (ballRb.sharedMaterial == null)
                {
                    PhysicsMaterial2D bouncyMaterial = new PhysicsMaterial2D
                    {
                        bounciness = 0.2f,
                        friction = 0.1f
                    };
                    ballRb.sharedMaterial = bouncyMaterial;
                }
            }

            BallController ballController = ball.GetComponent<BallController>();
            if (ballController != null)
            {
                ballController.OnBallCaught -= OnBallLanded;
                ballController.OnBallCaught += OnBallLanded;
                ballController.Initialize(targetCatcher.name, calculatedPath);
            }

            ballsInUse.Add(ball);
            activeBallCount++;
        }

        private void OnBallLanded(int catcherIndex, GameObject landedBall)
        {
            if (gameManager != null)
            {
                gameManager.OnBallLanded(catcherIndex);
            }

            if (landedBall != null && ballsInUse.Contains(landedBall))
            {
                StartCoroutine(ResetBallAfterDelay(landedBall, 1.5f));
            }
        }

        private IEnumerator ResetBallAfterDelay(GameObject ball, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (ball != null)
            {
                Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.simulated = false;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0;
                }

                ball.transform.position = initialBallWorldPosition;
                ball.transform.rotation = Quaternion.identity;

                if (ball.activeSelf)
                {
                    ball.SetActive(false);
                }

                ballsInUse.Remove(ball);
                activeBallCount--;
            }
        }

        private GameObject GetAvailableBall()
        {
            foreach (GameObject ball in ballPool)
            {
                if (ball != null && !ballsInUse.Contains(ball))
                {
                    return ball;
                }
            }
            return null;
        }

        private void UpdateActiveCatchers()
        {
            activeCatchers.Clear();

            if (boardController != null && boardController.GetCatchers().Count > 0)
            {
                foreach (Transform catcher in boardController.GetCatchers())
                {
                    if (catcher.gameObject.activeSelf)
                    {
                        activeCatchers.Add(catcher);
                    }
                }
            }
        }

        private void UpdateBallScale()
        {
            if (ballPool == null || ballPool.Count == 0) return;

            if (boardController != null)
            {
                currentRows = boardController.GetCurrentRows();
            }
            else
            {
                currentRows = Mathf.Max(activeCatchers.Count - 1, 8);
            }

            currentRows = Mathf.Clamp(currentRows, 8, 16);

            float t = Mathf.InverseLerp(8, 16, currentRows);
            float targetScale = Mathf.Lerp(ballScaleAt8Rows, ballScaleAt16Rows, t);

            foreach (GameObject ball in ballPool)
            {
                if (ball != null)
                {
                    ball.transform.localScale = Vector3.one * targetScale;
                }
            }
        }

        /// <summary>
        /// ✅ Calculate in LOCAL space, convert to WORLD
        /// Works with any orientation/rotation
        /// </summary>
        private void UpdateBallStartPosition()
        {
            if (boardController == null)
            {
                Debug.LogWarning("[BallLauncher] BoardController is null");
                return;
            }

            currentRows = boardController.GetCurrentRows();

            // Get first peg row LOCAL Y
            float firstRowLocalY = boardController.GetFirstPegRowLocalY();

            // Calculate offset based on rows
            float t = Mathf.InverseLerp(8, 16, currentRows);
            float offset = Mathf.Lerp(startOffsetAt8Rows, startOffsetAt16Rows, t);

            // ✅ Position in LOCAL space (center X = 0)
            Vector3 localPosition = new Vector3(
                0,                      // Center in local space
                firstRowLocalY + offset, // Above first peg row
                0
            );

            // ✅ Convert to WORLD space using board transform
            initialBallWorldPosition = boardController.transform.TransformPoint(localPosition);

            Debug.Log($"[BallLauncher] ✅ Position updated | Local: {localPosition} → World: {initialBallWorldPosition} | Rows: {currentRows}");
        }

        internal void UpdateBallSprites(string difficulty)
        {
            currentDifficulty = difficulty;

            Sprite selectedSprite = difficulty switch
            {
                "LOW" => lowDifficultySprite,
                "MEDIUM" => mediumDifficultySprite,
                "HIGH" => highDifficultySprite,
                _ => lowDifficultySprite
            };

            if (selectedSprite == null) return;

            foreach (GameObject ball in ballPool)
            {
                if (ball != null)
                {
                    Image ballImage = ball.GetComponent<Image>();
                    if (ballImage != null)
                    {
                        ballImage.sprite = selectedSprite;
                    }
                }
            }
        }

        /// <summary>
        /// ✅ Called on board rebuild (row change)
        /// </summary>
        internal void OnBoardRebuilt()
        {
            UpdateActiveCatchers();
            UpdateBallScale();
            UpdateBallStartPosition();
            Debug.Log("[BallLauncher] ✅ Board rebuilt");
        }

        /// <summary>
        /// ✅ Called on orientation change
        /// </summary>
        internal void OnOrientationChanged()
        {
            UpdateActiveCatchers();
            UpdateBallScale();
            UpdateBallStartPosition();
            Debug.Log("[BallLauncher] ✅ Orientation changed");
        }

        /// <summary>
        /// ✅ Called on risk change (sprites only)
        /// </summary>
        internal void OnRiskChanged(string riskName)
        {
            UpdateBallSprites(riskName);
            Debug.Log($"[BallLauncher] ✅ Risk changed: {riskName}");
        }

        internal bool HasAvailableBalls()
        {
            return activeBallCount < maxActiveBalls;
        }

        internal int GetActiveBallCount()
        {
            return activeBallCount;
        }

        private void OnDestroy()
        {
            if (ballPool != null)
            {
                foreach (GameObject ball in ballPool)
                {
                    if (ball != null)
                    {
                        BallController controller = ball.GetComponent<BallController>();
                        if (controller != null)
                        {
                            controller.OnBallCaught -= OnBallLanded;
                        }
                    }
                }
            }
        }
    }
}