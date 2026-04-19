using System.Collections.Generic;
using UnityEngine;

namespace TuringSignal.View
{
    public sealed class TrapView : MonoBehaviour
    {
        [Header("Sprites")]
        [SerializeField] private Sprite safeTrapSprite;
        [SerializeField] private Sprite dangerTrapSprite;

        [Header("Rendering")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0f, 0f);
        [SerializeField] private int sortingOrder = 2;

        private readonly List<SpriteRenderer> trapRenderers = new List<SpriteRenderer>();

        private GridView gridView;
        private bool areTrapsActive;

        public void Initialize(GridView gridView, Vector2Int[] trapCells, bool startActive)
        {
            this.gridView = gridView;
            areTrapsActive = startActive;

            RebuildTrapRenderers(trapCells);
            RefreshSprites();
        }

        public void SetTrapState(bool isActive)
        {
            areTrapsActive = isActive;
            RefreshSprites();
        }

        private void RebuildTrapRenderers(Vector2Int[] trapCells)
        {
            ClearExistingTrapRenderers();

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
                trapRenderers.Add(spriteRenderer);
            }
        }

        private void RefreshSprites()
        {
            Sprite targetSprite = areTrapsActive ? dangerTrapSprite : safeTrapSprite;

            for (int i = 0; i < trapRenderers.Count; i++)
            {
                if (trapRenderers[i] == null)
                {
                    continue;
                }

                trapRenderers[i].sprite = targetSprite;
            }
        }

        private void ClearExistingTrapRenderers()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            trapRenderers.Clear();
        }
    }
}
