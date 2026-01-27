using UnityEngine;
using System;
using System.Collections.Generic;

namespace PlinkoGame
{
    /// <summary>
    /// Robust ball controller with aggressive stuck prevention
    /// Handles 1000+ balls without tangling
    /// </summary>
    public class BallController : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] private float baseGravity = 1.2f;
        [SerializeField] private float pathForce = 18f;
        [SerializeField] private float maxVelocity = 5f;
        [SerializeField] private float nextPointDist = 0.3f;
        [SerializeField] private float downwardBias = 0.8f;
        [SerializeField] private float finalApproachDistance = 1.5f;
        [SerializeField] private float finalApproachForce = 30f;

        [Header("Stuck Prevention")]
        [SerializeField] private float stuckCheckTime = 0.1f;
        [SerializeField] private float stuckThreshold = 0.08f;
        [SerializeField] private int maxStuckChecks = 2;
        [SerializeField] private float teleportThreshold = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool visualizePath = true;

        public event Action<int, GameObject> OnBallCaught;
        public event Action<GameObject> OnPegHit;

        private Rigidbody2D rb;
        private List<Vector2> path;
        private int pathIndex;
        private bool active;
        private bool inFinalApproach;

        private float lastStuckCheck;
        private Vector2 lastPos;
        private int stuckCount;
        private HashSet<Collider2D> hitPegs = new HashSet<Collider2D>();
        private string targetCatcherName;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            ConfigurePhysics();
        }

        private void ConfigurePhysics()
        {
            if (rb == null) return;

            rb.gravityScale = baseGravity;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.8f;

            if (rb.sharedMaterial == null)
            {
                PhysicsMaterial2D mat = new PhysicsMaterial2D
                {
                    bounciness = 0.08f, // Minimal bounce
                    friction = 0.15f
                };
                rb.sharedMaterial = mat;
            }
        }

        public void Initialize(string targetName, List<Vector2> ballPath)
        {
            targetCatcherName = targetName;
            path = ballPath;
            pathIndex = 0;
            active = true;
            inFinalApproach = false;
            lastStuckCheck = Time.time;
            lastPos = transform.position;
            stuckCount = 0;
            hitPegs.Clear();

            if (rb != null)
            {
                rb.simulated = true;
                rb.WakeUp();
            }
        }

        private void FixedUpdate()
        {
            if (!active || path == null || pathIndex >= path.Count)
            {
                active = false;
                return;
            }

            HandleStuckDetection();
            ApplyPathForce();
            ClampVelocity();
        }

        private void HandleStuckDetection()
        {
            if (Time.time - lastStuckCheck < stuckCheckTime) return;

            Vector2 currentPos = transform.position;
            float moved = Vector2.Distance(currentPos, lastPos);

            if (moved < stuckThreshold && rb.linearVelocity.magnitude < stuckThreshold)
            {
                stuckCount++;

                if (stuckCount >= maxStuckChecks)
                {
                    // Teleport to next waypoint
                    if (pathIndex < path.Count)
                    {
                        Vector2 target = path[pathIndex];
                        float distToTarget = Vector2.Distance(currentPos, target);

                        if (distToTarget > teleportThreshold)
                        {
                            transform.position = target;
                            rb.linearVelocity = Vector2.down * 3f;
                            pathIndex++;
                            stuckCount = 0;
                        }
                        else
                        {
                            // Close enough, move to next point
                            pathIndex++;
                            stuckCount = 0;
                        }
                    }
                }
                else
                {
                    // Apply emergency force
                    Vector2 escapeDir = (path[pathIndex] - currentPos).normalized;
                    rb.linearVelocity = escapeDir * 5f;
                    rb.AddForce(escapeDir * 25f, ForceMode2D.Impulse);
                }
            }
            else if (moved > stuckThreshold * 2f)
            {
                stuckCount = 0;
            }

            lastPos = currentPos;
            lastStuckCheck = Time.time;
        }

        private void ApplyPathForce()
        {
            Vector2 currentPos = transform.position;
            Vector2 targetPoint = path[pathIndex];
            float dist = Vector2.Distance(currentPos, targetPoint);

            // Check if reached current waypoint
            if (dist < nextPointDist)
            {
                pathIndex++;
                if (pathIndex >= path.Count)
                {
                    active = false;
                    return;
                }
                targetPoint = path[pathIndex];
                dist = Vector2.Distance(currentPos, targetPoint);
            }

            // Check if near final target (last point in path)
            Vector2 finalTarget = path[path.Count - 1];
            float distanceToFinal = Vector2.Distance(currentPos, finalTarget);

            if (distanceToFinal < finalApproachDistance && !inFinalApproach)
            {
                inFinalApproach = true;
            }

            if (inFinalApproach)
            {
                // Strong direct force to final target
                Vector2 toFinal = (finalTarget - currentPos).normalized;
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, toFinal * 3f, Time.fixedDeltaTime * 10f);
                rb.AddForce(toFinal * finalApproachForce);
            }
            else
            {
                // Normal path following
                Vector2 direction = (targetPoint - currentPos).normalized;

                // Ensure downward movement
                if (direction.y > 0)
                {
                    direction.y = -0.2f;
                    direction.Normalize();
                }

                // Apply force toward waypoint with downward bias
                Vector2 force = direction * pathForce;
                force.y -= downwardBias;

                rb.AddForce(force);

                // Limit horizontal velocity
                Vector2 vel = rb.linearVelocity;
                float maxHorizontalSpeed = maxVelocity * 0.5f;
                if (Mathf.Abs(vel.x) > maxHorizontalSpeed)
                {
                    vel.x = Mathf.Sign(vel.x) * maxHorizontalSpeed;
                    rb.linearVelocity = vel;
                }
            }
        }

        private void ClampVelocity()
        {
            if (rb.linearVelocity.magnitude > maxVelocity)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Peg"))
            {
                if (!hitPegs.Contains(collision.collider))
                {
                    hitPegs.Add(collision.collider);
                    OnPegHit?.Invoke(collision.gameObject);
                    AudioManager.Instance?.PlayBallCollision();

                    // Dampen velocity to reduce bouncing
                    rb.linearVelocity *= 0.6f;

                    // Gentle push away from peg
                    Vector2 pushDir = (transform.position - collision.transform.position).normalized;
                    rb.AddForce(pushDir * 0.8f, ForceMode2D.Impulse);

                    // Always maintain downward component
                    Vector2 vel = rb.linearVelocity;
                    if (vel.y > -0.5f)
                    {
                        vel.y = -0.5f; // Minimum downward speed
                        rb.linearVelocity = vel;
                    }
                }
            }
            else if (collision.gameObject.CompareTag("Ball"))
            {
                // Minimal ball-to-ball interaction
                rb.linearVelocity *= 0.95f;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Catcher"))
            {
                active = false;
                int catcherIndex = ParseCatcherIndex(collision.name);

                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0;
                }

                OnBallCaught?.Invoke(catcherIndex, gameObject);
            }
        }

        private int ParseCatcherIndex(string name)
        {
            string num = name.Replace("Catchers", "").Replace("Catcher", "");
            return int.TryParse(num, out int index) ? index : -1;
        }

        private void OnDrawGizmos()
        {
            if (!visualizePath || path == null || path.Count == 0)
                return;

            // Draw path lines
            Gizmos.color = Color.cyan;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }

            // Draw waypoints
            for (int i = 0; i < path.Count; i++)
            {
                if (i == pathIndex)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(path[i], 0.2f);
                }
                else if (i < pathIndex)
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawSphere(path[i], 0.1f);
                }
                else
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(path[i], 0.15f);
                }
            }

            // Draw line to current target
            if (pathIndex < path.Count)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, path[pathIndex]);
            }
        }

        public string GetTargetCatcherName() => targetCatcherName;
    }
}