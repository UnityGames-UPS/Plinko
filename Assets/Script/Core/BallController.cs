using UnityEngine;
using System;
using System.Collections.Generic;

namespace PlinkoGame
{
    /// <summary>
    /// Controls individual ball physics and path following
    /// Uses proper encapsulation (no public variables)
    /// Updated with audio integration
    /// </summary>
    public class BallController : MonoBehaviour
    {
        [Header("Physics Settings")]
        [SerializeField] private float baseGravityScale = 1.5f;
        [SerializeField] private float fallSpeed = 4f;
        [SerializeField] private float maxSpeed = 15f; // Higher to allow emergency forces

        [Header("Path Following")]
        [SerializeField] private float pathFollowStrength = 28f;
        [SerializeField] private float nextPointThreshold = 0.3f;
        [SerializeField] private float finalApproachDistance = 2f;
        [SerializeField] private float stuckCheckInterval = 0.15f; // Check more frequently
        [SerializeField] private float stuckVelocityThreshold = 0.05f; // Lower threshold
        [SerializeField] private int maxStuckFrames = 3; // Teleport after 3 stuck checks
        [SerializeField] private bool visualizePath = true;

        [Header("Collision Response")]
        [SerializeField] private float pegBounceReduction = 0.2f;
        [SerializeField] private float pegPushForce = 2f;
        [SerializeField] private float unstuckForce = 20f; // Very strong physics push

        // Events
        public event Action<int, GameObject> OnBallCaught;
        public event Action<GameObject> OnPegHit;

        // Components
        private Rigidbody2D rb;

        // Path state
        private string targetCatcherName;
        private List<Vector2> calculatedPath;
        private int currentPathIndex;
        private bool isFollowingPath;
        private bool inFinalApproach;

        // Collision tracking
        private int pegCollisions;
        private HashSet<Collider2D> hitPegs = new HashSet<Collider2D>();

        // Stuck detection
        private float lastStuckCheck;
        private Vector2 lastPosition;
        private int stuckFrameCount = 0;
        private float ballSpawnTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = baseGravityScale;

