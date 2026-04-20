using UnityEngine;

namespace TuringSignal.Core.Data
{
    public enum Direction
    {
        Up,
        Right,
        Down,
        Left
    }

    public static class DirectionUtility
    {
        public static Vector2Int ToVector2Int(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return Vector2Int.up;
                case Direction.Right:
                    return Vector2Int.right;
                case Direction.Down:
                    return Vector2Int.down;
                case Direction.Left:
                    return Vector2Int.left;
                default:
                    return Vector2Int.zero;
            }
        }

        public static Direction RotateClockwise(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Left;
                case Direction.Left:
                    return Direction.Up;
                default:
                    return direction;
            }
        }

        /// <summary>
        /// For a baby lock: <paramref name="mouthOutward"/> is the direction the slot/opening points (开口朝向).
        /// Returns the <see cref="Direction"/> the robot must face from the adjacent cell to interact into the lock.
        /// </summary>
        public static Direction RequiredRobotFacingForLockMouthOutward(Direction mouthOutward)
        {
            switch (mouthOutward)
            {
                case Direction.Down:
                    return Direction.Up;
                case Direction.Up:
                    return Direction.Down;
                case Direction.Left:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Left;
                default:
                    return mouthOutward;
            }
        }
    }
}
