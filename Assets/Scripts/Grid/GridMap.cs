using UnityEngine;
using TuringSignal.Gameplay;

namespace TuringSignal.Grid
{
    public sealed class GridMap
    {
        private readonly GridCell[,] cells;

        public int Width { get; }
        public int Height { get; }

        public GridMap(int width, int height)
        {
            Width = width;
            Height = height;
            cells = new GridCell[width, height];

            Initialize();
        }

        private void Initialize()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    cells[x, y] = new GridCell(new Vector2Int(x, y));
                }
            }
        }

        public bool IsInside(Vector2Int coordinate)
        {
            return coordinate.x >= 0
                && coordinate.x < Width
                && coordinate.y >= 0
                && coordinate.y < Height;
        }

        public bool TryGetCell(Vector2Int coordinate, out GridCell cell)
        {
            if (!IsInside(coordinate))
            {
                cell = null;
                return false;
            }

            cell = cells[coordinate.x, coordinate.y];
            return true;
        }

        public bool CanMoveTo(Vector2Int coordinate)
        {
            if (!TryGetCell(coordinate, out GridCell cell))
            {
                return false;
            }

            return cell.IsWalkable;
        }

        public bool HasTrapAt(Vector2Int coordinate)
        {
            return TryGetCell(coordinate, out GridCell cell) && cell.HasTrap;
        }

        public bool TryGetInteractable(Vector2Int coordinate, out IBoardInteractable interactable)
        {
            if (!TryGetCell(coordinate, out GridCell cell))
            {
                interactable = null;
                return false;
            }

            interactable = cell.Interactable;
            return interactable != null;
        }

        public void SetWalkable(Vector2Int coordinate, bool isWalkable)
        {
            if (TryGetCell(coordinate, out GridCell cell))
            {
                cell.SetWalkable(isWalkable);
            }
        }

        public void SetTrap(Vector2Int coordinate, bool hasTrap)
        {
            if (TryGetCell(coordinate, out GridCell cell))
            {
                cell.SetTrap(hasTrap);
            }
        }

        public void SetInteractable(Vector2Int coordinate, IBoardInteractable interactable)
        {
            if (TryGetCell(coordinate, out GridCell cell))
            {
                cell.SetInteractable(interactable);
            }
        }

        public void ClearInteractable(Vector2Int coordinate)
        {
            if (TryGetCell(coordinate, out GridCell cell))
            {
                cell.ClearInteractable();
            }
        }
    }
}
