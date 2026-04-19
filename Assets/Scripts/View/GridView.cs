using System;
using UnityEngine;

namespace TuringSignal.View
{
    public sealed class GridView : MonoBehaviour
    {
        [SerializeField] private Vector2 cellSize = Vector2.one;
        [SerializeField] private bool centerGridOnOrigin = true;
        [SerializeField] private Vector2 originOffset = Vector2.zero;
        [Header("Gizmos")]
        [SerializeField] private bool drawGridGizmos = true;
        [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.45f);
        [SerializeField] private Color blockedCellColor = new Color(1f, 0.2f, 0.2f, 0.45f);
        [SerializeField] private Color spawnCellColor = new Color(0.2f, 1f, 0.6f, 0.6f);
        [SerializeField] private Color goalCellColor = new Color(1f, 0.85f, 0.2f, 0.75f);
        [SerializeField] private Color oddTrapColor = new Color(1f, 0.45f, 0.2f, 0.75f);
        [SerializeField] private Color evenTrapColor = new Color(0.8f, 0.35f, 1f, 0.75f);
        [SerializeField] private Color interactableCellColor = new Color(0.2f, 0.9f, 1f, 0.75f);

        private int previewWidth = 18;
        private int previewHeight = 12;
        private Vector2Int previewSpawnGridPosition = Vector2Int.zero;
        private Vector2Int previewGoalGridPosition = new Vector2Int(1, 0);
        private Vector2Int[] previewBlockedCells = Array.Empty<Vector2Int>();
        private Vector2Int[] previewOddTrapCells = Array.Empty<Vector2Int>();
        private Vector2Int[] previewEvenTrapCells = Array.Empty<Vector2Int>();
        private Vector2Int[] previewInteractableCells = Array.Empty<Vector2Int>();
        private bool previewOddTrapPhaseActive = true;

        public Vector3 GridToWorld(Vector2Int gridPosition)
        {
            Vector2 gridOrigin = GetGridOrigin();

            return new Vector3(
                gridOrigin.x + (gridPosition.x * cellSize.x),
                gridOrigin.y + (gridPosition.y * cellSize.y),
                0f);
        }

        public void ConfigurePreview(
            int width,
            int height,
            Vector2Int spawnGridPosition,
            Vector2Int goalGridPosition,
            Vector2Int[] blockedCells,
            Vector2Int[] oddTrapCells,
            Vector2Int[] evenTrapCells,
            bool oddTrapPhaseActive,
            Vector2Int[] interactableCells)
        {
            previewWidth = Mathf.Max(1, width);
            previewHeight = Mathf.Max(1, height);
            previewSpawnGridPosition = spawnGridPosition;
            previewGoalGridPosition = goalGridPosition;
            previewBlockedCells = blockedCells != null ? (Vector2Int[])blockedCells.Clone() : Array.Empty<Vector2Int>();
            previewOddTrapCells = oddTrapCells != null ? (Vector2Int[])oddTrapCells.Clone() : Array.Empty<Vector2Int>();
            previewEvenTrapCells = evenTrapCells != null ? (Vector2Int[])evenTrapCells.Clone() : Array.Empty<Vector2Int>();
            previewOddTrapPhaseActive = oddTrapPhaseActive;
            previewInteractableCells = interactableCells != null ? (Vector2Int[])interactableCells.Clone() : Array.Empty<Vector2Int>();
        }

        private Vector2 GetGridOrigin()
        {
            if (!centerGridOnOrigin)
            {
                return originOffset;
            }

            float centeredOriginX = -((previewWidth - 1) * cellSize.x * 0.5f);
            float centeredOriginY = -((previewHeight - 1) * cellSize.y * 0.5f);

            return new Vector2(
                centeredOriginX + originOffset.x,
                centeredOriginY + originOffset.y);
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

            DrawTrapCells(previewOddTrapCells, cellWorldSize, previewOddTrapPhaseActive ? oddTrapColor : DimColor(oddTrapColor));
            DrawTrapCells(previewEvenTrapCells, cellWorldSize, previewOddTrapPhaseActive ? DimColor(evenTrapColor) : evenTrapColor);

            Gizmos.color = interactableCellColor;

            for (int i = 0; i < previewInteractableCells.Length; i++)
            {
                Vector3 interactableCenter = GridToWorld(previewInteractableCells[i]);
                Gizmos.DrawCube(interactableCenter, cellWorldSize * 0.25f);
            }

            Gizmos.color = spawnCellColor;
            Gizmos.DrawCube(GridToWorld(previewSpawnGridPosition), cellWorldSize * 0.45f);

            Gizmos.color = goalCellColor;
            Gizmos.DrawCube(GridToWorld(previewGoalGridPosition), cellWorldSize * 0.4f);
        }

        private void DrawTrapCells(Vector2Int[] trapCells, Vector3 cellWorldSize, Color color)
        {
            if (trapCells == null || trapCells.Length == 0)
            {
                return;
            }

            Gizmos.color = color;

            for (int i = 0; i < trapCells.Length; i++)
            {
                Vector3 trapCenter = GridToWorld(trapCells[i]);
                Gizmos.DrawCube(trapCenter, cellWorldSize * 0.35f);
            }
        }

        private static Color DimColor(Color color)
        {
            return new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, color.a * 0.45f);
        }
    }
}
