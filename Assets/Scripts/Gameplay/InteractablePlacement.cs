using System;
using TuringSignal.Core.Data;
using UnityEngine;

namespace TuringSignal.Gameplay
{
    [Serializable]
    public sealed class InteractablePlacement
    {
        public InteractableRole role = InteractableRole.GenericItem;
        public KeyColor keyColor = KeyColor.Red;
        public string interactableId = "Item";
        public Vector2Int gridPosition = Vector2Int.zero;
    }
}
