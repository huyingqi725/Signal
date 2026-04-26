using System;
using UnityEngine;
using TuringSignal.Core.Tick;
using TuringSignal.Gameplay;

namespace TuringSignal.Input
{
    /// <summary>
    /// 键盘与 UI（红绿灯按钮）共用同一套意图；同帧内旋转/交互各最多生效一次，避免 Space+按钮或 Submit 重复触发。
    /// </summary>
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

        /// <summary>本帧内只允许一次旋转意图（键盘 Space 与 UI 红灯共用，避免同帧各触发一次变成 180°）。</summary>
        private int _rotateIntentConsumedFrame = -1;

        /// <summary>本帧内只允许一次交互意图（键盘 E 与 UI 绿灯共用）。</summary>
        private int _interactIntentConsumedFrame = -1;

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
            if (!CanAcceptIntentThisFrame())
            {
                return;
            }

            if (UnityEngine.Input.GetKeyDown(rotateKey)
                && TryConsumeRotateIntentThisFrame())
            {
                ApplyRotateIntent();
            }

            if (UnityEngine.Input.GetKeyDown(interactKey)
                && TryConsumeInteractIntentThisFrame())
            {
                TryApplyInteractIntent();
            }
        }

        /// <summary>
        /// Red light / Space equivalent — UI button or touch should call this (same rules as keyboard).
        /// </summary>
        public void TryRotateFromUi()
        {
            if (!CanAcceptIntentThisFrame())
            {
                return;
            }

            if (!TryConsumeRotateIntentThisFrame())
            {
                return;
            }

            ApplyRotateIntent();
        }

        /// <summary>
        /// Green light / E equivalent — UI button or touch should call this (same rules as keyboard).
        /// </summary>
        public void TryInteractFromUi()
        {
            if (!CanAcceptIntentThisFrame())
            {
                return;
            }

            if (!TryConsumeInteractIntentThisFrame())
            {
                return;
            }

            TryApplyInteractIntent();
        }

        private bool TryConsumeRotateIntentThisFrame()
        {
            int frame = Time.frameCount;

            if (frame == _rotateIntentConsumedFrame)
            {
                return false;
            }

            _rotateIntentConsumedFrame = frame;
            return true;
        }

        private bool TryConsumeInteractIntentThisFrame()
        {
            int frame = Time.frameCount;

            if (frame == _interactIntentConsumedFrame)
            {
                return false;
            }

            _interactIntentConsumedFrame = frame;
            return true;
        }

        private bool CanAcceptIntentThisFrame()
        {
            return inputEnabled
                && tickManager != null
                && robotLogic != null
                && tickManager.IsDecisionWindowOpen;
        }

        private void ApplyRotateIntent()
        {
            robotLogic.RotateIntentClockwise();
            OnRotatePressed?.Invoke();
        }

        private void TryApplyInteractIntent()
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
