using System;
using UnityEngine;
using TuringSignal.Core.Data;
using TuringSignal.Grid;

namespace TuringSignal.Gameplay
{
    public sealed class RobotLogic
    {
        private readonly GridMap gridMap;
        private KeyColor? carriedKey;

        public Vector2Int GridPosition { get; private set; }
        public Direction FacingDirection { get; private set; }
        public RobotIntent PendingIntent { get; private set; }
        public RobotIntent LockedIntent { get; private set; }

        public KeyColor? CarriedKey => carriedKey;
        public bool CarriesKey => carriedKey.HasValue;

        public event Action<KeyColor?> OnCarriedKeyChanged;
        public event Action<Vector2Int, Vector2Int> OnMoveSucceeded;
        public event Action<Vector2Int> OnMoveBlocked;
        public event Action<RobotIntent> OnIntentChanged;
        public event Action<Vector2Int> OnInteractSucceeded;
        public event Action<Vector2Int> OnInteractFailed;

        public RobotLogic(GridMap gridMap, Vector2Int startPosition, Direction startDirection)
        {
            this.gridMap = gridMap;
            GridPosition = startPosition;
            FacingDirection = startDirection;
            PendingIntent = RobotIntent.CreateMove(startDirection);
            LockedIntent = PendingIntent;
        }

        public void BeginDecisionWindow()
        {
            PendingIntent = RobotIntent.CreateMove(FacingDirection);
            OnIntentChanged?.Invoke(PendingIntent);
        }

        public void RotateIntentClockwise()
        {
            if (PendingIntent.Type == IntentType.Interact)
            {
                return;
            }

            Direction nextDirection = DirectionUtility.RotateClockwise(PendingIntent.Direction);
            PendingIntent = RobotIntent.CreateMove(nextDirection);
            FacingDirection = nextDirection;
            OnIntentChanged?.Invoke(PendingIntent);
        }

        public void SetInteractIntent()
        {
            PendingIntent = RobotIntent.CreateInteract(FacingDirection);
            OnIntentChanged?.Invoke(PendingIntent);
        }

        public bool TryPickupKey(KeyColor keyColor)
        {
            if (carriedKey.HasValue)
            {
                return false;
            }

            carriedKey = keyColor;
            OnCarriedKeyChanged?.Invoke(carriedKey);
            return true;
        }

        public bool TryConsumeCarriedKeyForLock(KeyColor lockColor)
        {
            if (!carriedKey.HasValue || carriedKey.Value != lockColor)
            {
                return false;
            }

            carriedKey = null;
            OnCarriedKeyChanged?.Invoke(null);
            return true;
        }

        /// <summary>
        /// True if the cell in front of the robot (same as <see cref="ExecuteInteract"/>) has an interactable that accepts interaction right now.
        /// Uses <see cref="FacingDirection"/>, which is kept in sync with the move intent when rotating during the decision window.
        /// </summary>
        public bool HasInteractableInFront()
        {
            Vector2Int targetCell = GridPosition + DirectionUtility.ToVector2Int(FacingDirection);

            if (!gridMap.TryGetInteractable(targetCell, out IBoardInteractable interactable))
            {
                return false;
            }

            return interactable.CanInteract(this);
        }

        public void ExecutePendingIntent()
        {
            LockedIntent = PendingIntent;

            switch (LockedIntent.Type)
            {
                case IntentType.Move:
                    ExecuteMove(LockedIntent.Direction);
                    break;
                case IntentType.Interact:
                    ExecuteInteract();
                    break;
            }
        }

        private void ExecuteMove(Direction direction)
        {
            Vector2Int from = GridPosition;
            Vector2Int delta = DirectionUtility.ToVector2Int(direction);
            Vector2Int target = from + delta;

            FacingDirection = direction;

            if (!gridMap.CanMoveTo(target))
            {
                OnMoveBlocked?.Invoke(target);
                return;
            }

            GridPosition = target;
            OnMoveSucceeded?.Invoke(from, target);
        }

        private void ExecuteInteract()
        {
            Vector2Int targetCell = GridPosition + DirectionUtility.ToVector2Int(FacingDirection);

            if (!gridMap.TryGetInteractable(targetCell, out IBoardInteractable interactable))
            {
                OnInteractFailed?.Invoke(targetCell);
                return;
            }

            if (!interactable.CanInteract(this))
            {
                OnInteractFailed?.Invoke(targetCell);
                return;
            }

            interactable.Interact(this);
            OnInteractSucceeded?.Invoke(targetCell);
        }
    }
}
