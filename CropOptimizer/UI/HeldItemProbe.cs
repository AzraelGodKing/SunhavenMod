using System;
using System.Collections.Generic;
using Wish;

namespace CropOptimizer.UI
{
    /// <summary>Detects whether the player has a watering can or fertilizer selected (Sun Haven 3.1).</summary>
    internal static class HeldItemProbe
    {
        public static bool IsWateringCanSelected()
        {
            if (Player.Instance?.UseItem is WateringCan)
                return true;

            return ItemMatches(item =>
                item.Type == ItemType.WateringCan
                || NameMatches(item, "watering"));
        }

        public static bool IsFertilizerSelected()
        {
            if (Player.Instance?.UseItem is Fertilizer)
                return true;

            return ItemMatches(item => NameMatches(item, "fertilizer", "compost"));
        }

        private static bool ItemMatches(Func<Item, bool> predicate)
        {
            foreach (Item item in EnumerateCandidateItems())
            {
                if (item.Equals(Item.Empty))
                    continue;
                if (predicate(item))
                    return true;
            }

            return false;
        }

        private static IEnumerable<Item> EnumerateCandidateItems()
        {
            Player player = Player.Instance;
            if (player == null)
                yield break;

            Item current = player.CurrentItem;
            if (current != null && !current.Equals(Item.Empty))
                yield return current;

            ItemIcon icon = Inventory.CurrentItemIcon;
            if (icon?.item != null && !icon.item.Equals(Item.Empty))
                yield return icon.item;

            PlayerInventory actionBar = player.PlayerInventory;
            if (actionBar?.Items == null)
                yield break;

            int slot = PlayerInventory.CurrentSlot;
            if (slot < 0 || slot >= actionBar.Items.Count)
                yield break;

            SlotItemData slotData = actionBar.Items[slot];
            if (slotData?.item != null && !slotData.item.Equals(Item.Empty))
                yield return slotData.item;
        }

        private static bool NameMatches(Item item, params string[] keywords)
        {
            if (item.Equals(Item.Empty))
                return false;

            string label = ResolveItemLabel(item);
            if (string.IsNullOrEmpty(label))
                return false;

            foreach (string keyword in keywords)
            {
                if (label.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static string ResolveItemLabel(Item item)
        {
            ItemIcon icon = Inventory.CurrentItemIcon;
            if (icon?.itemData != null)
            {
                string formatted = icon.itemData.FormattedName;
                if (!string.IsNullOrWhiteSpace(formatted))
                    return formatted;
            }

            Player player = Player.Instance;
            if (player?.ItemData != null && player.Item.Equals(item))
            {
                string formatted = player.ItemData.FormattedName;
                if (!string.IsNullOrWhiteSpace(formatted))
                    return formatted;
            }

            return null;
        }
    }
}
