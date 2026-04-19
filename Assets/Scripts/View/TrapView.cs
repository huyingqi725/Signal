using System.Collections.Generic;
using UnityEngine;

namespace TuringSignal.View
{
    public sealed class TrapView : MonoBehaviour
    {
        private sealed class TrapSpriteEntry
        {
            public SpriteRenderer Renderer;
            public bool IsOddTrap;
        }

        [Header("Sprites")]
        [SerializeField] private Sprite safeTrapSprite;
        [SerializeField] private Sprite dangerTrapSprite;

        [Header("Rendering")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0f, 0f);
        [SerializeField] private int sortingOrder = 2;

        private readonly List<TrapSpriteEntry> trapEntries = new List<TrapSpriteEntry>();

        private GridView gridView;
        private bool oddTrapPhaseActive;

        public void Initialize(GridView gridView, Vector2Int[] oddTrapCells, Vector2Int[] evenTrapCells, bool oddTrapPhaseActive)
        {
            this.gridView = gridView;
            this.oddTrapPhaseActive = oddTrapPhaseActive;

            ClearExistingTrapRenderers();
            RebuildTrapRenderers(oddTrapCells, true);
            RebuildTrapRenderers(evenTrapCells, false);
            RefreshSprites();
        }

        public void SetTrapPhase(bool oddTrapPhaseActive)
        {
            this.oddTrapPhaseActive = oddTrapPhaseActive;
            RefreshSprites();
        }

        private void RebuildTrapRenderers(Vector2Int[] trapCells, bool isOddTrap)
        {
            if (gridView == null || trapCells == null)
            {
                return;
            }

            for (int i = 0; i < trapCells.Length; i++)
            {
                Vector2Int trapCell = trapCells[i];
                GameObject trapObject = new GameObject($"Trap_{trapCell.x}_{trapCell.y}");
                trapObject.transform.SetParent(transform, false);
                trapObject.transform.position = gridView.GridToWorld(trapCell) + worldOffset;

                SpriteRenderer spriteRenderer = trapObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sortingOrder = sortingOrder;
                trapEntries.Add(new TrapSpriteEntry
                {
                    Renderer = spriteRenderer,
                    IsOddTrap = isOddTrap
                });
            }
        }

        private void RefreshSprites()
        {
            for (int i = 0; i < trapEntries.Count; i++)
            {
                TrapSpriteEntry entry = trapEntries[i];

                if (entry == null || entry.Renderer == null)
                {
                    continue;
                }

                bool isDanger = entry.IsOddTrap == oddTrapPhaseActive;
                entry.Renderer.sprite = isDanger ? dangerTrapSprite : safeTrapSprite;
            }
        }

        private void ClearExistingTrapRenderers()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            trapEntries.Clear();
        }
    }
}
