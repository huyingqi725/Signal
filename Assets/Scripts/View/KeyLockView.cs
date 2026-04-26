using System;
using UnityEngine;
using TuringSignal.Core.Data;
using TuringSignal.Gameplay;

namespace TuringSignal.View
{
    /// <summary>
    /// Bind a scene-placed key prop to a logical key at <see cref="gridCell"/> + <see cref="keyColor"/>.
    /// When set, this takes priority over <see cref="KeyLockView.redKeyWorldSprite"/> / blueKeyWorldSprite for that key.
    /// The object is hidden when picked up (not destroyed).
    /// </summary>
    [Serializable]
    public sealed class KeyWorldVisualOverride
    {
        [Tooltip("必须与 GameBootstrap 里该钥匙的格子坐标一致。")]
        public Vector2Int gridCell;

        public KeyColor keyColor;

        [Tooltip("场景里摆好的物体（可含子物体 SpriteRenderer）。拾取后 SetActive(false)。")]
        public Transform worldVisualRoot;
    }

    /// <summary>
    /// Bind a scene-placed prop on the lock cell. Keep it inactive in the scene for an empty lock;
    /// when the key is placed, the root is set active (opposite of key override).
    /// </summary>
    [Serializable]
    public sealed class LockWorldVisualOverride
    {
        [Tooltip("必须与 GameBootstrap 里该锁的格子坐标一致。")]
        public Vector2Int gridCell;

        public KeyColor lockColor;

        [Tooltip("场景里摆在锁格上的物体，默认隐藏；钥匙插入后 SetActive(true)。")]
        public Transform worldVisualRoot;
    }

    /// <summary>
    /// Scene override for a <see cref="BabyLockItemLogic"/>; match grid, color, and opening direction (与 InteractablePlacement.babyLockInteractionFace 相同).
    /// Optional <see cref="idleBabyWorldRoot"/> hides the pre-fill scene baby when the key is placed.
    /// </summary>
    [Serializable]
    public sealed class BabyLockWorldVisualOverride
    {
        [Tooltip("必须与 GameBootstrap 里该婴儿锁的格子坐标一致。")]
        public Vector2Int gridCell;

        public KeyColor lockColor;

        [Tooltip("与关卡里婴儿锁的开口朝向一致（mouth outward），不是机器人朝向。")]
        public Direction interactionFace;

        [Tooltip("未插钥匙时显示的场景婴儿；插钥匙后 SetActive(false)。拖入该婴儿根物体或含 Sprite 的父物体。")]
        public Transform idleBabyWorldRoot;

        [Tooltip("场景里摆在锁格上的物体，默认隐藏；钥匙插入后 SetActive(true)。")]
        public Transform worldVisualRoot;
    }

    /// <summary>
    /// Optional world / robot visuals for key–lock puzzle (sprites can be left null to skip).
    /// </summary>
    public sealed class KeyLockView : MonoBehaviour
    {
        [Header("World — key on ground (手动场景物体，优先)")]
        [SerializeField] private KeyWorldVisualOverride[] keyWorldVisualOverrides = Array.Empty<KeyWorldVisualOverride>();
        [Header("World — key on ground (程序生成，无 Override 匹配时使用)")]
        [SerializeField] private Sprite redKeyWorldSprite;
        [SerializeField] private Sprite blueKeyWorldSprite;
        [Header("World — lock (手动场景物体，优先)")]
        [SerializeField] private LockWorldVisualOverride[] lockWorldVisualOverrides = Array.Empty<LockWorldVisualOverride>();
        [Header("World — baby lock (手动场景物体，优先)")]
        [SerializeField] private BabyLockWorldVisualOverride[] babyLockWorldVisualOverrides = Array.Empty<BabyLockWorldVisualOverride>();
        [Header("World — lock empty / filled (程序生成，无 Override 匹配时使用)")]
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
            public bool IsSceneProvided;
        }

        private sealed class LockVisual
        {
            public LockItemLogic Logic;
            public SpriteRenderer Renderer;
            public Transform Transform;
            public bool IsSceneProvided;
        }

        private sealed class BabyLockVisual
        {
            public BabyLockItemLogic Logic;
            public SpriteRenderer Renderer;
            public Transform Transform;
            public bool IsSceneProvided;
            public Transform IdleBabyWorldRoot;
        }

