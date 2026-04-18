using System;
using UnityEngine;

namespace TuringSignal.View
{
    public sealed class GridView : MonoBehaviour
    {
        [SerializeField] private Vector2 cellSize = Vector2.one;
        [SerializeField] private Vector2 originOffset = Vector2.zero;
        [Header("Gizmos")]
        [SerializeField] private bool drawGridGizmos = true;
        [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.45f);
        [SerializeField] private Color blockedCellColor = new Color(1f, 0.2f, 0.2f, 0.45f);
        [SerializeField] private Color spawnCellColor = new Color(0.2f, 1f, 0.6f, 0.6f);
        [SerializeField] private Color goalCellColor = new Color(1f, 0.85f, 0.2f, 0.75f);
        [SerializeField] private Color trapCellColor = new Color(0.8f, 0.35f, 1f, 0.55f);

        private int previewWidth = 18;
        private int previewHeight = 12;
        private Vector2Int previewSpawnGridPosition = Vector2Int.zero;
        private Vector2Int previewGoalGridPosition = new Vector2Int(1, 0);
        private Vector2Int[] previewBlockedCells = Array.Empty<Vector2Int>();
        private Vector2Int[] previewTrapCells = Array.Empty<Vector2Int>();
        private bool previewTrapsVisible = true;

        public Vector3 GridToWorld(Vector2Int gridPosition)
        {
            return new Vector3(
                originOffset.x + (gridPosition.x * cellSize.x),
                originOffset.y + (gridPosition.y * cellSize.y),
                0f);
        }

        public void ConfigurePreview(
            int width,
            int height,
            Vector2Int spawnGridPosition,
            Vector2Int goalGridPosition,
            Vector2Int[] blockedCells,
            Vector2Int[] trapCells,
            bool trapsVisible)
        {
            previewWidth = Mathf.Max(1, width);
            previewHeight = Mathf.Max(1, height);
            previewSpawnGridPosition = spawnGridPosition;
            previewGoalGridPosition = goalGridPosition;
            previewBlockedCells = blockedCells != null ? (Vector2Int[])blockedCells.Clone() : Array.Empty<Vector2Int>();
            previewTrapCells = trapCells != null ? (Vector2Int[])trapCells.Clone() : Array.Empty<Vector2Int>();
            previewTrapsVisible = trapsVisible;
        }

        private void OnDrawGizmos()
        {
            if (!drawGridGizmos)
            {
                return;
            }

            Vector3 cellWorldSize = new Vector3(cellSize.x, cellSize.y, 0.01f);

            Gizmos.color = gridColor;

            for (int x = 0; x < previewWidth; x++)
            {
                for (int y = 0; y < previewHeight; y++)
                {
                    Vector3 cellCenter = GridToWorld(new Vector2Int(x, y));
                    Gizmos.DrawWireCube(cellCenter, cellWorldSize);
                }
            }

            Gizmos.color = blockedCellColor;

            for (int i = 0; i < previewBlockedCells.Length; i++)
            {
                Vector3 blockedCenter = GridToWorld(previewBlockedCells[i]);
                Gizmos.DrawCube(blockedCenter, cellWorldSize * 0.65f);
            }

            if (previewTrapsVisible)
            {
                Gizmos.color = trapCellColor;

                for (int i = 0; i < previewTrapCells.Length; i++)
                {
                    Vector3 trapCenter = GridToWorld(previewTrapCells[i]);
                    Gizmos.DrawCube(trapCenter, cellWorldSize * 0.35f);
                }
            }

            Gizmos.color = spawnCellColor;
            Gizmos.DrawCube(GridToWorld(previewSpawnGridPosition), cellWorldSize * 0.45f);

            Gizmos.color = goalCellColor;
            Gizmos.DrawCube(GridToWorld(previewGoalGridPosition), cellWorldSize * 0.4f);
        }
    }
}
