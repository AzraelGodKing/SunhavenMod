using UnityEngine;

namespace HavenDevTools.UI
{
    /// <summary>Shared IMGUI layout helpers for the resizable Haven Dev Tools window.</summary>
    internal static class DebugWindowLayout
    {
        public const float MinWidth = 420f;
        public const float MinHeight = 340f;
        public const float HeaderHeight = 44f;
        public const float GripSize = 14f;
        public const float DefaultWidth = 560f;
        public const float DefaultHeight = 640f;

        public static void ClampToScreen(ref Rect windowRect)
        {
            float maxW = Mathf.Max(MinWidth, Screen.width - 16f);
            float maxH = Mathf.Max(MinHeight, Screen.height - 16f);
            windowRect.width = Mathf.Clamp(windowRect.width, MinWidth, maxW);
            windowRect.height = Mathf.Clamp(windowRect.height, MinHeight, maxH);
            windowRect.x = Mathf.Clamp(windowRect.x, 8f, Screen.width - windowRect.width - 8f);
            windowRect.y = Mathf.Clamp(windowRect.y, 8f, Screen.height - windowRect.height - 8f);
        }

        /// <summary>Bottom-right resize grip. Mouse positions are window-local.</summary>
        public static bool DrawResizeGrip(
            float windowWidth,
            float windowHeight,
            GUIStyle gripStyle,
            ref bool isResizing,
            ref Vector2 resizeStartMouse,
            ref Vector2 resizeStartSize,
            out float newWidth,
            out float newHeight)
        {
            newWidth = windowWidth;
            newHeight = windowHeight;

            var gripRect = new Rect(
                windowWidth - GripSize - 4f,
                windowHeight - GripSize - 4f,
                GripSize,
                GripSize);

            GUI.Box(gripRect, GUIContent.none, gripStyle);

            var evt = Event.current;
            if (evt.type == EventType.MouseDown && evt.button == 0 && gripRect.Contains(evt.mousePosition))
            {
                isResizing = true;
                resizeStartMouse = evt.mousePosition;
                resizeStartSize = new Vector2(windowWidth, windowHeight);
                evt.Use();
            }

            if (isResizing && evt.type == EventType.MouseDrag)
            {
                var delta = evt.mousePosition - resizeStartMouse;
                newWidth = Mathf.Clamp(resizeStartSize.x + delta.x, MinWidth, Screen.width - 16f);
                newHeight = Mathf.Clamp(resizeStartSize.y + delta.y, MinHeight, Screen.height - 16f);
                evt.Use();
                return true;
            }

            if (evt.type == EventType.MouseUp && isResizing)
            {
                isResizing = false;
                return true;
            }

            if (evt.type == EventType.MouseUp)
                isResizing = false;

            return false;
        }

        public static Texture2D CreateResizeGripTexture()
        {
            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var clear = new Color(0f, 0f, 0f, 0f);
            var line = new Color(0.55f, 0.65f, 0.75f, 0.85f);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            for (int i = 0; i < 4; i++)
            {
                int offset = 3 + i * 3;
                for (int d = 0; d < 4 - i; d++)
                {
                    int x = offset + d;
                    int y = size - 4 - d - i;
                    if (x >= 0 && x < size && y >= 0 && y < size)
                        pixels[y * size + x] = line;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
