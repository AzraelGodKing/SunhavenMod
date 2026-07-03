using UnityEngine;

namespace GiftingAssistant.Game
{
    /// <summary>Heart slot math aligned with vanilla RelationshipHUD (5 points per heart).</summary>
    internal static class RelationshipHeartRules
    {
        public const int PointsPerHeart = 5;
        public const int HeartsPerRow = 5;

        /// <summary>0-based slot index for the silver marriage milestone heart (Lynn panel slot 15).</summary>
        public const int RomanceMilestoneSlotIndex = 14;

        public static int GetMaxHearts(float points)
        {
            if (points >= 75f)
                return 20;
            if (points >= 50f)
                return 15;
            return 10;
        }

        public static int GetFullHearts(float points, int maxHearts)
        {
            return Mathf.Clamp(Mathf.FloorToInt(points / PointsPerHeart), 0, maxHearts);
        }

        public static bool IsMilestoneSlot(int slotIndex, bool romanceable, int maxHearts)
        {
            return romanceable && maxHearts >= 15 && slotIndex == RomanceMilestoneSlotIndex;
        }
    }
}
