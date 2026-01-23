using UnityEngine;
using System.Collections.Generic;

namespace PlinkoGame
{
    /// <summary>
    /// Calculates ball path from start to target catcher
    /// Updated with proper encapsulation
    /// </summary>
    public class PathCalculator : MonoBehaviour
    {
        [Header("Path Settings")]
        [SerializeField] private float horizontalVariation = 0.03f;
        [SerializeField] private float pegPassOffset = 0.3f;
        [SerializeField] private float catcherApproachOffset = 0.5f;
        [SerializeField] private bool debugPath = true;

        // ============================================
        // PATH CALCULATION
        // ============================================

        public List<Vector2> CalculatePath(Vector2 startPos, Transform targetCatcher, List<List<Vector2>> pegRows)
        {
            List<Vector2> path = new List<Vector2>();
            path.Add(startPos);

            if (targetCatcher == null)
            {
                Debug.LogError("Target catcher is null!");
                return path;
            }

            Vector2 targetPos = targetCatcher.position;
            int totalRows = pegRows.Count;

            if (debugPath)
            {
                Debug.Log($"PATH: Start {startPos:F2} → Target {targetCatcher.name} at {targetPos:F2}, Rows: {totalRows}");
            }

            float currentX = startPos.x;
            float targetX = targetPos.x;
            float totalDistance = targetX - currentX;
            float stepPerRow = totalDistance / (totalRows + 1);

            if (debugPath)
            {
                Debug.Log($"PATH: Distance {totalDistance:F3}, Step/row {stepPerRow:F3}");
            }

            // Generate waypoints for each peg row
            for (int row = 0; row < totalRows; row++)
            {
                List<Vector2> rowPegs = pegRows[row];
                if (rowPegs.Count == 0) continue;

                // Calculate ideal X position for this row
                float idealX = startPos.x + (stepPerRow * (row + 1));

                // Find closest peg to ideal path
                Vector2 closestPeg = FindClosestPeg(rowPegs, idealX);

                // Determine which side to pass the peg on
                float offsetDirection = (idealX < closestPeg.x) ? -1f : 1f;

                // Create waypoint to the SIDE of the peg
                float waypointX = closestPeg.x + (offsetDirection * pegPassOffset);
                float waypointY = closestPeg.y - 0.2f;

                // Add tiny random variation for natural movement
                waypointX += Random.Range(-horizontalVariation, horizontalVariation);

                Vector2 waypoint = new Vector2(waypointX, waypointY);
                path.Add(waypoint);

                if (debugPath && row % 4 == 0)
                {
                    Debug.Log($"  Row {row}: Ideal {idealX:F2}, Peg {closestPeg.x:F2}, Waypoint {waypoint:F2}");
                }
            }

            // Add intermediate waypoint before catcher
            Vector2 approachPoint = new Vector2(targetX, targetPos.y + catcherApproachOffset);
            path.Add(approachPoint);

            // Final destination
            path.Add(targetPos);

            if (debugPath)
            {
                Debug.Log($"PATH: Generated {path.Count} waypoints, Final: {targetPos:F2}");
            }

            return path;
        }

        private Vector2 FindClosestPeg(List<Vector2> pegs, float targetX)
        {
            Vector2 closest = pegs[0];
            float minDist = Mathf.Abs(closest.x - targetX);

            foreach (Vector2 peg in pegs)
            {
                float dist = Mathf.Abs(peg.x - targetX);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = peg;
                }
            }

            return closest;
        }

        // ============================================
        // BOARD DATA EXTRACTION
        // ============================================

        public List<List<Vector2>> GetPegRowsFromBoard(BoardController board)
        {
            List<List<Vector2>> pegRows = new List<List<Vector2>>();
            Transform boardTransform = board.transform;
            Dictionary<float, List<Vector2>> rowDict = new Dictionary<float, List<Vector2>>();

            // Collect all active pegs organized by Y position
            for (int i = 0; i < boardTransform.childCount; i++)
            {
                Transform child = boardTransform.GetChild(i);

                if (child.gameObject.activeSelf && child.CompareTag("Peg"))
                {
                    float yPos = Mathf.Round(child.position.y * 100f) / 100f;

                    if (!rowDict.ContainsKey(yPos))
                    {
                        rowDict[yPos] = new List<Vector2>();
                    }

                    rowDict[yPos].Add(child.position);
                }
            }

            // Sort rows top to bottom
            List<float> sortedY = new List<float>(rowDict.Keys);
            sortedY.Sort((a, b) => b.CompareTo(a));

            foreach (float y in sortedY)
            {
                List<Vector2> row = rowDict[y];
                row.Sort((a, b) => a.x.CompareTo(b.x));
                pegRows.Add(row);
            }

            Debug.Log($"Found {pegRows.Count} peg rows");
            return pegRows;
        }
    }
}