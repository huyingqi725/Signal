namespace TuringSignal.Gameplay
{
    public interface IBoardInteractable
    {
        bool CanInteract(RobotLogic robotLogic);
        void Interact(RobotLogic robotLogic);
    }
}
