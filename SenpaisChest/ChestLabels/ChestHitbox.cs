using UnityEngine;

namespace SenpaisChest.ChestLabels
{
    internal class ChestHitbox : MonoBehaviour
    {
        public bool MouseOver { get; private set; }

        private void Start()
        {
            var mouseCollider = gameObject.AddComponent<BoxCollider2D>();
            mouseCollider.isTrigger = true;
            mouseCollider.size = new Vector2(1.8f, 2f);
            mouseCollider.offset = new Vector2(0.6667f, 0.6667f);
        }

        private void OnMouseEnter() => MouseOver = true;
        private void OnMouseExit() => MouseOver = false;
    }
}
