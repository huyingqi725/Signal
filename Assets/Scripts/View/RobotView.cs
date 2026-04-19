using System.Collections;
using UnityEngine;
using TuringSignal.Core.Data;
using TuringSignal.Gameplay;

namespace TuringSignal.View
{
    public sealed class RobotView : MonoBehaviour
    {
        private static readonly int FacingParameter = Animator.StringToHash("Facing");
        private static readonly int IsInteractingParameter = Animator.StringToHash("IsInteract");

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private float moveDuration = 0.12f;
        [Header("Intent Arrow")]
        [SerializeField] private bool showIntentArrow = true;
        [Tooltip("美术为竖直向上（+Y）的箭头；拖入后使用 Sprite 显示意图，否则使用程序画的 LineRenderer。")]
        [SerializeField] private Sprite intentArrowSprite;
        [SerializeField] private float arrowVerticalOffset = 0.55f;
        [SerializeField] private float arrowSpriteScale = 0.35f;
        [Tooltip("沿意图方向在竖直偏移基础上的额外位移（世界单位，沿意图方向）。")]
        [SerializeField] private float arrowSpriteForwardOffset = 0.2f;
        [SerializeField] private Color moveIntentColor = Color.cyan;
        [SerializeField] private Color interactIntentColor = Color.green;
        [Header("Intent Arrow (程序化线条，无 Sprite 时使用)")]
        [SerializeField] private float arrowLength = 0.45f;
        [SerializeField] private float arrowHeadSize = 0.16f;
        [SerializeField] private float arrowWidth = 0.08f;

        private GridView gridView;
        private RobotLogic robotLogic;
        private Coroutine moveCoroutine;
        private LineRenderer intentArrowRenderer;
        private Material intentArrowMaterial;
        private SpriteRenderer intentArrowSpriteRenderer;
        private Transform intentArrowTransform;
        private bool isGoalLocked;

        public float MoveDuration => moveDuration;

        public void Bind(GridView gridView, RobotLogic robotLogic)
        {
            Unbind();

            this.gridView = gridView;
            this.robotLogic = robotLogic;
            animator = animator != null ? animator : GetComponent<Animator>();

            if (this.robotLogic == null || this.gridView == null)
            {
                return;
            }

            this.robotLogic.OnMoveSucceeded += HandleMoveSucceeded;
            this.robotLogic.OnIntentChanged += HandleIntentChanged;
            transform.position = this.gridView.GridToWorld(this.robotLogic.GridPosition);
            SetFacing(this.robotLogic.FacingDirection);
            SetInteracting(this.robotLogic.PendingIntent.Type == IntentType.Interact);

            if (showIntentArrow)
            {
                if (intentArrowSprite != null)
                {
                    EnsureIntentArrowSprite();
                }
                else
                {
                    EnsureIntentArrowRenderer();
                }

                HandleIntentChanged(this.robotLogic.PendingIntent);
            }
        }

        private void OnDestroy()
        {
            Unbind();

            if (intentArrowMaterial != null)
            {
                Destroy(intentArrowMaterial);
            }

            intentArrowSpriteRenderer = null;
            intentArrowTransform = null;
        }

        private void HandleMoveSucceeded(Vector2Int from, Vector2Int to)
        {
            if (isGoalLocked)
            {
                return;
            }

            Vector3 targetPosition = gridView.GridToWorld(to);
            SetFacing(robotLogic.FacingDirection);

            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
            }

            moveCoroutine = StartCoroutine(PlayMove(targetPosition));
        }