        private KeyVisual[] keyVisuals = System.Array.Empty<KeyVisual>();
        private LockVisual[] lockVisuals = System.Array.Empty<LockVisual>();
        private BabyLockVisual[] babyLockVisuals = System.Array.Empty<BabyLockVisual>();

        public void Initialize(
            GridView gridView,
            Transform robotRoot,
            RobotLogic robotLogic,
            KeyItemLogic[] keys,
            LockItemLogic[] locks,
            BabyLockItemLogic[] babyLocks)
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
                    Transform manualRoot = FindManualKeyWorldRoot(key);

                    KeyVisual kv = new KeyVisual
                    {
                        Logic = key,
                        IsSceneProvided = manualRoot != null,
                    };

                    if (manualRoot != null)
                    {
                        kv.Transform = manualRoot;
                        kv.Renderer = manualRoot.GetComponentInChildren<SpriteRenderer>(true);
                    }
                    else
                    {
                        Sprite sprite = key.Color == KeyColor.Red ? redKeyWorldSprite : blueKeyWorldSprite;
                        kv.Transform = CreateWorldSprite($"Key_{key.Color}_{key.GridPosition}", sprite, key.GridPosition);
                        kv.Renderer = kv.Transform != null ? kv.Transform.GetComponent<SpriteRenderer>() : null;
                    }

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
                    Transform manualLockRoot = FindManualLockWorldRoot(lo);

                    LockVisual lv = new LockVisual
                    {
                        Logic = lo,
                        IsSceneProvided = manualLockRoot != null,
                    };

                    if (manualLockRoot != null)
                    {
                        lv.Transform = manualLockRoot;
                        lv.Renderer = manualLockRoot.GetComponentInChildren<SpriteRenderer>(true);
                    }
                    else
                    {
                        Sprite sprite = GetLockSprite(lo.Color, lo.HasKeyPlaced);
                        lv.Transform = CreateWorldSprite($"Lock_{lo.Color}_{lo.GridPosition}", sprite, lo.GridPosition);
                        lv.Renderer = lv.Transform != null ? lv.Transform.GetComponent<SpriteRenderer>() : null;
                    }

