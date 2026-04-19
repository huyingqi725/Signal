using System;
using TuringSignal.Core.Data;
using TuringSignal.Grid;
using UnityEngine;

namespace TuringSignal.Gameplay
{
    public sealed class KeyItemLogic : IBoardInteractable
    {
        private readonly GridMap gridMap;

        public KeyColor Color { get; }
        public Vector2Int GridPosition { get; }
        public bool IsPickedUp { get; private set; }

        public event Action<KeyItemLogic> OnPickedUp;

        public KeyItemLogic(GridMap gridMap, KeyColor color, Vector2Int gridPosition)
        {
            this.gridMap = gridMap;
            Color = color;
            GridPosition = gridPosition;
        }

        public bool CanInteract(RobotLogic robotLogic)
        {
            if (IsPickedUp)
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
            if (!CanInteract(robotLogic))
            {
                return;
            }

            if (!robotLogic.TryPickupKey(Color))
            {
                return;
            }

            IsPickedUp = true;
            gridMap.ClearInteractable(GridPosition);
            OnPickedUp?.Invoke(this);
        }
    }
}
