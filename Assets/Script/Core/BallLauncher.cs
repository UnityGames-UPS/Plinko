using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace PlinkoGame
{
    /// <summary>
    /// Manages ball launching and pool system
    /// Updated with audio integration
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

        [Header("Multi-Ball Settings")]
        [SerializeField] private int maxActiveBalls = 10;

        [Header("Difficulty Sprites")]
        [SerializeField] private Sprite lowDifficultySprite;
        [SerializeField] private Sprite mediumDifficultySprite;
        [SerializeField] private Sprite highDifficultySprite;

        private List<Transform> activeCatchers = new List<Transform>();
        private Vector3 initialBallPosition;
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

            if (spawnTransform != null)
            {
                initialBallPosition = spawnTransform.position;
            }
            else
            {
                initialBallPosition = Vector3.zero;
            }

            UpdateActiveCatchers();
            UpdateBallScale();
            UpdateBallSprites(currentDifficulty);
        }

        private void ValidateAndInitializeBalls()
        {
            if (ballPool == null || ballPool.Count == 0)
            {
                return;
            }

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
                return;
            }

            GameObject availableBall = GetAvailableBall();
            if (availableBall == null)
            {
                return;
            }

            Transform targetCatcher = activeCatchers[targetCatcherIndex];
            DropBallInternal(availableBall, targetCatcher);
        }

        private void DropBallInternal(GameObject ball, Transform targetCatcher)
        {
            // ✅ AUDIO: Play ball spawn sound
            AudioManager.Instance?.PlayBallSpawn();

            List<List<Vector2>> pegRows = pathCalculator.GetPegRowsFromBoard(boardController);
            List<Vector2> calculatedPath = pathCalculator.CalculatePath(
                initialBallPosition,
                targetCatcher,
                pegRows
            );

            ball.transform.position = initialBallPosition;
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

                ball.transform.position = initialBallPosition;
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

            if (selectedSprite == null)
            {
                return;
            }

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

        internal void OnBoardRebuilt()
        {
            UpdateActiveCatchers();
            UpdateBallScale();
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