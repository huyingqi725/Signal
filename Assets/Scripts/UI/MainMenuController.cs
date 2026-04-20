using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TuringSignal.UI
{
    /// <summary>
    /// Lives on the DDOL fade canvas so fade-in continues after the menu scene unloads.
    /// </summary>
    internal sealed class MainMenuSceneFadeRunner : MonoBehaviour
    {
        public void BeginActivateSceneAndFadeInFromBlack(AsyncOperation loadOp, CanvasGroup blackout, float fadeInDuration)
        {
            StartCoroutine(Run(loadOp, blackout, fadeInDuration));
        }

        private IEnumerator Run(AsyncOperation loadOp, CanvasGroup blackout, float fadeInDuration)
        {
            loadOp.allowSceneActivation = true;

            while (!loadOp.isDone)
            {
                yield return null;
            }

            float inDur = Mathf.Max(0.01f, fadeInDuration);
            float t = 0f;

            while (t < inDur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / inDur);

                if (blackout != null)
                {
                    blackout.alpha = 1f - u;
                }

                yield return null;
            }

            if (blackout != null)
            {
                blackout.alpha = 0f;
            }

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Main menu: each story sentence is its own <see cref="TextMeshProUGUI"/> (same bubble position, stacked in hierarchy).
    /// Typewriter runs on the active line; mouse click advances to the next TMP. After the last line, &quot;Now Start&quot; appears as before.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Menu")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button exitButton;

        [Tooltip("Optional: hidden while the story plays (e.g. panel that holds both buttons).")]
        [SerializeField] private GameObject menuRoot;

        [Header("Story (one TMP per sentence, same slot in bubble)")]
        [SerializeField] private GameObject storyRoot;

        [Tooltip("按顺序：每句单独一个 TextMeshPro，文案写在各 TMP 上；一次只显示一个，播完点左键切下一句。若为空可在下方勾选自动从 Story Root 下收集。")]
        [SerializeField] private TextMeshProUGUI[] storyLineTexts = new TextMeshProUGUI[3];

        [Tooltip("未手动指定 storyLineTexts 时，在 Story Root 下按层级顺序收集 TextMeshPro（会排除 Now Start 子树与 nowStartLabel）。")]
        [SerializeField] private bool autoCollectStoryTmpsUnderStoryRoot = true;

        [Tooltip("Delay after each character is revealed (real-time seconds).")]
        [SerializeField] private float secondsPerCharacter = 0.04f;

        [SerializeField] private string firstLevelSceneName = "Level01";

        [Header("Story — skip")]
        [Tooltip("打字过程中：任意键或鼠标点击可立刻打完当前这一句。")]
        [SerializeField] private bool allowSkipToEndOfCurrentLine = true;

        [Header("Now Start")]
        [Tooltip("Shown after the story; should be a child of story UI (or same Canvas).")]
        [SerializeField] private GameObject nowStartRoot;

        [SerializeField] private Button nowStartButton;

        [Tooltip("TMP used for the blinking line; text is set to \"Now Start\" at runtime.")]
        [SerializeField] private TextMeshProUGUI nowStartLabel;

        [SerializeField] private float nowStartBlinkInterval = 0.45f;

        [Header("Transition to level")]
        [SerializeField] private float crossFadeOutDuration = 0.55f;

        [SerializeField] private float fadeInFromBlackDuration = 0.55f;

        private Coroutine _storyCoroutine;
        private Coroutine _blinkCoroutine;
        private Coroutine _transitionCoroutine;
        private bool _storyTypingInProgress;
        private bool _transitionInProgress;

        private void Awake()
        {
            if (storyRoot != null)
            {
                TryAutoPopulateStoryLineTextsIfEmpty();
            }

            if (storyRoot != null)
            {
                storyRoot.SetActive(false);
            }

            if (nowStartRoot != null)
            {
                nowStartRoot.SetActive(false);
            }

            if (nowStartLabel != null)
            {
                nowStartLabel.text = "Now Start";
                nowStartLabel.raycastTarget = true;
            }

            if (nowStartButton != null)
            {
                nowStartButton.transition = Selectable.Transition.None;
                nowStartButton.onClick.RemoveListener(OnNowStartClicked);
                nowStartButton.onClick.AddListener(OnNowStartClicked);
                nowStartButton.interactable = false;
            }

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
                startButton.onClick.AddListener(OnStartClicked);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(OnExitClicked);
                exitButton.onClick.AddListener(OnExitClicked);
            }

            HideAllStoryLineTextObjects();
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(OnExitClicked);
            }

            if (nowStartButton != null)
            {
                nowStartButton.onClick.RemoveListener(OnNowStartClicked);
            }
        }

        public void OnStartClicked()
        {
            if (_storyTypingInProgress || _transitionInProgress)
            {
                return;
            }

            if (storyRoot != null)
            {
                TryAutoPopulateStoryLineTextsIfEmpty();
            }

            if (storyRoot == null || GetValidStoryLineTexts().Count == 0)
            {
                Debug.LogWarning(
                    "MainMenuController: 无法开始剧情 — storyRoot 未指定，或 storyLineTexts 为空且未能在 Story Root 下收集到 TMP。" +
                    "请在 Inspector 为 Story Line Texts 逐个拖入句子 TextMeshPro，或把句子 TMP 放在 Story Root 下并勾选 Auto Collect Story Tmps Under Story Root。");
                BeginTransitionToFirstLevel();
                return;
            }

            _storyTypingInProgress = true;
            SetMenuInteractable(false);

            if (menuRoot != null)
            {
                menuRoot.SetActive(false);
            }

            storyRoot.SetActive(true);

            if (_storyCoroutine != null)
            {
                StopCoroutine(_storyCoroutine);
            }

            _storyCoroutine = StartCoroutine(PlayStoryThenShowNowStart());
        }

        public void OnExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnNowStartClicked()
        {
            if (_transitionInProgress || !_storyTypingInProgress)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(firstLevelSceneName))
            {
                Debug.LogError("MainMenuController: firstLevelSceneName is empty.");
                return;
            }

            if (_blinkCoroutine != null)
            {
                StopCoroutine(_blinkCoroutine);
                _blinkCoroutine = null;
            }

            if (nowStartLabel != null)
            {
                Color c = nowStartLabel.color;
                c.a = 1f;
                nowStartLabel.color = c;
            }

            if (nowStartButton != null)
            {
                nowStartButton.interactable = false;
            }

            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }

            _transitionCoroutine = StartCoroutine(CrossFadeOutLoadLevelFadeIn());
        }

        private void SetMenuInteractable(bool value)
        {
            if (startButton != null)
            {
                startButton.interactable = value;
            }

            if (exitButton != null)
            {
                exitButton.interactable = value;
            }
        }

        private IEnumerator PlayStoryThenShowNowStart()
        {
            List<TextMeshProUGUI> lines = GetValidStoryLineTexts();

            if (lines.Count == 0)
            {
                Debug.LogWarning("MainMenuController: no story line TMPs; showing Now Start or loading level.");
                _storyCoroutine = null;
                TryShowNowStartOrFallback();
                yield break;
            }

            HideAllStoryLineTextObjects();

            for (int i = 0; i < lines.Count; i++)
            {
                TextMeshProUGUI lineTmp = lines[i];
                lineTmp.gameObject.SetActive(true);

                yield return PlayTypewriterOnTmp(lineTmp);

                if (i < lines.Count - 1)
                {
                    yield return WaitForLeftMouseClickToContinue();
                    lineTmp.gameObject.SetActive(false);
                }
            }

            _storyCoroutine = null;
            TryShowNowStartOrFallback();
        }

        private void TryAutoPopulateStoryLineTextsIfEmpty()
        {
            if (!autoCollectStoryTmpsUnderStoryRoot || storyRoot == null)
            {
                return;
            }

            if (GetValidStoryLineTexts().Count > 0)
            {
                return;
            }

            TextMeshProUGUI[] all = storyRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            List<TextMeshProUGUI> pick = new List<TextMeshProUGUI>();

            foreach (TextMeshProUGUI t in all)
            {
                if (t == null)
                {
                    continue;
                }

                if (nowStartLabel != null && ReferenceEquals(t, nowStartLabel))
                {
                    continue;
                }

                if (nowStartRoot != null)
                {
                    Transform nt = nowStartRoot.transform;
                    Transform cur = t.transform;

                    if (ReferenceEquals(cur, nt) || cur.IsChildOf(nt))
                    {
                        continue;
                    }
                }

                pick.Add(t);
            }

            if (pick.Count == 0)
            {
                return;
            }

            storyLineTexts = pick.ToArray();
        }

        private List<TextMeshProUGUI> GetValidStoryLineTexts()
        {
            List<TextMeshProUGUI> list = new List<TextMeshProUGUI>();

            if (storyLineTexts == null)
            {
                return list;
            }

            for (int i = 0; i < storyLineTexts.Length; i++)
            {
                if (storyLineTexts[i] != null)
                {
                    list.Add(storyLineTexts[i]);
                }
            }

            return list;
        }

        private void HideAllStoryLineTextObjects()
        {
            foreach (TextMeshProUGUI t in GetValidStoryLineTexts())
            {
                if (t != null)
                {
                    t.gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator PlayTypewriterOnTmp(TextMeshProUGUI tmp)
        {
            string fullText = tmp.text;
            tmp.text = fullText;
            tmp.maxVisibleCharacters = 0;
            tmp.ForceMeshUpdate();

            int charCount = tmp.textInfo.characterCount;
            float delay = Mathf.Max(0f, secondsPerCharacter);

            if (charCount <= 0 || delay <= 0f)
            {
                tmp.maxVisibleCharacters = int.MaxValue;
                yield break;
            }

            yield return null;
            yield return null;

            int frameIndex = 0;

            for (int visible = 1; visible <= charCount; visible++)
            {
                frameIndex++;

                if (allowSkipToEndOfCurrentLine
                    && frameIndex > 2
                    && SkipInputThisFrame())
                {
                    tmp.maxVisibleCharacters = charCount;
                    break;
                }

                tmp.maxVisibleCharacters = visible;
                yield return new WaitForSecondsRealtime(delay);
            }

            tmp.maxVisibleCharacters = int.MaxValue;
        }

        private IEnumerator WaitForLeftMouseClickToContinue()
        {
            yield return null;

            while (!UnityEngine.Input.GetMouseButtonDown(0))
            {
                yield return null;
            }

            while (UnityEngine.Input.GetMouseButton(0))
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }

        private void TryShowNowStartOrFallback()
        {
            if (nowStartRoot != null && nowStartButton != null && nowStartLabel != null)
            {
                nowStartRoot.SetActive(true);
                nowStartButton.interactable = true;

                if (_blinkCoroutine != null)
                {
                    StopCoroutine(_blinkCoroutine);
                }

                _blinkCoroutine = StartCoroutine(BlinkNowStartLabel());
            }
            else
            {
                Debug.LogWarning(
                    "MainMenuController: nowStartRoot / nowStartButton / nowStartLabel not fully assigned; loading level.");
                BeginTransitionToFirstLevel();
            }
        }

        private IEnumerator BlinkNowStartLabel()
        {
            float interval = Mathf.Max(0.05f, nowStartBlinkInterval);

            while (nowStartLabel != null && nowStartRoot != null && nowStartRoot.activeInHierarchy)
            {
                Color visible = nowStartLabel.color;
                visible.a = 1f;
                nowStartLabel.color = visible;
                yield return new WaitForSecondsRealtime(interval);

                Color dim = nowStartLabel.color;
                dim.a = 0f;
                nowStartLabel.color = dim;
                yield return new WaitForSecondsRealtime(interval);
            }
        }

        private IEnumerator CrossFadeOutLoadLevelFadeIn()
        {
            _transitionInProgress = true;

            CanvasGroup storyCg = storyRoot != null
                ? storyRoot.GetComponent<CanvasGroup>() ?? storyRoot.AddComponent<CanvasGroup>()
                : null;

            GameObject ddol = CreateDdolBlackoutCanvas(out CanvasGroup blackout);
            float outDur = Mathf.Max(0.01f, crossFadeOutDuration);
            float t = 0f;

            while (t < outDur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / outDur);

                if (storyCg != null)
                {
                    storyCg.alpha = 1f - u;
                }

                blackout.alpha = u;
                yield return null;
            }

            if (storyCg != null)
            {
                storyCg.alpha = 0f;
            }

            blackout.alpha = 1f;

            AsyncOperation load = SceneManager.LoadSceneAsync(firstLevelSceneName);
            load.allowSceneActivation = false;

            while (load.progress < 0.9f)
            {
                yield return null;
            }

            MainMenuSceneFadeRunner runner = ddol.AddComponent<MainMenuSceneFadeRunner>();
            runner.BeginActivateSceneAndFadeInFromBlack(load, blackout, fadeInFromBlackDuration);

            _transitionInProgress = false;
            _storyTypingInProgress = false;
            _transitionCoroutine = null;
        }

        private static GameObject CreateDdolBlackoutCanvas(out CanvasGroup blackoutGroup)
        {
            GameObject root = new GameObject("MainMenuSceneFade");
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

        private void BeginTransitionToFirstLevel()
        {
            if (string.IsNullOrWhiteSpace(firstLevelSceneName))
            {
                Debug.LogError("MainMenuController: firstLevelSceneName is empty.");
                SetMenuInteractable(true);

                if (menuRoot != null)
                {
                    menuRoot.SetActive(true);
                }

                if (storyRoot != null)
                {
                    storyRoot.SetActive(false);
                }

                _storyTypingInProgress = false;
                return;
            }

            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }

            _storyTypingInProgress = true;
            _transitionCoroutine = StartCoroutine(CrossFadeOutLoadLevelFadeIn());
        }

        private static bool SkipInputThisFrame()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0)
                || UnityEngine.Input.GetMouseButtonDown(1)
                || UnityEngine.Input.GetMouseButtonDown(2))
            {
                return true;
            }

            return UnityEngine.Input.anyKeyDown;
        }
    }
}
