using System;
using UnityEngine;
using TuringSignal.Grid;

namespace TuringSignal.Gameplay
{
    public sealed class GridItemLogic : IBoardInteractable
    {
        private readonly GridMap gridMap;

        public string InteractableId { get; }
        public Vector2Int GridPosition { get; }
        public bool IsConsumed { get; private set; }

        public event Action<GridItemLogic> OnInteracted;

        public GridItemLogic(GridMap gridMap, string interactableId, Vector2Int gridPosition)
        {
            this.gridMap = gridMap;
            InteractableId = string.IsNullOrWhiteSpace(interactableId) ? "Item" : interactableId;
            GridPosition = gridPosition;
        }

        public bool CanInteract(RobotLogic robotLogic)
        {
            if (IsConsumed)
            {
                return false;
            }

            if (robotLogic.CarriesKey)
            {
                return false;
            }

            return true;
        }

        public void Interact(RobotLogic robotLogic)
        {
            if (IsConsumed)
            {
                return;
            }

            IsConsumed = true;
            gridMap.ClearInteractable(GridPosition);
            OnInteracted?.Invoke(this);

            Debug.Log($"Interacted with '{InteractableId}' at {GridPosition}.");
        }
    }
}
