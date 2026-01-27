using UnityEngine;
using System.Collections.Generic;

namespace PlinkoGame
{
    /// <summary>
    /// Calculates ball path from start to target catcher
    /// IMPROVED VERSION:
    /// - Works in LOCAL SPACE (rotation-independent)
    /// - Uses SMART gap positioning (biased toward target)
    /// - Creates SMOOTH, natural paths (no sharp direction changes)
    /// </summary>
    public class PathCalculator : MonoBehaviour
    {
        [Header("Path Settings")]
        [SerializeField] private float horizontalVariation = 0.015f;
        [SerializeField] private float verticalOffset = 0.3f;
        [SerializeField] private float catcherApproachOffset = 0.5f;
        [SerializeField][Range(0f, 1f)] private float gapBiasFactor = 0.65f; // How much to bias toward target (0.5 = center, 1.0 = edge)
        [SerializeField] private bool debugPath = false;

        // ============================================
        // PATH CALCULATION - SMOOTH & NATURAL
        // ============================================

        public List<Vector2> CalculatePath(Vector2 startPosWorld, Transform targetCatcher, List<List<Vector2>> pegRowsWorld)
        {
            List<Vector2> pathWorld = new List<Vector2>();

            if (targetCatcher == null)
            {
                Debug.LogError("[PathCalc] Target catcher is null!");
                pathWorld.Add(startPosWorld);
                return pathWorld;
            }

            Transform boardTransform = targetCatcher.parent;
            if (boardTransform == null)
            {
                Debug.LogError("[PathCalc] Target catcher has no parent (board)!");
                pathWorld.Add(startPosWorld);
                return pathWorld;
            }

            // Convert to local space
            Vector2 startPosLocal = boardTransform.InverseTransformPoint(startPosWorld);
            Vector2 targetPosLocal = boardTransform.InverseTransformPoint(targetCatcher.position);

            // Convert peg rows to local space
            List<List<Vector2>> pegRowsLocal = new List<List<Vector2>>();
            foreach (List<Vector2> rowWorld in pegRowsWorld)
            {
                List<Vector2> rowLocal = new List<Vector2>();
                foreach (Vector2 pegWorld in rowWorld)
                {
                    Vector2 pegLocal = boardTransform.InverseTransformPoint(pegWorld);
                    rowLocal.Add(pegLocal);
                }
                rowLocal.Sort((a, b) => a.x.CompareTo(b.x));
                pegRowsLocal.Add(rowLocal);
            }

            if (debugPath)
            {
            }

            // Calculate smooth path in local space
            List<Vector2> pathLocal = CalculateSmoothPath(startPosLocal, targetPosLocal, pegRowsLocal);

            // Convert back to world space
            foreach (Vector2 pointLocal in pathLocal)
            {
                Vector2 pointWorld = boardTransform.TransformPoint(pointLocal);
                pathWorld.Add(pointWorld);
            }

            return pathWorld;
        }

        private List<Vector2> CalculateSmoothPath(Vector2 startPos, Vector2 targetPos, List<List<Vector2>> pegRows)
        {
            List<Vector2> path = new List<Vector2>();
            path.Add(startPos);

            int totalRows = pegRows.Count;
            if (totalRows == 0)
            {
                path.Add(targetPos);
                return path;
            }

            float startX = startPos.x;
            float targetX = targetPos.x;
            float totalDistance = targetX - startX;

            // Track previous waypoint for smoothing
            Vector2 previousPoint = startPos;

            for (int row = 0; row < totalRows; row++)
            {
                List<Vector2> rowPegs = pegRows[row];
                if (rowPegs.Count == 0) continue;

                // Calculate ideal X (linear progression)
                float progress = (float)(row + 1) / (totalRows + 1);
                float idealX = startX + (totalDistance * progress);

                // Find smooth waypoint that flows toward target
                Vector2 waypoint = FindSmoothWaypoint(rowPegs, idealX, previousPoint, targetPos);

                // Very small random variation for realism
                waypoint.x += Random.Range(-horizontalVariation, horizontalVariation);

                path.Add(waypoint);
                previousPoint = waypoint;

                if (debugPath && row % 4 == 0)
                {
                }
            }

            // Smooth approach to target
            Vector2 approachPoint = new Vector2(targetX, targetPos.y + catcherApproachOffset);
            path.Add(approachPoint);
            path.Add(targetPos);

            return path;
        }

