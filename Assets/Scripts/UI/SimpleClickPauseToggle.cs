using UnityEngine;
using UnityEngine.UI;

namespace TuringSignal.UI
{
    /// <summary>
    /// 仅通过点击按钮切换暂停：第一次点暂停（timeScale=0），再点继续（timeScale=1）。
    /// 不打开任何菜单、不监听键盘；挂到 UICanvas 上并拖入一个 Button 即可。
    /// </summary>
    public sealed class SimpleClickPauseToggle : MonoBehaviour
    {
        [SerializeField] private Button pauseToggleButton;

        private bool isPaused;

        private void Awake()
        {
            if (pauseToggleButton != null)
            {
                pauseToggleButton.onClick.RemoveListener(TogglePause);
                pauseToggleButton.onClick.AddListener(TogglePause);
            }
        }

        private void OnDestroy()
        {
            if (pauseToggleButton != null)
            {
                pauseToggleButton.onClick.RemoveListener(TogglePause);
            }

            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }
        }

        private void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
        }
    }
}
