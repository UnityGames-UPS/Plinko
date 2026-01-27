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
        [SerializeField] private float maxSpeed = 8f;

        [Header("Path Following")]
        [SerializeField] private float pathFollowStrength = 20f;
        [SerializeField] private float nextPointThreshold = 0.3f;
        [SerializeField] private float finalApproachDistance = 2f;
        [SerializeField] private bool visualizePath = true;

        [Header("Collision Response")]
        [SerializeField] private float pegBounceReduction = 0.3f;
        [SerializeField] private float pegPushForce = 1.5f;

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
                        bounciness = 0.2f,
                        friction = 0.1f
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

                if (inFinalApproach)
                {
                    Vector2 toFinal = (finalTarget - currentPos).normalized;
                    Vector2 desiredVelocity = toFinal * fallSpeed * 1.5f;
                    rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVelocity, Time.fixedDeltaTime * pathFollowStrength);
                }
                else
                {
                    Vector2 desiredVelocity = direction * fallSpeed;
                    desiredVelocity.y -= baseGravityScale;
                    rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVelocity, Time.fixedDeltaTime * pathFollowStrength);
                }

                if (rb.linearVelocity.magnitude > maxSpeed)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
                }
            }
            else
            {
                currentPathIndex++;
            }

            float targetAngle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.z;
            float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.fixedDeltaTime * 5f);
            transform.rotation = Quaternion.Euler(0, 0, newAngle);
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

                    rb.linearVelocity *= (1f - pegBounceReduction);

                    Vector2 pushDir = (transform.position - collision.transform.position).normalized;
                    rb.AddForce(pushDir * pegPushForce, ForceMode2D.Impulse);
                }
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
            bool hitTarget = (catcher.name == targetCatcherName);

            Debug.Log($"Target: {targetCatcherName}, HIT: {hitTarget}");

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