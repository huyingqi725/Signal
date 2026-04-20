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
        [Tooltip("仅 BabyLock：锁「开口」朝向（mouth 指向 Up/Right/Down/Left）。例如开口朝下(Down)时，机器人须在锁格下方邻格面朝上。")]
        public Direction babyLockInteractionFace = Direction.Up;
        public string interactableId = "Item";
        public Vector2Int gridPosition = Vector2Int.zero;
    }
}
