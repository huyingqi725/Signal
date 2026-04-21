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

        [Header("Debug")]
        [Tooltip("勾选后：TryRotateFromUi / TryInteractFromUi 因规则被拒时在 Console 打出原因（便于排查按钮无反应）。")]
        [SerializeField] private bool logUiIntentRejections;

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
                TryApplyInteractIntent(false);
            }
        }

        /// <summary>
        /// Red light / Space equivalent — UI button or touch should call this (same rules as keyboard).
        /// </summary>
        public void TryRotateFromUi()
        {
            if (!CanAcceptIntentThisFrame())
            {
                LogUiRejection("TryRotateFromUi");
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
                LogUiRejection("TryInteractFromUi");
                return;
            }

            if (!TryConsumeInteractIntentThisFrame())
            {
                return;
            }

            TryApplyInteractIntent(true);
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

        private void TryApplyInteractIntent(bool fromUi)
        {
            if (restrictInteractToFacingInteractable && !robotLogic.HasInteractableInFront())
            {
                if (fromUi && logUiIntentRejections)
                {
                    Debug.Log(
                        "[RobotInputRouter] TryInteractFromUi: 前方没有可交互物（与按 E 无效时相同）。",
                        this);
                }

                return;
            }

            robotLogic.SetInteractIntent();
            OnInteractPressed?.Invoke();
        }

        private void LogUiRejection(string methodName)
        {
            if (!logUiIntentRejections)
            {
                return;
            }

            string reason;

            if (!inputEnabled)
            {
                reason = "输入已关闭（例如过关过渡中）。";
            }
            else if (tickManager == null || robotLogic == null)
            {
                reason = "尚未 Initialize（缺少 TickManager / RobotLogic 引用）。";
            }
            else if (!tickManager.IsDecisionWindowOpen)
            {
                reason = "当前不在决策窗内（节拍正在执行中，稍等再按）。";
            }
            else
            {
                reason = "未知。";
            }

            Debug.Log($"[RobotInputRouter] {methodName} 未生效：{reason}", this);
        }
    }
}