        private void HandleIntentChanged(RobotIntent intent)
        {
            if (isGoalLocked)
            {
                return;
            }

            SetFacing(intent.Direction);
            SetInteracting(intent.Type == IntentType.Interact);

            if (!showIntentArrow)
            {
                return;
            }

            Vector2Int intentVector = DirectionUtility.ToVector2Int(intent.Direction);
            Vector2 direction2 = new Vector2(intentVector.x, intentVector.y);

            if (direction2.sqrMagnitude < 0.0001f)
            {
                direction2 = Vector2.right;
            }
            else
            {
                direction2.Normalize();
            }

            Color arrowColor = intent.Type == IntentType.Interact ? interactIntentColor : moveIntentColor;

            if (intentArrowSprite != null)
            {
                EnsureIntentArrowSprite();

                if (intentArrowRenderer != null)
                {
                    intentArrowRenderer.enabled = false;
                }

                Vector3 baseOffset = new Vector3(0f, arrowVerticalOffset, 0f);
                intentArrowTransform.localPosition = baseOffset + (Vector3)(direction2 * arrowSpriteForwardOffset);
                float angleZ = Vector2.SignedAngle(Vector2.up, direction2);
                intentArrowTransform.localRotation = Quaternion.Euler(0f, 0f, angleZ);
                float s = Mathf.Max(0.01f, arrowSpriteScale);
                intentArrowTransform.localScale = new Vector3(s, s, 1f);
                intentArrowSpriteRenderer.sprite = intentArrowSprite;
                intentArrowSpriteRenderer.color = arrowColor;
                intentArrowSpriteRenderer.enabled = true;
            }
            else
            {
                if (intentArrowSpriteRenderer != null)
                {
                    intentArrowSpriteRenderer.enabled = false;
                }

                EnsureIntentArrowRenderer();

                Vector3 origin = new Vector3(0f, arrowVerticalOffset, 0f);
                Vector3 direction = new Vector3(direction2.x, direction2.y, 0f);

                Vector3 tip = origin + (direction * arrowLength);
                Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f);
                Vector3 leftHead = tip - (direction * arrowHeadSize) + (perpendicular * arrowHeadSize * 0.7f);
                Vector3 rightHead = tip - (direction * arrowHeadSize) - (perpendicular * arrowHeadSize * 0.7f);

                intentArrowRenderer.positionCount = 5;
                intentArrowRenderer.SetPosition(0, origin);
                intentArrowRenderer.SetPosition(1, tip);
                intentArrowRenderer.SetPosition(2, leftHead);
                intentArrowRenderer.SetPosition(3, tip);
                intentArrowRenderer.SetPosition(4, rightHead);

                intentArrowRenderer.startColor = arrowColor;
                intentArrowRenderer.endColor = arrowColor;
                intentArrowRenderer.enabled = true;
            }
        }

        private IEnumerator PlayMove(Vector3 targetPosition)
        {
            Vector3 startPosition = transform.position;
            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            transform.position = targetPosition;
            moveCoroutine = null;
        }

        public void EnterGoalIdleState()
        {
            isGoalLocked = true;

            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }

            if (robotLogic != null && gridView != null)
            {
                transform.position = gridView.GridToWorld(robotLogic.GridPosition);
            }

            if (intentArrowRenderer != null)
            {
                intentArrowRenderer.enabled = false;
            }

            if (intentArrowSpriteRenderer != null)
            {
                intentArrowSpriteRenderer.enabled = false;
            }

            if (animator == null)
            {
                return;
            }

            animator.speed = 1f;
            animator.Play(GetIdleStateName(robotLogic != null ? robotLogic.FacingDirection : Direction.Down), 0, 0f);
            animator.Update(0f);
            animator.speed = 0f;
        }

        private void Unbind()
        {
            if (robotLogic != null)
            {
                robotLogic.OnMoveSucceeded -= HandleMoveSucceeded;
                robotLogic.OnIntentChanged -= HandleIntentChanged;
            }

            robotLogic = null;
            gridView = null;
            isGoalLocked = false;
        }

        private void SetFacing(Direction direction)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetInteger(FacingParameter, (int)direction);
        }

        private void SetInteracting(bool isInteracting)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetBool(IsInteractingParameter, isInteracting);
        }

        private void EnsureIntentArrowSprite()
        {
            if (intentArrowSpriteRenderer != null)
            {
                return;
            }

            GameObject arrowObject = new GameObject("IntentArrowSprite");
            arrowObject.transform.SetParent(transform, false);
            intentArrowTransform = arrowObject.transform;
            intentArrowSpriteRenderer = arrowObject.AddComponent<SpriteRenderer>();
            intentArrowSpriteRenderer.sortingOrder = 10;
        }

        private void EnsureIntentArrowRenderer()
        {
            if (intentArrowRenderer != null)
            {
                return;
            }

            GameObject arrowObject = new GameObject("IntentArrow");
            arrowObject.transform.SetParent(transform, false);

            intentArrowRenderer = arrowObject.AddComponent<LineRenderer>();
            intentArrowRenderer.useWorldSpace = false;
            intentArrowRenderer.loop = false;
            intentArrowRenderer.widthMultiplier = arrowWidth;
            intentArrowRenderer.numCapVertices = 4;
            intentArrowRenderer.numCornerVertices = 2;
            intentArrowRenderer.sortingOrder = 10;

            Shader lineShader = Shader.Find("Sprites/Default");

            if (lineShader == null)
            {
                lineShader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (lineShader != null)
            {
                intentArrowMaterial = new Material(lineShader);
                intentArrowRenderer.material = intentArrowMaterial;
            }
        }

        private static string GetIdleStateName(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return "Up 1";
                case Direction.Right:
                    return "Right 1";
                case Direction.Down:
                    return "Down 1";
                case Direction.Left:
                    return "Left 1";
                default:
                    return "Down 1";
            }
        }
    }
}
