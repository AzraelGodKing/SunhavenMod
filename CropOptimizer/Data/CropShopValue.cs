using System;

namespace CropOptimizer.Data
{
    /// <summary>
    /// Shop proceeds for one crop (or a farm total). Sun Haven uses gold; Nel'Vari uses orbs;
    /// Withergate uses tickets. Region produce often has <c>sellPrice == 0</c> with the real
    /// value on <c>orbSellPrice</c> / <c>ticketSellPrice</c>.
    /// </summary>
    internal readonly struct CropShopValue
    {
        public CropShopValue(int gold, int orbs, int tickets)
        {
            Gold = gold < 0 ? 0 : gold;
            Orbs = orbs < 0 ? 0 : orbs;
            Tickets = tickets < 0 ? 0 : tickets;
        }

        public int Gold { get; }
        public int Orbs { get; }
        public int Tickets { get; }

        public bool HasAny => Gold > 0 || Orbs > 0 || Tickets > 0;

        public CropShopValue Scaled(float qualityMultiplier)
        {
            float mul = qualityMultiplier < 0f ? 0f : qualityMultiplier;
            return new CropShopValue(
                Scale(Gold, mul),
                Scale(Orbs, mul),
                Scale(Tickets, mul));
        }

        private static int Scale(int amount, float multiplier)
        {
            if (amount <= 0 || multiplier <= 0f)
                return 0;
            return (int)Math.Round(amount * (double)multiplier, MidpointRounding.AwayFromZero);
        }

        public static CropShopValue MergePreferNonZero(CropShopValue primary, CropShopValue fallback)
        {
            return new CropShopValue(
                primary.Gold > 0 ? primary.Gold : fallback.Gold,
                primary.Orbs > 0 ? primary.Orbs : fallback.Orbs,
                primary.Tickets > 0 ? primary.Tickets : fallback.Tickets);
        }
    }
}
