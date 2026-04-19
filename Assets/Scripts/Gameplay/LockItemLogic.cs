using System;
using TuringSignal.Core.Data;
using TuringSignal.Grid;
using UnityEngine;

namespace TuringSignal.Gameplay
{
    public sealed class LockItemLogic : IBoardInteractable
    {
        private readonly GridMap gridMap;

        public KeyColor Color { get; }
        public Vector2Int GridPosition { get; }
        public bool HasKeyPlaced { get; private set; }

        public event Action<LockItemLogic> OnKeyPlaced;

        public LockItemLogic(GridMap gridMap, KeyColor color, Vector2Int gridPosition)
        {
            this.gridMap = gridMap;
            Color = color;
            GridPosition = gridPosition;
        }

        public bool CanInteract(RobotLogic robotLogic)
        {
            if (HasKeyPlaced)
            {
                return false;
            }

            if (!robotLogic.CarriesKey)
            {
                return false;
            }

            return robotLogic.CarriedKey == Color;
        }

        public void Interact(RobotLogic robotLogic)
        {
            if (!CanInteract(robotLogic))
            {
                return;
            }

            if (!robotLogic.TryConsumeCarriedKeyForLock(Color))
            {
                return;
            }

            HasKeyPlaced = true;
            gridMap.ClearInteractable(GridPosition);
            OnKeyPlaced?.Invoke(this);
        }
    }
}
