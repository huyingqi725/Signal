using UnityEngine;
using TuringSignal.Gameplay;

namespace TuringSignal.Grid
{
    public sealed class GridCell
    {
        public Vector2Int Coordinate { get; }
        public bool IsWalkable { get; private set; }
        public bool HasTrap { get; private set; }
        public IBoardInteractable Interactable { get; private set; }

        public bool HasInteractable => Interactable != null;

        public GridCell(Vector2Int coordinate, bool isWalkable = true)
        {
            Coordinate = coordinate;
            IsWalkable = isWalkable;
        }

        public void SetWalkable(bool isWalkable)
        {
            IsWalkable = isWalkable;
        }

        public void SetTrap(bool hasTrap)
        {
            HasTrap = hasTrap;
        }

        public void SetInteractable(IBoardInteractable interactable)
        {
            Interactable = interactable;
        }

        public void ClearInteractable()
        {
            Interactable = null;
        }
    }
}
