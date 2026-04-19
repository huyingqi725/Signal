using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TuringSignal.View
{
    public sealed class GoalView : MonoBehaviour
    {
        [Header("Sprites")]
        [SerializeField] private Sprite defaultGoalSprite;
        [SerializeField] private Sprite reachedGoalSprite;
        [SerializeField] private Sprite armSprite;

        [Header("Rendering")]
        [SerializeField] private Vector3 goalWorldOffset = Vector3.zero;
        [SerializeField] private Vector3 armOffset = new Vector3(0.45f, 0.85f, 0f);
        [SerializeField] private Vector3 captureWorldOffset = new Vector3(4f, 3f, 0f);
        [SerializeField] private int goalSortingOrder = 1;
        [SerializeField] private int armSortingOrder = 5;

        [Header("Timings")]
        [SerializeField] private float armAppearDelay = 0.2f;
        [SerializeField] private float captureMoveDuration = 0.5f;
        [SerializeField] private float fadeDuration = 0.25f;

        private GridView gridView;
        private Vector2Int goalGridPosition;
        private SpriteRenderer goalRenderer;
        private SpriteRenderer armRenderer;

        public void Initialize(GridView gridView, Vector2Int goalGridPosition)
        {
            this.gridView = gridView;
            this.goalGridPosition = goalGridPosition;

            EnsureRenderers();
            RefreshGoalPosition();
            SetDefaultState();
        }

        public void SetDefaultState()
        {
            EnsureRenderers();
            goalRenderer.sprite = defaultGoalSprite;
            armRenderer.enabled = false;
        }

        public IEnumerator PlayGoalSequence(Transform robotTransform, float robotMoveDuration, string nextSceneName)
        {
            EnsureRenderers();

            goalRenderer.sprite = reachedGoalSprite != null ? reachedGoalSprite : defaultGoalSprite;
            armRenderer.enabled = true;

            yield return new WaitForSeconds(robotMoveDuration);
            yield return new WaitForSeconds(armAppearDelay);

            Vector3 robotStart = robotTransform.position;
            Vector3 robotTarget = robotStart + captureWorldOffset;
            Vector3 armStart = robotTransform.position + armOffset;
            Vector3 armTarget = robotTarget + armOffset;

            float elapsed = 0f;

            while (elapsed < captureMoveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / captureMoveDuration);
                robotTransform.position = Vector3.Lerp(robotStart, robotTarget, t);
                armRenderer.transform.position = Vector3.Lerp(armStart, armTarget, t);
                yield return null;
            }

            yield return FadeOutAndIn(nextSceneName);
        }

        private IEnumerator FadeOutAndIn(string nextSceneName)
        {
            FadeOverlayController overlayController = EnsureFadeOverlay();
            overlayController.BeginTransition(nextSceneName, fadeDuration);
            yield break;
        }

        private void EnsureRenderers()
        {
            if (goalRenderer == null)
            {
                GameObject goalObject = new GameObject("GoalSprite");
                goalObject.transform.SetParent(transform, false);
                goalRenderer = goalObject.AddComponent<SpriteRenderer>();
                goalRenderer.sortingOrder = goalSortingOrder;
            }

            if (armRenderer == null)
            {
                GameObject armObject = new GameObject("GoalArm");
                armObject.transform.SetParent(transform, false);
                armRenderer = armObject.AddComponent<SpriteRenderer>();
                armRenderer.sprite = armSprite;
                armRenderer.sortingOrder = armSortingOrder;
            }
        }

        private void RefreshGoalPosition()
        {
            if (gridView == null)
            {
                return;
            }

            Vector3 goalWorldPosition = gridView.GridToWorld(goalGridPosition) + goalWorldOffset;

            if (goalRenderer != null)
            {
                goalRenderer.transform.position = goalWorldPosition;
            }

            if (armRenderer != null)
            {
                armRenderer.transform.position = goalWorldPosition + armOffset;
            }
        }

        private static IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration)
        {
            canvasGroup.alpha = from;

            if (duration <= 0f)
            {
                canvasGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            canvasGroup.alpha = to;
        }

        private static FadeOverlayController EnsureFadeOverlay()
        {
            GameObject fadeRoot = new GameObject("GoalFadeOverlay");
            Object.DontDestroyOnLoad(fadeRoot);

            Canvas canvas = fadeRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            fadeRoot.AddComponent<GraphicRaycaster>();

            CanvasGroup canvasGroup = fadeRoot.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            GameObject imageObject = new GameObject("FadeImage");
            imageObject.transform.SetParent(fadeRoot.transform, false);

            RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Image image = imageObject.AddComponent<Image>();
            image.color = Color.black;

            return fadeRoot.AddComponent<FadeOverlayController>();
        }

        private sealed class FadeOverlayController : MonoBehaviour
        {
            private CanvasGroup canvasGroup;

            private void Awake()
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            public void BeginTransition(string nextSceneName, float fadeDuration)
            {
                StartCoroutine(PlayTransition(nextSceneName, fadeDuration));
            }

            private IEnumerator PlayTransition(string nextSceneName, float fadeDuration)
            {
                yield return FadeCanvasGroup(canvasGroup, 0f, 1f, fadeDuration);

                if (!string.IsNullOrWhiteSpace(nextSceneName))
                {
                    SceneManager.LoadScene(nextSceneName);
                }
                else
                {
                    Scene currentScene = SceneManager.GetActiveScene();
                    int nextBuildIndex = currentScene.buildIndex + 1;

                    if (nextBuildIndex >= 0 && nextBuildIndex < SceneManager.sceneCountInBuildSettings)
                    {
                        SceneManager.LoadScene(nextBuildIndex);
                    }
                    else
                    {
                        Debug.LogWarning("GoalView could not find a next scene to load.");
                    }
                }

                yield return null;
                yield return FadeCanvasGroup(canvasGroup, 1f, 0f, fadeDuration);
                Destroy(gameObject);
            }
        }
    }
}
