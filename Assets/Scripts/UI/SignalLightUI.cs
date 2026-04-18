using System.Collections;
using UnityEngine;
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

        [Header("Sprites")]
        [SerializeField] private Sprite redLightIdleSprite;
        [SerializeField] private Sprite redLightPressedSprite;
        [SerializeField] private Sprite greenLightIdleSprite;
        [SerializeField] private Sprite greenLightPressedSprite;

        [Header("Timing")]
        [SerializeField] private float pressVisualDuration = 0.12f;

        private Coroutine redLightCoroutine;
        private Coroutine greenLightCoroutine;

        private void Awake()
        {
            ApplyIdleSprites();
        }

        private void OnEnable()
        {
            if (inputRouter == null)
            {
                return;
            }

            inputRouter.OnRotatePressed += HandleRedLightPressed;
            inputRouter.OnInteractPressed += HandleGreenLightPressed;
        }

        private void OnDisable()
        {
            if (inputRouter == null)
            {
                return;
            }

            inputRouter.OnRotatePressed -= HandleRedLightPressed;
            inputRouter.OnInteractPressed -= HandleGreenLightPressed;
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
    }
}
