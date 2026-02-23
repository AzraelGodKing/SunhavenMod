using UnityEngine;

namespace HavenDevTools.API
{
    /// <summary>
    /// Interface for custom panels that third-party mods can register with HavenDevTools.
    /// Implement this and call DevToolsRegistry.Register(panel) to add your mod to the Extensions tab.
    /// </summary>
    public interface IDevToolsPanel
    {
        /// <summary>BepInEx plugin GUID (e.g. "com.author.modname")</summary>
        string ModGuid { get; }

        /// <summary>Display name shown in the Extensions tab</summary>
        string DisplayName { get; }

        /// <summary>Draw the panel content. Use the provided styles for consistency.</summary>
        void Draw(GUIStyle boxStyle, GUIStyle buttonStyle, GUIStyle labelStyle);
    }
}
