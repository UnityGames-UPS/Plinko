using UnityEngine;
using System.Collections.Generic;

namespace PlinkoGame
{
    /// <summary>
    /// Creates unique randomized paths with strong peg avoidance
    /// Each ball gets different waypoints even for same target
    /// </summary>
    public class PathCalculator : MonoBehaviour
    {
        [Header("Path Settings")]
        [SerializeField] private float pegClearance = 0.65f;
        [SerializeField] private float rowOffset = 0.7f;
        [SerializeField] private float randomSpread = 0.2f;
        [SerializeField] private float targetApproach = 0.4f;

        [Header("Debug")]
        [SerializeField] private bool debugPath = false;

        public List<Vector2> CalculatePath(Vector2 startWorld, Transform targetCatcher, List<List<Vector2>> pegRowsWorld)
        {
            List<Vector2> pathWorld = new List<Vector2>();

            if (targetCatcher == null)
            {
                pathWorld.Add(startWorld);
                return pathWorld;
            }

            Transform board = targetCatcher.parent;
            if (board == null)
            {
                pathWorld.Add(startWorld);
                return pathWorld;
            }

            // Convert to local space
            Vector2 startLocal = board.InverseTransformPoint(startWorld);
            Vector2 targetLocal = board.InverseTransformPoint(targetCatcher.position);

            if (debugPath)
            {
                Debug.Log($"[PathCalc] Start: {startLocal}, Target: {targetLocal}");
            }

            // Convert peg rows to local
            List<List<Vector2>> pegRowsLocal = new List<List<Vector2>>();
            foreach (List<Vector2> rowWorld in pegRowsWorld)
            {
                List<Vector2> rowLocal = new List<Vector2>();
                foreach (Vector2 pegWorld in rowWorld)
                {
                    rowLocal.Add(board.InverseTransformPoint(pegWorld));
                }
                rowLocal.Sort((a, b) => a.x.CompareTo(b.x));
                pegRowsLocal.Add(rowLocal);
            }

            // Generate unique path in local space
            List<Vector2> pathLocal = GenerateUniquePath(startLocal, targetLocal, pegRowsLocal);

            // Convert back to world space
            foreach (Vector2 pointLocal in pathLocal)
            {
                pathWorld.Add(board.TransformPoint(pointLocal));
            }

            return pathWorld;
        }

        private List<Vector2> GenerateUniquePath(Vector2 start, Vector2 target, List<List<Vector2>> pegRows)
        {
            List<Vector2> path = new List<Vector2>();

            // Randomize start position
            Vector2 randomStart = start;
            randomStart.x += Random.Range(-randomSpread * 0.8f, randomSpread * 0.8f);
            path.Add(randomStart);

            if (pegRows.Count == 0)
            {
                path.Add(target);
                return path;
            }

            float startX = randomStart.x;
            float targetX = target.x;
            float totalDist = targetX - startX;

            // Create waypoints for each row
            for (int i = 0; i < pegRows.Count; i++)
            {
                List<Vector2> row = pegRows[i];
                if (row.Count == 0) continue;

                // Calculate ideal X with progression toward target
                float progress = (float)(i + 1) / (pegRows.Count + 1);
                float idealX = startX + (totalDist * progress);

                // Reduced randomization for more consistent paths
                float uniqueOffset = Random.Range(-randomSpread * 1.2f, randomSpread * 1.2f);
                idealX += uniqueOffset;

                // Smaller wave pattern
                float wave = Mathf.Sin(i * 0.8f + Random.value * 6.28f) * randomSpread * 0.5f;
                idealX += wave;

                // Find safe waypoint
                Vector2 waypoint = FindSafeWaypoint(row, idealX);
                path.Add(waypoint);

                if (debugPath && i % 3 == 0)
                {
                    Debug.Log($"[PathCalc] Row {i}: idealX={idealX:F2}, waypoint={waypoint}");
                }
            }

            // Add approach point DIRECTLY above target with minimal offset
            Vector2 approach = new Vector2(
                target.x + Random.Range(-randomSpread * 0.2f, randomSpread * 0.2f),
                target.y + targetApproach
            );
            path.Add(approach);

            // Add final target point
            path.Add(target);

            return path;
        }

