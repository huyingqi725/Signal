using System;
using TuringSignal.Core.Data;
using TuringSignal.Grid;
using UnityEngine;

namespace TuringSignal.Gameplay
{
    /// <summary>
    /// Directional lock: same key color rules as <see cref="LockItemLogic"/>, plus a slot orientation.
    /// <see cref="InteractionFace"/> is the direction the <b>opening points</b> (开口朝向); the robot must stand
    /// on the neighbor cell and face <see cref="DirectionUtility.RequiredRobotFacingForLockMouthOutward"/>.
    /// </summary>
    public sealed class BabyLockItemLogic : IBoardInteractable
    {
        /// <summary>Set by KeyLockView when its debug checkbox is enabled.</summary>
        public static bool DiagnosticsEnabled { get; set; }

        private readonly GridMap gridMap;

        public KeyColor Color { get; }
        public Vector2Int GridPosition { get; }
        /// <summary>Opening points this way (mouth outward / 开口朝向).</summary>
        public Direction InteractionFace { get; }
        public bool HasKeyPlaced { get; private set; }

        public event Action<BabyLockItemLogic> OnKeyPlaced;

        public BabyLockItemLogic(GridMap gridMap, KeyColor color, Vector2Int gridPosition, Direction interactionFace)
        {
            this.gridMap = gridMap;
            Color = color;
            GridPosition = gridPosition;
            InteractionFace = interactionFace;
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

            if (robotLogic.CarriedKey != Color)
            {
                return false;
            }

            Direction requiredFacing = DirectionUtility.RequiredRobotFacingForLockMouthOutward(InteractionFace);

            if (robotLogic.FacingDirection != requiredFacing)
            {
                return false;
            }

            Vector2Int frontCell = robotLogic.GridPosition + DirectionUtility.ToVector2Int(robotLogic.FacingDirection);
            return frontCell == GridPosition;
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

            if (DiagnosticsEnabled)
            {
                Debug.Log(
                    $"[BabyLockLogic] Fill OK — grid={GridPosition} color={Color} mouthOut={InteractionFace} " +
                    $"reqFacing={DirectionUtility.RequiredRobotFacingForLockMouthOutward(InteractionFace)} " +
                    $"HasKeyPlaced={HasKeyPlaced} (logic finished; if no [BabyLockView] line next, view did not refresh.)");
            }

            OnKeyPlaced?.Invoke(this);
        }
    }
}
