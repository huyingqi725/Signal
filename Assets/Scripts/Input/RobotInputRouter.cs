using System;
using UnityEngine;
using TuringSignal.Core.Tick;
using TuringSignal.Gameplay;

namespace TuringSignal.Input
{
    public sealed class RobotInputRouter : MonoBehaviour
    {
        [SerializeField] private KeyCode rotateKey = KeyCode.Space;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        public event Action OnRotatePressed;
        public event Action OnInteractPressed;

        private TickManager tickManager;
        private RobotLogic robotLogic;
        private bool inputEnabled = true;
        private bool restrictInteractToFacingInteractable = true;

        public void Initialize(
            TickManager tickManager,
            RobotLogic robotLogic,
            bool restrictInteractToFacingInteractable = true)
        {
            this.tickManager = tickManager;
            this.robotLogic = robotLogic;
            this.restrictInteractToFacingInteractable = restrictInteractToFacingInteractable;
        }

        public void SetInputEnabled(bool isEnabled)
        {
            inputEnabled = isEnabled;
        }

        private void Update()
        {
            if (!inputEnabled || tickManager == null || robotLogic == null)
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
                OnRotatePressed?.Invoke();
            }

            if (UnityEngine.Input.GetKeyDown(interactKey))
            {
                if (restrictInteractToFacingInteractable && !robotLogic.HasInteractableInFront())
                {
                    return;
                }

                robotLogic.SetInteractIntent();
                OnInteractPressed?.Invoke();
            }
        }
    }
}