        private Vector2 FindSafeWaypoint(List<Vector2> rowPegs, float idealX)
        {
            if (rowPegs.Count == 0)
            {
                return new Vector2(idealX, 0);
            }

            List<Vector2> pegs = new List<Vector2>(rowPegs);
            pegs.Sort((a, b) => a.x.CompareTo(b.x));

            // Place waypoint BELOW pegs by rowOffset
            float y = pegs[0].y - rowOffset;

            // Find safe X position
            float x = idealX;

            // Check if ideal X is safe from all pegs
            bool needsAdjustment = false;
            foreach (Vector2 peg in pegs)
            {
                float dist = Mathf.Abs(x - peg.x);
                if (dist < pegClearance)
                {
                    needsAdjustment = true;
                    break;
                }
            }

            // If not safe, find nearest gap
            if (needsAdjustment)
            {
                x = FindNearestGap(pegs, idealX);
            }

            // Add final random offset (reduced for better accuracy)
            x += Random.Range(-randomSpread * 0.2f, randomSpread * 0.2f);

            // Verify final position is safe
            foreach (Vector2 peg in pegs)
            {
                float dist = Mathf.Abs(x - peg.x);
                if (dist < pegClearance * 0.8f)
                {
                    // Push away from peg
                    float pushDir = Mathf.Sign(x - peg.x);
                    x = peg.x + (pushDir * pegClearance);
                }
            }

            return new Vector2(x, y);
        }

        private float FindNearestGap(List<Vector2> pegs, float idealX)
        {
            // Left of all pegs
            if (idealX < pegs[0].x)
            {
                return pegs[0].x - pegClearance;
            }

            // Right of all pegs
            if (idealX > pegs[pegs.Count - 1].x)
            {
                return pegs[pegs.Count - 1].x + pegClearance;
            }

            // Find gap containing ideal X
            for (int i = 0; i < pegs.Count - 1; i++)
            {
                float leftPegX = pegs[i].x;
                float rightPegX = pegs[i + 1].x;

                if (idealX >= leftPegX && idealX <= rightPegX)
                {
                    float gapCenter = (leftPegX + rightPegX) / 2f;
                    float gapWidth = rightPegX - leftPegX;

                    // Only use gap if wide enough
                    if (gapWidth > pegClearance * 2.5f)
                    {
                        float offset = (idealX - gapCenter) / (gapWidth * 0.5f);
                        offset = Mathf.Clamp(offset, -0.7f, 0.7f);
                        return gapCenter + (offset * gapWidth * 0.3f);
                    }
                    else
                    {
                        // Gap too narrow, go to side with more space
                        float leftDist = Mathf.Abs(idealX - leftPegX);
                        float rightDist = Mathf.Abs(idealX - rightPegX);

                        if (leftDist < rightDist)
                        {
                            return leftPegX - pegClearance;
                        }
                        else
                        {
                            return rightPegX + pegClearance;
                        }
                    }
                }
            }

            // Fallback
            return (pegs[0].x + pegs[1].x) / 2f;
        }

        public List<List<Vector2>> GetPegRowsFromBoard(BoardController board)
        {
            List<List<Vector2>> pegRows = new List<List<Vector2>>();
            Transform boardTransform = board.transform;
            Dictionary<float, List<Vector2>> rowDict = new Dictionary<float, List<Vector2>>();

            for (int i = 0; i < boardTransform.childCount; i++)
            {
                Transform child = boardTransform.GetChild(i);

                if (child.gameObject.activeSelf && child.CompareTag("Peg"))
                {
                    float yPos = Mathf.Round(child.localPosition.y * 100f) / 100f;

                    if (!rowDict.ContainsKey(yPos))
                    {
                        rowDict[yPos] = new List<Vector2>();
                    }

                    rowDict[yPos].Add(child.position);
                }
            }

            List<float> sortedY = new List<float>(rowDict.Keys);
            sortedY.Sort((a, b) => b.CompareTo(a));

            foreach (float y in sortedY)
            {
                pegRows.Add(rowDict[y]);
            }

            return pegRows;
        }
    }
}