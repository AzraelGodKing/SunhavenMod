using System;
using System.Collections.Generic;
using System.Linq;

namespace HavensAlmanac.Data
{
    /// <summary>
    /// Central data manager that owns the list of active data providers.
    /// Providers are registered at startup based on which mods are installed.
    /// </summary>
    public class AlmanacDataAggregator
    {
        private readonly List<IModDataProvider> _providers = new List<IModDataProvider>();

        public IReadOnlyList<IModDataProvider> Providers => _providers;
        public int InstalledModCount => _providers.Count;
        public bool HasAnyData => _providers.Any(p => p.IsReady);

        /// <summary>
        /// Count of providers that represent actual other-mod integrations
        /// (excludes the always-registered built-in Mod Health provider).
        /// Used for "no supported mods detected" messaging so it only fires when
        /// the user really has nothing else installed to aggregate data from.
        /// </summary>
        public int IntegrationModCount =>
            _providers.Count(p => !(p is Integration.ModHealthDataProvider));

        /// <summary>
        /// True when at least one provider reports it has daily-relevant content
        /// to show in the morning briefing right now. The DailyBriefing uses this
        /// to suppress itself when there's nothing noteworthy to say.
        /// </summary>
        public bool HasAnyBriefingContent => _providers.Any(p => p.HasBriefingContent);

        public void RegisterProvider(IModDataProvider provider)
        {
            _providers.Add(provider);
        }

        public void RefreshAll()
        {
            foreach (var provider in _providers)
            {
                try
                {
                    provider.Refresh();
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogWarning($"[Almanac] Error refreshing {provider.ModName}: {ex.Message}");
                }
            }
        }
    }
}
