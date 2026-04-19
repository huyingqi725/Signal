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
        [SerializeField] private float arrowVerticalOffset = 0.55f;
        [SerializeField] private float arrowLength = 0.45f;
        [SerializeField] private float arrowHeadSize = 0.16f;
        [SerializeField] private float arrowWidth = 0.08f;
        [SerializeField] private Color moveIntentColor = Color.cyan;
        [SerializeField] private Color interactIntentColor = Color.green;

        private GridView gridView;
        private RobotLogic robotLogic;
        private Coroutine moveCoroutine;
        private LineRenderer intentArrowRenderer;
        private Material intentArrowMaterial;
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
                EnsureIntentArrowRenderer();
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

            EnsureIntentArrowRenderer();

            Vector3 origin = new Vector3(0f, arrowVerticalOffset, 0f);
            Vector2Int intentVector = DirectionUtility.ToVector2Int(intent.Direction);
            Vector3 direction = new Vector3(intentVector.x, intentVector.y, 0f).normalized;

            if (direction == Vector3.zero)
            {
                direction = Vector3.right;
            }

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

            Color arrowColor = intent.Type == IntentType.Interact ? interactIntentColor : moveIntentColor;
            intentArrowRenderer.startColor = arrowColor;
            intentArrowRenderer.endColor = arrowColor;
            intentArrowRenderer.enabled = true;
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
