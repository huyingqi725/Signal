using System;
using UnityEngine;

namespace TuringSignal.Gameplay
{
    [Serializable]
    public sealed class InteractablePlacement
    {
        public string interactableId = "Item";
        public Vector2Int gridPosition = Vector2Int.zero;
    }
}
