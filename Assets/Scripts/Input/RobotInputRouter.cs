using UnityEngine;
using TuringSignal.Core.Tick;
using TuringSignal.Gameplay;

namespace TuringSignal.Input
{
    public sealed class RobotInputRouter : MonoBehaviour
    {
        [SerializeField] private KeyCode rotateKey = KeyCode.Space;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private TickManager tickManager;
        private RobotLogic robotLogic;

        public void Initialize(TickManager tickManager, RobotLogic robotLogic)
        {
            this.tickManager = tickManager;
            this.robotLogic = robotLogic;
        }

        private void Update()
        {
            if (tickManager == null || robotLogic == null)
            {
                return;
            }

            if (!tickManager.IsDecisionWindowOpen)
            {
                return;
            }

            if (UnityEngine.Input.GetKeyDown(rotateKey))
            {
                robotLogic.RotateIntentClockwise();
            }

            if (UnityEngine.Input.GetKeyDown(interactKey))
            {
                robotLogic.SetInteractIntent();
            }
        }
    }
}
