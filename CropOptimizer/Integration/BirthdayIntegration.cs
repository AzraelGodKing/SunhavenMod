using System.Collections.Generic;
using BepInEx.Bootstrap;

namespace CropOptimizer.Integration
{
    internal sealed class BirthdayIntegration
    {
        public bool IsAvailable => Chainloader.PluginInfos.ContainsKey("com.azraelgodking.squirrelsbirthdayreminder");

        public IReadOnlyList<string> GetSuggestedReserveProduce()
        {
            if (!IsAvailable)
                return new List<string>();

            var manager = BirthdayReminder.Plugin.GetManager();
            if (manager == null || !manager.HasBirthdays)
                return new List<string>();

            var suggestions = new List<string>();
            foreach (var birthday in manager.TodaysBirthdays)
            {
                if (birthday.AllLovedGifts != null && birthday.AllLovedGifts.Count > 0)
                    suggestions.Add($"{birthday.NPCName}: {birthday.AllLovedGifts[0]}");
            }
            return suggestions;
        }
    }
}