        /// <summary>
        /// Finds waypoint that creates smooth, natural path
        /// Biases position within gap toward the target direction
        /// </summary>
        private Vector2 FindSmoothWaypoint(List<Vector2> rowPegs, float idealX, Vector2 previousPoint, Vector2 targetPos)
        {
            if (rowPegs.Count == 0)
            {
                return new Vector2(idealX, 0);
            }

            List<Vector2> sortedPegs = new List<Vector2>(rowPegs);
            sortedPegs.Sort((a, b) => a.x.CompareTo(b.x));

            // Determine direction of travel
            float directionToTarget = Mathf.Sign(targetPos.x - previousPoint.x);

            // LEFT of all pegs
            if (idealX < sortedPegs[0].x)
            {
                float gapX = sortedPegs[0].x - 0.5f;
                float gapY = sortedPegs[0].y - verticalOffset;
                return new Vector2(gapX, gapY);
            }

            // RIGHT of all pegs
            if (idealX > sortedPegs[sortedPegs.Count - 1].x)
            {
                float gapX = sortedPegs[sortedPegs.Count - 1].x + 0.5f;
                float gapY = sortedPegs[sortedPegs.Count - 1].y - verticalOffset;
                return new Vector2(gapX, gapY);
            }

            // Find gap containing ideal X
            for (int i = 0; i < sortedPegs.Count - 1; i++)
            {
                Vector2 leftPeg = sortedPegs[i];
                Vector2 rightPeg = sortedPegs[i + 1];

                if (idealX >= leftPeg.x && idealX <= rightPeg.x)
                {
                    // SMOOTH PATH: Bias position within gap toward target
                    float gapX = CalculateBiasedGapPosition(leftPeg.x, rightPeg.x, idealX, directionToTarget);
                    float gapY = leftPeg.y - verticalOffset;

                    if (debugPath)
                    {
                    }

                    return new Vector2(gapX, gapY);
                }
            }

            // Fallback
            float centerX = (sortedPegs[0].x + sortedPegs[1].x) / 2f;
            float centerY = sortedPegs[0].y - verticalOffset;
            return new Vector2(centerX, centerY);
        }

        /// <summary>
        /// Calculates position within gap that creates smooth flow
        /// Instead of always using center, bias toward direction of travel
        /// </summary>
        private float CalculateBiasedGapPosition(float leftPegX, float rightPegX, float idealX, float direction)
        {
            float gapWidth = rightPegX - leftPegX;
            float gapCenter = (leftPegX + rightPegX) / 2f;

            // Calculate how far ideal X is from center (normalized -1 to 1)
            float offsetFromCenter = (idealX - gapCenter) / (gapWidth * 0.5f);
            offsetFromCenter = Mathf.Clamp(offsetFromCenter, -1f, 1f);

            // Apply bias factor to smooth the path
            // gapBiasFactor = 0.5: Always center (rigid)
            // gapBiasFactor = 0.65: Slightly biased (smooth, natural)
            // gapBiasFactor = 1.0: Maximum bias (follows ideal closely)
            float biasedOffset = offsetFromCenter * gapBiasFactor;

            // Calculate final position
            float biasedX = gapCenter + (biasedOffset * gapWidth * 0.5f);

            // Ensure we stay safely within gap (with margins)
            float margin = gapWidth * 0.1f; // 10% margin from edges
            biasedX = Mathf.Clamp(biasedX, leftPegX + margin, rightPegX - margin);

            return biasedX;
        }

        // ============================================
        // BOARD DATA EXTRACTION
        // ============================================

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
                    Vector3 localPos = child.localPosition;
                    float yPos = Mathf.Round(localPos.y * 100f) / 100f;

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
                List<Vector2> row = rowDict[y];
                pegRows.Add(row);
            }

            return pegRows;
        }
    }
}