using System;
using System.Collections.Generic;

namespace GiftingAssistant.Data
{
    public enum GiftPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }

    [Serializable]
    public class GiftRosterEntry
    {
        public string NpcName { get; set; }
        public GiftPriority Priority { get; set; }

        /// <summary>Player-toggled "gifted today" flag, cleared on day rollover.</summary>
        public bool ManualGiftedToday { get; set; }

        /// <summary>
        /// Item IDs the player picked as this NPC's preferred gifts. Empty = fall back to the
        /// game's full loved/liked lists. Used to declutter the row and the Sun Haven Todo task.
        /// </summary>
        public List<int> PreferredGiftIds { get; set; }

        public GiftRosterEntry()
        {
            NpcName = "";
            Priority = GiftPriority.Normal;
            ManualGiftedToday = false;
            PreferredGiftIds = new List<int>();
        }

        public GiftRosterEntry(string npcName, GiftPriority priority = GiftPriority.Normal) : this()
        {
            NpcName = npcName;
            Priority = priority;
        }
    }

    [Serializable]
    public class GiftRosterData
    {
        public string CharacterName { get; set; }
        public List<GiftRosterEntry> Entries { get; set; }

        /// <summary>In-game date key (e.g. "1_Spring_3") of the last daily reset, so we only reset once per day.</summary>
        public string LastResetDateKey { get; set; }
        public DateTime LastUpdated { get; set; }

        public GiftRosterData()
        {
            Entries = new List<GiftRosterEntry>();
            LastResetDateKey = "";
            LastUpdated = DateTime.Now;
        }

        public GiftRosterData(string characterName) : this()
        {
            CharacterName = characterName;
        }
    }
}