                if (rb.sharedMaterial == null)
                {
                    PhysicsMaterial2D bouncyMaterial = new PhysicsMaterial2D
                    {
                        bounciness = 0.15f,
                        friction = 0.05f
                    };
                    rb.sharedMaterial = bouncyMaterial;
                }
            }
            else
            {
                Debug.LogError($"Ball {gameObject.name} missing Rigidbody2D!");
            }
        }

        public void Initialize(string targetCatcher, List<Vector2> path)
        {
            targetCatcherName = targetCatcher;
            calculatedPath = path;
            currentPathIndex = 0;
            isFollowingPath = true;
            inFinalApproach = false;
            pegCollisions = 0;
            hitPegs.Clear();
            lastStuckCheck = Time.time;
            lastPosition = transform.position;
            stuckFrameCount = 0;
            ballSpawnTime = Time.time;
        }

        private void FixedUpdate()
        {
            if (!isFollowingPath || calculatedPath == null || calculatedPath.Count == 0)
                return;

            if (currentPathIndex >= calculatedPath.Count)
            {
                isFollowingPath = false;
                return;
            }

            // Check if ball is stuck
            CheckIfStuck();

            Vector2 currentPos = transform.position;
            Vector2 targetPoint = calculatedPath[currentPathIndex];
            float distanceToTarget = Vector2.Distance(currentPos, targetPoint);

            Vector2 finalTarget = calculatedPath[calculatedPath.Count - 1];
            float distanceToFinal = Vector2.Distance(currentPos, finalTarget);

            if (distanceToFinal < finalApproachDistance && !inFinalApproach)
            {
                inFinalApproach = true;
            }

            if (distanceToTarget > nextPointThreshold)
            {
                Vector2 direction = (targetPoint - currentPos).normalized;

                // Reduce path following strength if ball was recently stuck (let physics take over)
                float effectiveStrength = stuckFrameCount > 0 ? pathFollowStrength * 0.5f : pathFollowStrength;

                if (inFinalApproach)
                {
                    // Strong final approach to catcher
                    Vector2 toFinal = (finalTarget - currentPos).normalized;
                    Vector2 desiredVelocity = toFinal * fallSpeed * 2f;
                    rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVelocity, Time.fixedDeltaTime * effectiveStrength * 1.5f);
                }
                else
                {
                    // Normal path following
                    Vector2 desiredVelocity = direction * fallSpeed;
                    desiredVelocity.y -= baseGravityScale;
                    rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVelocity, Time.fixedDeltaTime * effectiveStrength);
                }

                // Clamp velocity
                if (rb.linearVelocity.magnitude > maxSpeed)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
                }
            }
            else
            {
                currentPathIndex++;
            }

            // Smooth rotation based on velocity
            if (rb.linearVelocity.magnitude > 0.1f)
            {
                float targetAngle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                float currentAngle = transform.eulerAngles.z;
                float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.fixedDeltaTime * 5f);
                transform.rotation = Quaternion.Euler(0, 0, newAngle);
            }
        }

        private void CheckIfStuck()
        {
            if (Time.time - lastStuckCheck < stuckCheckInterval)
                return;

            // Ensure physics is active
            if (rb != null && !rb.simulated)
            {
                rb.simulated = true;
            }

            Vector2 currentPos = transform.position;
            float distanceMoved = Vector2.Distance(currentPos, lastPosition);

            // If ball hasn't moved much, it might be stuck
            if (distanceMoved < stuckVelocityThreshold && rb.linearVelocity.magnitude < stuckVelocityThreshold)
            {
                stuckFrameCount++;

                if (currentPathIndex < calculatedPath.Count)
                {
                    Vector2 nextPoint = calculatedPath[currentPathIndex];
                    Vector2 directionToTarget = (nextPoint - currentPos).normalized;

                    if (stuckFrameCount >= maxStuckFrames)
                    {
                        // EMERGENCY: Very aggressive force to guarantee movement

                        // Ensure Rigidbody is active
                        rb.WakeUp();
                        rb.bodyType = RigidbodyType2D.Dynamic;

                        // 1. Override velocity completely toward target
                        rb.linearVelocity = directionToTarget * fallSpeed * 3f;

                        // 2. Apply massive force
                        rb.AddForce(directionToTarget * unstuckForce * 3f, ForceMode2D.Impulse);

                        // 3. Strong upward force to escape peg cage
                        rb.AddForce(Vector2.up * unstuckForce, ForceMode2D.Impulse);

                        // 4. Add random sideways force to break symmetry
                        float randomSide = UnityEngine.Random.Range(-1f, 1f);
                        rb.AddForce(Vector2.right * randomSide * unstuckForce * 0.5f, ForceMode2D.Impulse);

                        // Don't reset counter - keep applying force until clearly moving
                        Debug.Log($"[Ball] EMERGENCY FORCE applied - frame {stuckFrameCount}, velocity: {rb.linearVelocity.magnitude}");
                    }
                    else
                    {
                        // Normal unstuck attempts
                        rb.AddForce(directionToTarget * unstuckForce, ForceMode2D.Impulse);
                        rb.AddForce(Vector2.up * unstuckForce * 0.3f, ForceMode2D.Impulse);
                    }
                }
            }
            else
            {
                // Ball is moving significantly, reset stuck counter
                if (distanceMoved > stuckVelocityThreshold * 3f || rb.linearVelocity.magnitude > stuckVelocityThreshold * 3f)
                {
                    stuckFrameCount = 0;
                }
            }

            lastPosition = currentPos;
            lastStuckCheck = Time.time;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Peg"))
            {
                if (!hitPegs.Contains(collision.collider))
                {
                    hitPegs.Add(collision.collider);
                    pegCollisions++;

                    OnPegHit?.Invoke(collision.gameObject);

                    // AUDIO: Play ball collision sound
                    AudioManager.Instance?.PlayBallCollision();

                    // Reduce velocity slightly
                    rb.linearVelocity *= (1f - pegBounceReduction);

                    // Push ball away from peg
                    Vector2 pushDir = (transform.position - collision.transform.position).normalized;
                    rb.AddForce(pushDir * pegPushForce, ForceMode2D.Impulse);
                }
            }
            else if (collision.gameObject.CompareTag("Ball"))
            {
                // Reduce collision impact between balls
                rb.linearVelocity *= 0.9f;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Catcher"))
            {
                HandleBallCaught(collision);
            }
        }

        private void HandleBallCaught(Collider2D catcher)
        {
            isFollowingPath = false;
            int actualCatcherIndex = ParseCatcherIndex(catcher.name);

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0;
            }

            OnBallCaught?.Invoke(actualCatcherIndex, gameObject);
        }

        private int ParseCatcherIndex(string catcherName)
        {
            string numberPart = catcherName.Replace("Catchers", "").Replace("Catcher", "");
            if (int.TryParse(numberPart, out int index))
                return index;

            Debug.LogWarning($"Could not parse catcher index from: {catcherName}");
            return -1;
        }

        private void OnDrawGizmos()
        {
            if (!visualizePath || calculatedPath == null || calculatedPath.Count == 0)
                return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < calculatedPath.Count - 1; i++)
            {
                Gizmos.DrawLine(calculatedPath[i], calculatedPath[i + 1]);
            }

            for (int i = 0; i < calculatedPath.Count; i++)
            {
                if (i == currentPathIndex)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(calculatedPath[i], 0.2f);
                }
                else if (i < currentPathIndex)
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawSphere(calculatedPath[i], 0.1f);
                }
                else
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(calculatedPath[i], 0.15f);
                }
            }

            if (currentPathIndex < calculatedPath.Count)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, calculatedPath[currentPathIndex]);
            }
        }

        public int GetPegCollisions() => pegCollisions;
        public string GetTargetCatcherName() => targetCatcherName;
    }
}