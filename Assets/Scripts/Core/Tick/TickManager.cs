using System;
using UnityEngine;

namespace TuringSignal.Core.Tick
{
    public sealed class TickManager : MonoBehaviour
    {
        [SerializeField] private float tickDuration = 0.3f;

        public event Action<int> OnDecisionWindowStarted;
        public event Action<int> OnTickExecuted;

        public int CurrentTickIndex { get; private set; }
        public float TickDuration => tickDuration;
        public float TickProgressNormalized => tickDuration <= 0f ? 0.1f : Mathf.Clamp01(elapsedTime / tickDuration);
        public bool IsDecisionWindowOpen { get; private set; }

        private float elapsedTime;

        private void Start()
        {
            CurrentTickIndex = 0;
            elapsedTime = 0f;
            IsDecisionWindowOpen = true;
            OnDecisionWindowStarted?.Invoke(CurrentTickIndex);
        }

        private void Update()
        {
            if (tickDuration <= 0f)
            {
                return;
            }

            elapsedTime += Time.deltaTime;

            if (elapsedTime < tickDuration)
            {
                return;
            }

            IsDecisionWindowOpen = false;
            // Callback runs while CurrentTickIndex is still the executing tick; it increments only after this returns.
            OnTickExecuted?.Invoke(CurrentTickIndex);

            CurrentTickIndex++;
            elapsedTime = 0f;
            IsDecisionWindowOpen = true;
            OnDecisionWindowStarted?.Invoke(CurrentTickIndex);
        }
    }
}