                    lo.OnKeyPlaced += HandleLockKeyPlaced;
                    lockVisuals[i] = lv;
                    RefreshLockWorld(lo);
                }
            }

            if (babyLocks != null && babyLocks.Length > 0)
            {
                babyLockVisuals = new BabyLockVisual[babyLocks.Length];

                for (int i = 0; i < babyLocks.Length; i++)
                {
                    BabyLockItemLogic baby = babyLocks[i];
                    TryGetBabyLockWorldOverride(baby, out Transform manualRoot, out Transform idleBabyRoot);

                    BabyLockVisual bv = new BabyLockVisual
                    {
                        Logic = baby,
                        IdleBabyWorldRoot = idleBabyRoot,
                        IsSceneProvided = manualRoot != null,
                    };

                    if (manualRoot != null)
                    {
                        bv.Transform = manualRoot;
                        bv.Renderer = manualRoot.GetComponentInChildren<SpriteRenderer>(true);
                    }
                    else
                    {
                        Sprite sprite = GetLockSprite(baby.Color, baby.HasKeyPlaced);
                        bv.Transform = CreateWorldSprite($"BabyLock_{baby.Color}_{baby.InteractionFace}_{baby.GridPosition}", sprite, baby.GridPosition);
                        bv.Renderer = bv.Transform != null ? bv.Transform.GetComponent<SpriteRenderer>() : null;
                    }

                    baby.OnKeyPlaced += HandleBabyLockKeyPlaced;
                    babyLockVisuals[i] = bv;
                    RefreshBabyLockWorld(baby);
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

                if (keyVisuals[i].Transform != null && !keyVisuals[i].IsSceneProvided)
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

                if (lockVisuals[i].Transform != null && !lockVisuals[i].IsSceneProvided)
                {
                    Destroy(lockVisuals[i].Transform.gameObject);
                }
            }

            lockVisuals = System.Array.Empty<LockVisual>();

            for (int i = 0; i < babyLockVisuals.Length; i++)
            {
                if (babyLockVisuals[i].Logic != null)
                {
                    babyLockVisuals[i].Logic.OnKeyPlaced -= HandleBabyLockKeyPlaced;
                }

                if (babyLockVisuals[i].Transform != null && !babyLockVisuals[i].IsSceneProvided)
                {
                    Destroy(babyLockVisuals[i].Transform.gameObject);
                }
            }

            babyLockVisuals = System.Array.Empty<BabyLockVisual>();

            if (heldKeyTransform != null)
            {
                Destroy(heldKeyTransform.gameObject);
                heldKeyTransform = null;
                heldKeyRenderer = null;
            }

            gridView = null;
        }

        private Transform FindManualKeyWorldRoot(KeyItemLogic key)
        {
            if (keyWorldVisualOverrides == null || keyWorldVisualOverrides.Length == 0)
            {
                return null;
            }

            for (int o = 0; o < keyWorldVisualOverrides.Length; o++)
            {
                KeyWorldVisualOverride entry = keyWorldVisualOverrides[o];

                if (entry.worldVisualRoot == null)
                {
                    continue;
                }

                if (entry.gridCell == key.GridPosition && entry.keyColor == key.Color)
                {
                    return entry.worldVisualRoot;
                }
            }

            return null;
        }

        private Transform FindManualLockWorldRoot(LockItemLogic lo)
        {
            if (lockWorldVisualOverrides == null || lockWorldVisualOverrides.Length == 0)
            {
                return null;
            }

            for (int o = 0; o < lockWorldVisualOverrides.Length; o++)
            {
                LockWorldVisualOverride entry = lockWorldVisualOverrides[o];

                if (entry.worldVisualRoot == null)
                {
                    continue;
                }

                if (entry.gridCell == lo.GridPosition && entry.lockColor == lo.Color)
                {
                    return entry.worldVisualRoot;
                }
            }

            return null;
        }

        private void TryGetBabyLockWorldOverride(
            BabyLockItemLogic baby,
            out Transform filledWorldRoot,
            out Transform idleBabyWorldRoot)
        {
            filledWorldRoot = null;
            idleBabyWorldRoot = null;

            if (babyLockWorldVisualOverrides == null || babyLockWorldVisualOverrides.Length == 0)
            {
                return;
            }

            for (int o = 0; o < babyLockWorldVisualOverrides.Length; o++)
            {
                BabyLockWorldVisualOverride entry = babyLockWorldVisualOverrides[o];

                if (entry.gridCell == baby.GridPosition
                    && entry.lockColor == baby.Color
                    && entry.interactionFace == baby.InteractionFace)
                {
                    filledWorldRoot = entry.worldVisualRoot;
                    idleBabyWorldRoot = entry.idleBabyWorldRoot;
                    return;
                }
            }
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

        private void HandleBabyLockKeyPlaced(BabyLockItemLogic baby)
        {
            RefreshBabyLockWorld(baby);
        }

        private void RefreshKeyWorld(KeyItemLogic key)
        {
            for (int i = 0; i < keyVisuals.Length; i++)
            {
                if (keyVisuals[i].Logic != key)
                {
                    continue;
                }

                if (keyVisuals[i].IsSceneProvided && keyVisuals[i].Transform != null)
                {
                    keyVisuals[i].Transform.gameObject.SetActive(!key.IsPickedUp);
                }
                else if (keyVisuals[i].Renderer != null)
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

                if (lockVisuals[i].IsSceneProvided && lockVisuals[i].Transform != null)
                {
                    lockVisuals[i].Transform.gameObject.SetActive(lo.HasKeyPlaced);
                    return;
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

        private void RefreshBabyLockWorld(BabyLockItemLogic baby)
        {
            for (int i = 0; i < babyLockVisuals.Length; i++)
            {
                if (babyLockVisuals[i].Logic != baby)
                {
                    continue;
                }

                if (babyLockVisuals[i].IdleBabyWorldRoot != null)
                {
                    babyLockVisuals[i].IdleBabyWorldRoot.gameObject.SetActive(!baby.HasKeyPlaced);
                }

                if (babyLockVisuals[i].IsSceneProvided && babyLockVisuals[i].Transform != null)
                {
                    bool show = baby.HasKeyPlaced;
                    babyLockVisuals[i].Transform.gameObject.SetActive(show);
                    return;
                }

                SpriteRenderer r = babyLockVisuals[i].Renderer;

                if (r == null)
                {
                    return;
                }

                Sprite sprite = GetLockSprite(baby.Color, baby.HasKeyPlaced);

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
