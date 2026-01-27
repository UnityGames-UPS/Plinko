using UnityEngine;
using System.Collections.Generic;

namespace PlinkoGame
{
    /// <summary>
    /// Calculates ball path from start to target catcher
    /// COMPLETE FIX:
    /// 1. Works in LOCAL SPACE (rotation-independent)
    /// 2. Uses GAP-based waypoints (never hits pegs)
    /// 3. Extracts peg positions in local space
    /// </summary>
    public class PathCalculator : MonoBehaviour
    {
        [Header("Path Settings")]
        [SerializeField] private float horizontalVariation = 0.02f;
        [SerializeField] private float verticalOffset = 0.3f;
        [SerializeField] private float catcherApproachOffset = 0.5f;
        [SerializeField] private bool debugPath = false;

        // ============================================
        // PATH CALCULATION - FULLY LOCAL SPACE
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

            // Get board transform for local space conversion
            Transform boardTransform = targetCatcher.parent;
            if (boardTransform == null)
            {
                Debug.LogError("[PathCalc] Target catcher has no parent (board)!");
                pathWorld.Add(startPosWorld);
                return pathWorld;
            }

            // ✅ Convert EVERYTHING to local space FIRST
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
                // Sort by X in local space
                rowLocal.Sort((a, b) => a.x.CompareTo(b.x));
                pegRowsLocal.Add(rowLocal);
            }

            if (debugPath)
            {
                Debug.Log($"[PathCalc] LOCAL SPACE | Start: {startPosLocal:F2} | Target: {targetPosLocal:F2} | Rows: {pegRowsLocal.Count}");
            }

            // Calculate path in LOCAL space
            List<Vector2> pathLocal = CalculatePathInLocalSpace(startPosLocal, targetPosLocal, pegRowsLocal);

            // Convert final path back to WORLD space
            foreach (Vector2 pointLocal in pathLocal)
            {
                Vector2 pointWorld = boardTransform.TransformPoint(pointLocal);
                pathWorld.Add(pointWorld);
            }

            if (debugPath)
            {
                Debug.Log($"[PathCalc] Generated {pathWorld.Count} waypoints");
            }

            return pathWorld;
        }

        private List<Vector2> CalculatePathInLocalSpace(Vector2 startPos, Vector2 targetPos, List<List<Vector2>> pegRows)
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

            // Generate waypoints for each peg row
            for (int row = 0; row < totalRows; row++)
            {
                List<Vector2> rowPegs = pegRows[row];
                if (rowPegs.Count == 0) continue;

                // Calculate ideal X position (linear progression from start to target)
                float progress = (float)(row + 1) / (totalRows + 1);
                float idealX = startX + (totalDistance * progress);

                // Find waypoint in the GAP between pegs
                Vector2 waypoint = FindGapWaypoint(rowPegs, idealX);

                // Add small random variation
                waypoint.x += Random.Range(-horizontalVariation, horizontalVariation);

                path.Add(waypoint);

                if (debugPath && row % 4 == 0)
                {
                    Debug.Log($"  [PathCalc] Row {row}: Ideal X={idealX:F2}, Waypoint={waypoint:F2}");
                }
            }

            // Add approach waypoint before catcher
            Vector2 approachPoint = new Vector2(targetX, targetPos.y + catcherApproachOffset);
            path.Add(approachPoint);

            // Final destination
            path.Add(targetPos);

            return path;
        }

        /// <summary>
        /// Finds waypoint position in the GAP between pegs (never on/near a peg)
        /// </summary>
        private Vector2 FindGapWaypoint(List<Vector2> rowPegs, float idealX)
        {
            if (rowPegs.Count == 0)
            {
                return new Vector2(idealX, 0);
            }

            // Pegs should already be sorted by X, but ensure it
            List<Vector2> sortedPegs = new List<Vector2>(rowPegs);
            sortedPegs.Sort((a, b) => a.x.CompareTo(b.x));

            // CASE 1: Ideal X is LEFT of all pegs
            if (idealX < sortedPegs[0].x)
            {
                float gapX = sortedPegs[0].x - 0.5f;
                float gapY = sortedPegs[0].y - verticalOffset;
                return new Vector2(gapX, gapY);
            }

            // CASE 2: Ideal X is RIGHT of all pegs
            if (idealX > sortedPegs[sortedPegs.Count - 1].x)
            {
                float gapX = sortedPegs[sortedPegs.Count - 1].x + 0.5f;
                float gapY = sortedPegs[sortedPegs.Count - 1].y - verticalOffset;
                return new Vector2(gapX, gapY);
            }

            // CASE 3: Find the gap that contains ideal X
            for (int i = 0; i < sortedPegs.Count - 1; i++)
            {
                Vector2 leftPeg = sortedPegs[i];
                Vector2 rightPeg = sortedPegs[i + 1];

                // Check if idealX falls in this gap
                if (idealX >= leftPeg.x && idealX <= rightPeg.x)
                {
                    // Position waypoint in CENTER of gap
                    float gapCenterX = (leftPeg.x + rightPeg.x) / 2f;
                    float gapY = leftPeg.y - verticalOffset;

                    if (debugPath)
                    {
                        Debug.Log($"    [PathCalc] Gap: [{leftPeg.x:F2}, {rightPeg.x:F2}] → Center: {gapCenterX:F2}");
                    }

                    return new Vector2(gapCenterX, gapY);
                }
            }

            // FALLBACK: Use closest gap
            float closestGapX = (sortedPegs[0].x + sortedPegs[1].x) / 2f;
            float closestGapY = sortedPegs[0].y - verticalOffset;
            float minDistance = Mathf.Abs(closestGapX - idealX);

            for (int i = 1; i < sortedPegs.Count - 1; i++)
            {
                float gapX = (sortedPegs[i].x + sortedPegs[i + 1].x) / 2f;
                float distance = Mathf.Abs(gapX - idealX);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestGapX = gapX;
                    closestGapY = sortedPegs[i].y - verticalOffset;
                }
            }

            return new Vector2(closestGapX, closestGapY);
        }

        // ============================================
        // BOARD DATA EXTRACTION - IN LOCAL SPACE
        // ============================================

        public List<List<Vector2>> GetPegRowsFromBoard(BoardController board)
        {
            List<List<Vector2>> pegRows = new List<List<Vector2>>();
            Transform boardTransform = board.transform;

            // ✅ Use LOCAL positions for peg extraction
            Dictionary<float, List<Vector2>> rowDict = new Dictionary<float, List<Vector2>>();

            // Collect all active pegs organized by LOCAL Y position
            for (int i = 0; i < boardTransform.childCount; i++)
            {
                Transform child = boardTransform.GetChild(i);

                if (child.gameObject.activeSelf && child.CompareTag("Peg"))
                {
                    // ✅ Use LOCAL position instead of world position
                    Vector3 localPos = child.localPosition;
                    float yPos = Mathf.Round(localPos.y * 100f) / 100f;

                    if (!rowDict.ContainsKey(yPos))
                    {
                        rowDict[yPos] = new List<Vector2>();
                    }

                    // Store as world position (will be converted to local in CalculatePath)
                    rowDict[yPos].Add(child.position);
                }
            }

            // Sort rows top to bottom
            List<float> sortedY = new List<float>(rowDict.Keys);
            sortedY.Sort((a, b) => b.CompareTo(a)); // Descending order

            foreach (float y in sortedY)
            {
                List<Vector2> row = rowDict[y];
                // Don't sort here - will be sorted in local space later
                pegRows.Add(row);
            }

            Debug.Log($"[PathCalc] Extracted {pegRows.Count} peg rows from board");
            return pegRows;
        }
    }
}