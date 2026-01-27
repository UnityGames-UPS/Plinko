using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace PlinkoGame
{
    /// <summary>
    /// Ball launcher with optimized spawning for high ball counts
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
        [SerializeField] private float baseGravityScale = 1.2f;

        [Header("Ball Scaling")]
        [SerializeField] private float ballScaleAt8Rows = 1.0f;
        [SerializeField] private float ballScaleAt16Rows = 0.6f;

        [Header("Ball Start Position")]
        [SerializeField] private float startOffsetAt8Rows = 0.5f;
        [SerializeField] private float startOffsetAt16Rows = 0.3f;

        [Header("Multi-Ball Settings")]
        [SerializeField] private int maxActiveBalls = 50;
        [SerializeField] private float spawnDelay = 0.05f;

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

        private Queue<int> spawnQueue = new Queue<int>();
        private bool isSpawning = false;
        private Coroutine spawnCoroutine;

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

                // Configure for minimal ball-to-ball collision
                rb.simulated = false;
                ball.SetActive(false);

                // Set collision layers to reduce ball-to-ball physics
                ball.layer = LayerMask.NameToLayer("Default");
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

            spawnQueue.Enqueue(targetCatcherIndex);

            if (!isSpawning)
            {
                if (spawnCoroutine != null)
                {
                    StopCoroutine(spawnCoroutine);
                }
                spawnCoroutine = StartCoroutine(ProcessSpawnQueue());
            }
        }

        private IEnumerator ProcessSpawnQueue()
        {
            isSpawning = true;

            while (spawnQueue.Count > 0)
            {
                if (activeBallCount >= maxActiveBalls)
                {
                    yield return new WaitForSeconds(0.1f);
                    continue;
                }

                int targetCatcherIndex = spawnQueue.Dequeue();

                GameObject availableBall = GetAvailableBall();
                if (availableBall == null)
                {
                    yield return new WaitForSeconds(0.1f);
                    continue;
                }

                Transform targetCatcher = activeCatchers[targetCatcherIndex];
                DropBallInternal(availableBall, targetCatcher);

                yield return new WaitForSeconds(spawnDelay);
            }

            isSpawning = false;
        }

        private void DropBallInternal(GameObject ball, Transform targetCatcher)
        {
            AudioManager.Instance?.PlayBallSpawn();

            List<List<Vector2>> pegRows = pathCalculator.GetPegRowsFromBoard(boardController);

            // Generate unique path for this ball
            List<Vector2> calculatedPath = pathCalculator.CalculatePath(
                initialBallWorldPosition,
                targetCatcher,
                pegRows
            );

            // Add small random spawn offset to prevent overlap
            Vector3 spawnPos = initialBallWorldPosition;
            spawnPos.x += Random.Range(-0.1f, 0.1f);

            ball.transform.position = spawnPos;
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
                ballRb.linearDamping = 0.5f;
                ballRb.angularDamping = 0.8f;
                ballRb.simulated = true;
                ballRb.WakeUp();

                if (ballRb.sharedMaterial == null)
                {
                    PhysicsMaterial2D bouncyMaterial = new PhysicsMaterial2D
                    {
                        bounciness = 0.08f, // Minimal bounce
                        friction = 0.15f
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
                StartCoroutine(ResetBallAfterDelay(landedBall, 1.2f));
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

        private void UpdateBallStartPosition()
        {
            if (boardController == null)
            {
                Debug.LogWarning("[BallLauncher] BoardController is null");
                return;
            }

            currentRows = boardController.GetCurrentRows();
            float firstRowLocalY = boardController.GetFirstPegRowLocalY();

            float t = Mathf.InverseLerp(8, 16, currentRows);
            float offset = Mathf.Lerp(startOffsetAt8Rows, startOffsetAt16Rows, t);

            Vector3 localPosition = new Vector3(0, firstRowLocalY + offset, 0);
            initialBallWorldPosition = boardController.transform.TransformPoint(localPosition);
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

        internal void OnBoardRebuilt()
        {
            UpdateActiveCatchers();
            UpdateBallScale();
            UpdateBallStartPosition();
        }

        internal void OnOrientationChanged()
        {
            UpdateActiveCatchers();
            UpdateBallScale();
            UpdateBallStartPosition();
        }

        internal void OnRiskChanged(string riskName)
        {
            UpdateBallSprites(riskName);
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
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            spawnQueue.Clear();

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