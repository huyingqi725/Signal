using UnityEngine;
using TuringSignal.Core.Data;
using TuringSignal.Gameplay;

namespace TuringSignal.View
{
    /// <summary>
    /// Optional world / robot visuals for key–lock puzzle (sprites can be left null to skip).
    /// </summary>
    public sealed class KeyLockView : MonoBehaviour
    {
        [Header("World — key on ground")]
        [SerializeField] private Sprite redKeyWorldSprite;
        [SerializeField] private Sprite blueKeyWorldSprite;
        [Header("World — lock empty / filled")]
        [SerializeField] private Sprite redLockEmptySprite;
        [SerializeField] private Sprite redLockFilledSprite;
        [SerializeField] private Sprite blueLockEmptySprite;
        [SerializeField] private Sprite blueLockFilledSprite;
        [Header("Robot — held key")]
        [SerializeField] private Transform heldKeyAttachPoint;
        [SerializeField] private Vector3 heldKeyLocalOffset = new Vector3(0f, 0.35f, 0f);
        [SerializeField] private float heldKeyScale = 0.25f;
        [SerializeField] private Sprite redKeyHeldSprite;
        [SerializeField] private Sprite blueKeyHeldSprite;
        [SerializeField] private int sortingOrder = 12;

        private GridView gridView;
        private RobotLogic subscribedRobot;
        private SpriteRenderer heldKeyRenderer;
        private Transform heldKeyTransform;

        private sealed class KeyVisual
        {
            public KeyItemLogic Logic;
            public SpriteRenderer Renderer;
            public Transform Transform;
        }

        private sealed class LockVisual
        {
            public LockItemLogic Logic;
            public SpriteRenderer Renderer;
            public Transform Transform;
        }

        private KeyVisual[] keyVisuals = System.Array.Empty<KeyVisual>();
        private LockVisual[] lockVisuals = System.Array.Empty<LockVisual>();

