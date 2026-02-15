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
