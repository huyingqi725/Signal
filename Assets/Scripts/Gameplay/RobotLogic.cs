using System;
using UnityEngine;
using TuringSignal.Core.Data;
using TuringSignal.Grid;

namespace TuringSignal.Gameplay
{
    public sealed class RobotLogic
    {
        private readonly GridMap gridMap;

        public Vector2Int GridPosition { get; private set; }
        public Direction FacingDirection { get; private set; }
        public RobotIntent PendingIntent { get; private set; }
        public RobotIntent LockedIntent { get; private set; }

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
            OnIntentChanged?.Invoke(PendingIntent);
        }

        public void SetInteractIntent()
        {
            PendingIntent = RobotIntent.CreateInteract(FacingDirection);
            OnIntentChanged?.Invoke(PendingIntent);
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

            if (gridMap.HasTrapAt(target))
            {
                Debug.Log("Robot stepped on a trap cell.");
            }
        }

        private void ExecuteInteract()
        {
            Vector2Int targetCell = GridPosition + DirectionUtility.ToVector2Int(FacingDirection);

            if (!gridMap.TryGetInteractable(targetCell, out IBoardInteractable interactable))
            {
                Debug.Log($"No interactable found in front cell {targetCell}.");
                OnInteractFailed?.Invoke(targetCell);
                return;
            }

            if (!interactable.CanInteract(this))
            {
                Debug.Log("Interactable rejected the interaction.");
                OnInteractFailed?.Invoke(targetCell);
                return;
            }

            interactable.Interact(this);
            OnInteractSucceeded?.Invoke(targetCell);
        }
    }
}
