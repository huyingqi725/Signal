using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TuringSignal.UI
{
    public sealed class PauseMenuUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject menuRoot;

        [Tooltip("继续/菜单按钮：与 Esc 相同，未暂停→打开暂停菜单，已暂停→关闭。若需未暂停时也能点开菜单，请把按钮放在 menuRoot 外（始终可点）。")]
        [SerializeField] private Button resumeButton;

        [Tooltip("可选：第二颗按钮，与 Resume 行为相同；若与 Resume 为同一引用可留空。")]
        [SerializeField] private Button canvasPauseToggleButton;

        [Header("Input")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

        private bool isPaused;

        private void Awake()
        {
            SetMenuVisible(false);

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(OnTogglePauseClicked);
                resumeButton.onClick.AddListener(OnTogglePauseClicked);
            }

            if (canvasPauseToggleButton != null
                && !ReferenceEquals(canvasPauseToggleButton, resumeButton))
            {
                canvasPauseToggleButton.onClick.RemoveListener(OnTogglePauseClicked);
                canvasPauseToggleButton.onClick.AddListener(OnTogglePauseClicked);
            }
        }

        private void Update()
        {
            if (!UnityEngine.Input.GetKeyDown(pauseKey))
            {
                return;
            }

            TogglePause();
        }

        private void OnDestroy()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(OnTogglePauseClicked);
            }

            if (canvasPauseToggleButton != null
                && !ReferenceEquals(canvasPauseToggleButton, resumeButton))
            {
                canvasPauseToggleButton.onClick.RemoveListener(OnTogglePauseClicked);
            }

            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }
        }

        private void OnTogglePauseClicked()
        {
            TogglePause();
        }

        /// <summary>与 Esc 相同：未暂停→打开暂停菜单，已暂停→关闭。</summary>
        private void TogglePause()
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        public void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;
            SetMenuVisible(true);
        }

        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;
            SetMenuVisible(false);
        }

        public void LoadLevelByName(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("PauseMenuUI received an empty scene name.");
                return;
            }

            ResumeForSceneLoad();
            SceneManager.LoadScene(sceneName);
        }

        public void LoadLevelByBuildIndex(int buildIndex)
        {
            if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogWarning($"PauseMenuUI received invalid build index: {buildIndex}.");
                return;
            }

            ResumeForSceneLoad();
            SceneManager.LoadScene(buildIndex);
        }

        private void ResumeForSceneLoad()
        {
            Time.timeScale = 1f;
            isPaused = false;
        }

        private void SetMenuVisible(bool visible)
        {
            if (menuRoot != null)
            {
                menuRoot.SetActive(visible);
            }
        }
    }
}
