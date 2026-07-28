using System;
using System.Collections.Generic;

namespace BirthdayReminder.Data
{
    /// <summary>
    /// Represents an NPC's birthday information
    /// </summary>
    public class NPCBirthday
    {
        public string NPCName { get; set; }
        public string Season { get; set; }  // "Spring", "Summer", "Fall", "Winter"
        public int Day { get; set; }        // 1-27
        public List<string> LovedGifts { get; set; } = new List<string>();
        public List<string> LikedGifts { get; set; } = new List<string>();

        public NPCBirthday() { }

        public NPCBirthday(string npcName, string season, int day)
        {
            NPCName = npcName;
            Season = season;
            Day = day;
        }

        public bool IsBirthday(string currentSeason, int currentDay)
        {
            return string.Equals(Season, currentSeason, StringComparison.OrdinalIgnoreCase)
                   && Day == currentDay;
        }
    }

    /// <summary>
    /// Tracks which NPCs have been gifted on their birthday
    /// </summary>
    public class GiftTrackingData
    {
        public string CharacterName { get; set; }
        public int Year { get; set; }
        public string Season { get; set; }
        public int Day { get; set; }
        public HashSet<string> GiftedNPCs { get; set; } = new HashSet<string>();

        public GiftTrackingData() { }

        public GiftTrackingData(string characterName, int year, string season, int day)
        {
            CharacterName = characterName;
            Year = year;
            Season = season;
            Day = day;
            GiftedNPCs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool HasGifted(string npcName)
        {
            return GiftedNPCs.Contains(npcName);
        }

        public void MarkGifted(string npcName)
        {
            GiftedNPCs.Add(npcName);
        }

        public bool IsSameDay(int year, string season, int day)
        {
            return Year == year
                   && string.Equals(Season, season, StringComparison.OrdinalIgnoreCase)
                   && Day == day;
        }
    }

    /// <summary>
    /// Birthday display info for the HUD
    /// </summary>
    public class BirthdayDisplayInfo
    {
        public string NPCName { get; set; }
        public bool HasBeenGifted { get; set; }
        public string GiftHint { get; set; }  // Short hint: "Loves: X, Y, Z"
        public List<string> AllLovedGifts { get; set; } = new List<string>();  // Full list for expanded view
        public List<string> AllLikedGifts { get; set; } = new List<string>();  // Full list for expanded view

        public BirthdayDisplayInfo(string npcName, bool gifted, string giftHint = "")
        {
            NPCName = npcName;
            HasBeenGifted = gifted;
            GiftHint = giftHint;
        }

        public BirthdayDisplayInfo(string npcName, bool gifted, string giftHint, List<string> lovedGifts, List<string> likedGifts)
        {
            NPCName = npcName;
            HasBeenGifted = gifted;
            GiftHint = giftHint;
            AllLovedGifts = lovedGifts ?? new List<string>();
            AllLikedGifts = likedGifts ?? new List<string>();
        }
    }
}
