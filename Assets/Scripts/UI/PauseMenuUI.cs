using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TuringSignal.UI
{
    public sealed class PauseMenuUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private Button resumeButton;

        [Header("Input")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

        private bool isPaused;

        private void Awake()
        {
            SetMenuVisible(false);

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(ResumeGame);
                resumeButton.onClick.AddListener(ResumeGame);
            }
        }

        private void Update()
        {
            if (!UnityEngine.Input.GetKeyDown(pauseKey))
            {
                return;
            }

            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        private void OnDestroy()
        {
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
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
