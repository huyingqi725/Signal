namespace TuringSignal.Core.Data
{
    public enum IntentType
    {
        Move,
        Interact
    }

    public readonly struct RobotIntent
    {
        public IntentType Type { get; }
        public Direction Direction { get; }

        public RobotIntent(IntentType type, Direction direction)
        {
            Type = type;
            Direction = direction;
        }

        public static RobotIntent CreateMove(Direction direction)
        {
            return new RobotIntent(IntentType.Move, direction);
        }

        public static RobotIntent CreateInteract(Direction direction)
        {
            return new RobotIntent(IntentType.Interact, direction);
        }
    }
}
