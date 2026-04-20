using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TuringSignal.UI
{
    /// <summary>
    /// Win 等场景：绑定「返回主菜单」按钮，渐隐当前界面 → 全黑 → 加载 MainScene → 渐显。
    /// 需将 <see cref="mainSceneName"/> 与 Build Settings 中主菜单场景名一致。
    /// </summary>
    public sealed class ReturnToMainMenuButton : MonoBehaviour
    {
        [SerializeField] private Button backMainBtn;

        [Tooltip("返回按钮上的 TMP；不填则从 Back Main Btn 子物体上查找。")]
        [SerializeField] private TextMeshProUGUI backButtonLabel;

        [Tooltip("文字显、隐各持续该时长（秒），完整周期为 2× 该值。默认 0.35 → 亮 0.35s / 暗 0.35s。")]
        [SerializeField] private float backButtonBlinkInterval = 0.35f;

        [Tooltip("可选：胜利界面根节点上的 CanvasGroup；渐隐时 alpha 1→0。不填则仅靠黑屏盖住当前画面。")]
        [SerializeField] private CanvasGroup fadeOutContent;

        [SerializeField] private string mainSceneName = "MainScene";

        [SerializeField] private float crossFadeOutDuration = 0.55f;

        [SerializeField] private float fadeInFromBlackDuration = 0.55f;

        private bool _transitionInProgress;
        private Coroutine _transitionCoroutine;
        private Coroutine _blinkCoroutine;

        private void Awake()
        {
            if (backButtonLabel == null && backMainBtn != null)
            {
                backButtonLabel = backMainBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (backButtonLabel != null)
            {
                backButtonLabel.raycastTarget = true;
            }

            if (backMainBtn != null)
            {
                backMainBtn.onClick.RemoveListener(OnBackClicked);
                backMainBtn.onClick.AddListener(OnBackClicked);
            }
        }

        private void Start()
        {
            if (backButtonLabel != null && _blinkCoroutine == null)
            {
                _blinkCoroutine = StartCoroutine(BlinkBackButtonLabel());
            }
        }

        private void OnDestroy()
        {
            StopBlinkAndShowSolid();

            if (backMainBtn != null)
            {
                backMainBtn.onClick.RemoveListener(OnBackClicked);
            }
        }

        private IEnumerator BlinkBackButtonLabel()
        {
            float interval = Mathf.Max(0.05f, backButtonBlinkInterval);

            while (backButtonLabel != null)
            {
                Color visible = backButtonLabel.color;
                visible.a = 1f;
                backButtonLabel.color = visible;
                yield return new WaitForSecondsRealtime(interval);

                Color dim = backButtonLabel.color;
                dim.a = 0f;
                backButtonLabel.color = dim;
                yield return new WaitForSecondsRealtime(interval);
            }
        }

        private void StopBlinkAndShowSolid()
        {
            if (_blinkCoroutine != null)
            {
                StopCoroutine(_blinkCoroutine);
                _blinkCoroutine = null;
            }

            if (backButtonLabel != null)
            {
                Color c = backButtonLabel.color;
                c.a = 1f;
                backButtonLabel.color = c;
            }
        }

        private void OnBackClicked()
        {
            if (_transitionInProgress)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(mainSceneName))
            {
                Debug.LogError("ReturnToMainMenuButton: mainSceneName is empty.");
                return;
            }

            StopBlinkAndShowSolid();

            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }

            _transitionCoroutine = StartCoroutine(TransitionToMainScene());
        }

        private IEnumerator TransitionToMainScene()
        {
            _transitionInProgress = true;

            if (backMainBtn != null)
            {
                backMainBtn.interactable = false;
            }

            CanvasGroup contentCg = fadeOutContent;

            GameObject ddol = CreateDdolBlackoutCanvas(out CanvasGroup blackout);
            float outDur = Mathf.Max(0.01f, crossFadeOutDuration);
            float t = 0f;

            while (t < outDur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / outDur);

                if (contentCg != null)
                {
                    contentCg.alpha = 1f - u;
                }

                blackout.alpha = u;
                yield return null;
            }

            if (contentCg != null)
            {
                contentCg.alpha = 0f;
            }

            blackout.alpha = 1f;

            AsyncOperation load = SceneManager.LoadSceneAsync(mainSceneName);
            load.allowSceneActivation = false;

            while (load.progress < 0.9f)
            {
                yield return null;
            }

            MainMenuSceneFadeRunner runner = ddol.AddComponent<MainMenuSceneFadeRunner>();
            runner.BeginActivateSceneAndFadeInFromBlack(load, blackout, fadeInFromBlackDuration);

            _transitionInProgress = false;
            _transitionCoroutine = null;
        }

        private static GameObject CreateDdolBlackoutCanvas(out CanvasGroup blackoutGroup)
        {
            GameObject root = new GameObject("ReturnToMainMenuSceneFade");
            Object.DontDestroyOnLoad(root);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            GameObject imageGo = new GameObject("Black");
            RectTransform rt = imageGo.AddComponent<RectTransform>();
            rt.SetParent(root.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image image = imageGo.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;

            blackoutGroup = root.AddComponent<CanvasGroup>();
            blackoutGroup.alpha = 0f;
            blackoutGroup.blocksRaycasts = true;
            blackoutGroup.interactable = true;

            return root;
        }
    }
}