        public void Initialize(
            GridView gridView,
            Transform robotRoot,
            RobotLogic robotLogic,
            KeyItemLogic[] keys,
            LockItemLogic[] locks)
        {
            Shutdown();

            this.gridView = gridView;
            subscribedRobot = robotLogic;

            Transform attach = heldKeyAttachPoint != null ? heldKeyAttachPoint : robotRoot;
            GameObject heldGo = new GameObject("HeldKey");
            heldGo.transform.SetParent(attach, false);
            heldGo.transform.localPosition = heldKeyLocalOffset;
            heldKeyTransform = heldGo.transform;
            heldKeyTransform.localScale = Vector3.one * Mathf.Max(0.01f, heldKeyScale);
            heldKeyRenderer = heldGo.AddComponent<SpriteRenderer>();
            heldKeyRenderer.sortingOrder = sortingOrder;
            heldKeyRenderer.enabled = false;

            if (subscribedRobot != null)
            {
                subscribedRobot.OnCarriedKeyChanged += HandleCarriedKeyChanged;
                HandleCarriedKeyChanged(subscribedRobot.CarriedKey);
            }

            if (keys != null && keys.Length > 0)
            {
                keyVisuals = new KeyVisual[keys.Length];

                for (int i = 0; i < keys.Length; i++)
                {
                    KeyItemLogic key = keys[i];
                    Sprite sprite = key.Color == KeyColor.Red ? redKeyWorldSprite : blueKeyWorldSprite;

                    KeyVisual kv = new KeyVisual
                    {
                        Logic = key,
                        Transform = CreateWorldSprite($"Key_{key.Color}_{key.GridPosition}", sprite, key.GridPosition),
                    };

                    kv.Renderer = kv.Transform != null ? kv.Transform.GetComponent<SpriteRenderer>() : null;
                    key.OnPickedUp += HandleKeyPickedUp;
                    keyVisuals[i] = kv;
                    RefreshKeyWorld(key);
                }
            }

            if (locks != null && locks.Length > 0)
            {
                lockVisuals = new LockVisual[locks.Length];

                for (int i = 0; i < locks.Length; i++)
                {
                    LockItemLogic lo = locks[i];
                    Sprite sprite = GetLockSprite(lo.Color, lo.HasKeyPlaced);

                    LockVisual lv = new LockVisual
                    {
                        Logic = lo,
                        Transform = CreateWorldSprite($"Lock_{lo.Color}_{lo.GridPosition}", sprite, lo.GridPosition),
                    };

                    lv.Renderer = lv.Transform != null ? lv.Transform.GetComponent<SpriteRenderer>() : null;
                    lo.OnKeyPlaced += HandleLockKeyPlaced;
                    lockVisuals[i] = lv;
                    RefreshLockWorld(lo);
                }
            }
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void Shutdown()
        {
            if (subscribedRobot != null)
            {
                subscribedRobot.OnCarriedKeyChanged -= HandleCarriedKeyChanged;
                subscribedRobot = null;
            }

            for (int i = 0; i < keyVisuals.Length; i++)
            {
                if (keyVisuals[i].Logic != null)
                {
                    keyVisuals[i].Logic.OnPickedUp -= HandleKeyPickedUp;
                }

                if (keyVisuals[i].Transform != null)
                {
                    Destroy(keyVisuals[i].Transform.gameObject);
                }
            }

            keyVisuals = System.Array.Empty<KeyVisual>();

            for (int i = 0; i < lockVisuals.Length; i++)
            {
                if (lockVisuals[i].Logic != null)
                {
                    lockVisuals[i].Logic.OnKeyPlaced -= HandleLockKeyPlaced;
                }

                if (lockVisuals[i].Transform != null)
                {
                    Destroy(lockVisuals[i].Transform.gameObject);
                }
            }

            lockVisuals = System.Array.Empty<LockVisual>();

            if (heldKeyTransform != null)
            {
                Destroy(heldKeyTransform.gameObject);
                heldKeyTransform = null;
                heldKeyRenderer = null;
            }

            gridView = null;
        }

        private Transform CreateWorldSprite(string name, Sprite sprite, Vector2Int gridPosition)
        {
            if (sprite == null || gridView == null)
            {
                return null;
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(gridView.transform, false);
            go.transform.position = gridView.GridToWorld(gridPosition);
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = sortingOrder;
            return go.transform;
        }

        private Sprite GetLockSprite(KeyColor color, bool filled)
        {
            if (color == KeyColor.Red)
            {
                return filled ? redLockFilledSprite : redLockEmptySprite;
            }

            return filled ? blueLockFilledSprite : blueLockEmptySprite;
        }

        private void HandleKeyPickedUp(KeyItemLogic key)
        {
            RefreshKeyWorld(key);
        }

        private void HandleLockKeyPlaced(LockItemLogic lo)
        {
            RefreshLockWorld(lo);
        }

        private void RefreshKeyWorld(KeyItemLogic key)
        {
            for (int i = 0; i < keyVisuals.Length; i++)
            {
                if (keyVisuals[i].Logic != key)
                {
                    continue;
                }

                if (keyVisuals[i].Renderer != null)
                {
                    keyVisuals[i].Renderer.enabled = !key.IsPickedUp;
                }

                return;
            }
        }

        private void RefreshLockWorld(LockItemLogic lo)
        {
            for (int i = 0; i < lockVisuals.Length; i++)
            {
                if (lockVisuals[i].Logic != lo)
                {
                    continue;
                }

                SpriteRenderer r = lockVisuals[i].Renderer;

                if (r == null)
                {
                    return;
                }

                Sprite sprite = GetLockSprite(lo.Color, lo.HasKeyPlaced);

                if (sprite != null)
                {
                    r.sprite = sprite;
                }

                r.enabled = sprite != null;
                return;
            }
        }

        private void HandleCarriedKeyChanged(KeyColor? key)
        {
            if (heldKeyRenderer == null)
            {
                return;
            }

            if (!key.HasValue)
            {
                heldKeyRenderer.enabled = false;
                return;
            }

            Sprite s = key.Value == KeyColor.Red ? redKeyHeldSprite : blueKeyHeldSprite;

            if (s == null)
            {
                heldKeyRenderer.enabled = false;
                return;
            }

            heldKeyRenderer.sprite = s;
            heldKeyRenderer.enabled = true;
        }
    }
}
