using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TuringSignal.Input;

namespace TuringSignal.UI
{
    public sealed class SignalLightUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RobotInputRouter inputRouter;
        [SerializeField] private Image redLightImage;
        [SerializeField] private Image greenLightImage;

        [Header("Click (V1.1)")]
        [Tooltip("可选：红灯点击区域；不填且勾选 Auto Create 时会在红灯 Image 下自动生成透明 Button。若在此拖了按钮，请删掉该 Button 上 Inspector 里重复的 OnClick（否则会转两次）。")]
        [SerializeField] private Button redLightClickButton;

        [Tooltip("可选：绿灯点击区域；同上，勿与 Persistent OnClick 重复绑定 TryInteractFromUi。")]
        [SerializeField] private Button greenLightClickButton;

        [Tooltip("无手动 Button 时在灯图下生成透明点击层。若你已在灯 Image 子级放了 Button，会自动跳过生成，以免透明层挡掉你的 OnClick。")]
        [SerializeField] private bool autoCreateInvisibleClickOverlays = true;

        [Header("Main menu")]
        [Tooltip("UICanvas 上的返回主菜单按钮。")]
        [SerializeField] private Button backMainButton;

        [Tooltip("须与 Build Settings 中主菜单场景名一致。")]
        [SerializeField] private string mainMenuSceneName = "MainScene";

        [Header("Sprites")]
        [SerializeField] private Sprite redLightIdleSprite;
        [SerializeField] private Sprite redLightPressedSprite;
        [SerializeField] private Sprite greenLightIdleSprite;
        [SerializeField] private Sprite greenLightPressedSprite;

        [Header("Timing")]
        [SerializeField] private float pressVisualDuration = 0.12f;

        private Coroutine redLightCoroutine;
        private Coroutine greenLightCoroutine;

        private Button _runtimeRedClickButton;
        private Button _runtimeGreenClickButton;

        private void Awake()
        {
            ApplyIdleSprites();
            EnsureClickTargets();
            WireClickTargets();

            if (backMainButton != null)
            {
                backMainButton.onClick.RemoveListener(OnBackMainClicked);
                backMainButton.onClick.AddListener(OnBackMainClicked);
            }
        }

        private void OnDestroy()
        {
            UnwireClickTargets();

            if (backMainButton != null)
            {
                backMainButton.onClick.RemoveListener(OnBackMainClicked);
            }

            if (_runtimeRedClickButton != null)
            {
                Destroy(_runtimeRedClickButton.gameObject);
                _runtimeRedClickButton = null;
            }

            if (_runtimeGreenClickButton != null)
            {
                Destroy(_runtimeGreenClickButton.gameObject);
                _runtimeGreenClickButton = null;
            }
        }

        private void OnEnable()
        {
            if (inputRouter != null)
            {
                inputRouter.OnRotatePressed += HandleRedLightPressed;
                inputRouter.OnInteractPressed += HandleGreenLightPressed;
            }

            WireClickTargets();
        }

        private void OnDisable()
        {
            if (inputRouter != null)
            {
                inputRouter.OnRotatePressed -= HandleRedLightPressed;
                inputRouter.OnInteractPressed -= HandleGreenLightPressed;
            }

            UnwireClickTargets();
        }

        private void OnValidate()
        {
            pressVisualDuration = Mathf.Max(0.01f, pressVisualDuration);
        }

        private void HandleRedLightPressed()
        {
            if (redLightImage == null)
            {
                return;
            }

            RestartLightAnimation(
                ref redLightCoroutine,
                redLightImage,
                redLightPressedSprite,
                redLightIdleSprite);
        }

        private void HandleGreenLightPressed()
        {
            if (greenLightImage == null)
            {
                return;
            }

            RestartLightAnimation(
                ref greenLightCoroutine,
                greenLightImage,
                greenLightPressedSprite,
                greenLightIdleSprite);
        }

        private void RestartLightAnimation(ref Coroutine routine, Image targetImage, Sprite pressedSprite, Sprite idleSprite)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(PlayPressVisual(targetImage, pressedSprite, idleSprite));
        }

        private IEnumerator PlayPressVisual(Image targetImage, Sprite pressedSprite, Sprite idleSprite)
        {
            targetImage.sprite = pressedSprite != null ? pressedSprite : idleSprite;

            if (pressVisualDuration > 0f)
            {
                yield return new WaitForSeconds(pressVisualDuration);
            }

            targetImage.sprite = idleSprite;

            if (targetImage == redLightImage)
            {
                redLightCoroutine = null;
            }
            else if (targetImage == greenLightImage)
            {
                greenLightCoroutine = null;
            }
        }

        private void ApplyIdleSprites()
        {
            if (redLightImage != null)
            {
                redLightImage.sprite = redLightIdleSprite;
            }

            if (greenLightImage != null)
            {
                greenLightImage.sprite = greenLightIdleSprite;
            }
        }

        private void EnsureClickTargets()
        {
            if (redLightClickButton == null
                && autoCreateInvisibleClickOverlays
                && redLightImage != null
                && _runtimeRedClickButton == null
                && !HasAnyButtonUnder(redLightImage.rectTransform))
            {
                _runtimeRedClickButton = CreateInvisibleOverlayButton(redLightImage.rectTransform, "RedLightClick");
            }

            if (greenLightClickButton == null
                && autoCreateInvisibleClickOverlays
                && greenLightImage != null
                && _runtimeGreenClickButton == null
                && !HasAnyButtonUnder(greenLightImage.rectTransform))
            {
                _runtimeGreenClickButton = CreateInvisibleOverlayButton(greenLightImage.rectTransform, "GreenLightClick");
            }
        }

        private static bool HasAnyButtonUnder(RectTransform root)
        {
            if (root == null)
            {
                return false;
            }

            return root.GetComponentsInChildren<Button>(true).Length > 0;
        }

        private static Button CreateInvisibleOverlayButton(RectTransform parent, string objectName)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.None;

            return button;
        }

        private void WireClickTargets()
        {
            if (inputRouter == null)
            {
                return;
            }

            Button red = redLightClickButton != null ? redLightClickButton : _runtimeRedClickButton;
            Button green = greenLightClickButton != null ? greenLightClickButton : _runtimeGreenClickButton;

            if (red != null)
            {
                red.onClick.RemoveListener(OnRedLightClicked);
                red.onClick.AddListener(OnRedLightClicked);
            }

            if (green != null)
            {
                green.onClick.RemoveListener(OnGreenLightClicked);
                green.onClick.AddListener(OnGreenLightClicked);
            }
        }

        private void UnwireClickTargets()
        {
            Button red = redLightClickButton != null ? redLightClickButton : _runtimeRedClickButton;
            Button green = greenLightClickButton != null ? greenLightClickButton : _runtimeGreenClickButton;

            if (red != null)
            {
                red.onClick.RemoveListener(OnRedLightClicked);
            }

            if (green != null)
            {
                green.onClick.RemoveListener(OnGreenLightClicked);
            }
        }

        private void OnRedLightClicked()
        {
            inputRouter?.TryRotateFromUi();
        }

        private void OnGreenLightClicked()
        {
            inputRouter?.TryInteractFromUi();
        }

        private void OnBackMainClicked()
        {
            if (string.IsNullOrWhiteSpace(mainMenuSceneName))
            {
                Debug.LogWarning("SignalLightUI: mainMenuSceneName is empty.");
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
