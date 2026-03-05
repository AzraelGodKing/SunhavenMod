using System.Collections.Generic;

namespace SunHavenMuseumUtilityTracker.Data
{
    /// <summary>
    /// Defines all museum sections, bundles, and items.
    /// Item IDs match actual Sun Haven game item IDs for icon loading.
    /// </summary>
    public static class MuseumContent
    {
        private static List<MuseumSection> _sections;

        public static List<MuseumSection> GetAllSections()
        {
            if (_sections == null)
            {
                _sections = BuildMuseumContent();
            }
            return _sections;
        }

        private static List<MuseumSection> BuildMuseumContent()
        {
            var sections = new List<MuseumSection>();

            // ==================== HALL OF GEMS ====================
            var hallOfGems = new MuseumSection("hall_of_gems", "The Hall of Gems", "A dazzling collection of precious gems and minerals.");

            // Bundle IDs from game: Wish.MuseumCurator.miningMuseumProgress (sync uses these progress keys)
            // Mana Bundle (1 item, requires 20)
            var manaBundle = new MuseumBundle("ManaBundle", "Mana Bundle", "hall_of_gems", "Collect 20 Mana Drops.");
            manaBundle.Items.AddRange(new[]
            {
                new MuseumItem("mana_drop", "Mana Drop (x20)", "ManaBundle", 60234, ItemRarity.Rare, "A shimmering drop of pure mana. Donate 20."),
            });
            hallOfGems.Bundles.Add(manaBundle);

            // Money Bundle
            var moneyBundle = new MuseumBundle("MoneyBundle", "Money Bundle", "hall_of_gems", "Wealth and currency collection.");
            moneyBundle.Items.AddRange(new[]
            {
                new MuseumItem("coins", "Coins (x25,000)", "MoneyBundle", 60000, ItemRarity.Common, "Gold coins. Donate 25,000."),
                new MuseumItem("mana_orbs", "Mana Orbs (x1,000)", "MoneyBundle", 60001, ItemRarity.Uncommon, "Magical mana orbs. Donate 1,000."),
                new MuseumItem("tickets", "Tickets (x1,000)", "MoneyBundle", 60002, ItemRarity.Uncommon, "Event tickets. Donate 1,000."),
            });
            hallOfGems.Bundles.Add(moneyBundle);

            // Golden Bundle
            var goldenBundle = new MuseumBundle("GoldenBundle", "Golden Bundle", "hall_of_gems", "Rare golden items collection.");
            goldenBundle.Items.AddRange(new[]
            {
                new MuseumItem("golden_milk", "Golden Milk", "GoldenBundle", 2920, ItemRarity.Rare, "Milk with a golden hue."),
                new MuseumItem("golden_egg", "Golden Egg", "GoldenBundle", 3052, ItemRarity.Rare, "A shimmering golden egg."),
                new MuseumItem("golden_wool", "Golden Wool", "GoldenBundle", 2113, ItemRarity.Rare, "Luxurious golden wool."),
                new MuseumItem("golden_pomegranate", "Golden Pomegranate", "GoldenBundle", 3053, ItemRarity.Rare, "A golden pomegranate."),
                new MuseumItem("golden_log", "Golden Log", "GoldenBundle", 2114, ItemRarity.Rare, "A log of golden wood."),
                new MuseumItem("golden_feather", "Golden Feather", "GoldenBundle", 2115, ItemRarity.Rare, "A brilliant golden feather."),
                new MuseumItem("golden_silk", "Golden Silk", "GoldenBundle", 2116, ItemRarity.Rare, "Fine golden silk."),
                new MuseumItem("golden_apple", "Golden Apple", "GoldenBundle", 3054, ItemRarity.Rare, "A golden apple."),
                new MuseumItem("golden_orange", "Golden Orange", "GoldenBundle", 3055, ItemRarity.Rare, "A golden orange."),
                new MuseumItem("golden_strawberry", "Golden Strawberry", "GoldenBundle", 3057, ItemRarity.Rare, "A golden strawberry."),
                new MuseumItem("golden_blueberry", "Golden Blueberry", "GoldenBundle", 3056, ItemRarity.Rare, "A golden blueberry."),
                new MuseumItem("golden_peach", "Golden Peach", "GoldenBundle", 3058, ItemRarity.Rare, "A golden peach."),
                new MuseumItem("golden_raspberry", "Golden Raspberry", "GoldenBundle", 3059, ItemRarity.Rare, "A golden raspberry."),
            });
            hallOfGems.Bundles.Add(goldenBundle);

            // Bars Bundle
            var barsBundle = new MuseumBundle("BarsBundle", "Bars Bundle", "hall_of_gems", "Metal bars collection.");
            barsBundle.Items.AddRange(new[]
            {
                new MuseumItem("copper_bar", "Copper Bar", "BarsBundle", 1200, ItemRarity.Common, "A bar of copper."),
                new MuseumItem("iron_bar", "Iron Bar", "BarsBundle", 1201, ItemRarity.Common, "A bar of iron."),
                new MuseumItem("adamant_bar", "Adamant Bar", "BarsBundle", 1202, ItemRarity.Uncommon, "A bar of adamant."),
                new MuseumItem("mithril_bar", "Mithril Bar", "BarsBundle", 1203, ItemRarity.Rare, "A bar of mithril."),
                new MuseumItem("sunite_bar", "Sunite Bar", "BarsBundle", 1204, ItemRarity.Epic, "A bar of sunite."),
                new MuseumItem("gold_bar", "Gold Bar", "BarsBundle", 1205, ItemRarity.Rare, "A bar of gold."),
                new MuseumItem("glorite_bar", "Glorite Bar", "BarsBundle", 1206, ItemRarity.Legendary, "A bar of glorite."),
                new MuseumItem("elven_steel_bar", "Elven Steel Bar", "BarsBundle", 1207, ItemRarity.Epic, "A bar of elven steel."),
            });
            hallOfGems.Bundles.Add(barsBundle);

            // Gem Bundle (game uses "GemBundle" not "GemsBundle")
            var gemsBundle = new MuseumBundle("GemBundle", "Gems Bundle", "hall_of_gems", "Precious gems collection.");
            gemsBundle.Items.AddRange(new[]
            {
                new MuseumItem("sapphire", "Sapphire", "GemBundle", 1000, ItemRarity.Rare, "A brilliant blue sapphire."),
                new MuseumItem("ruby", "Ruby", "GemBundle", 1001, ItemRarity.Rare, "A deep red ruby."),
                new MuseumItem("amethyst", "Amethyst", "GemBundle", 1002, ItemRarity.Uncommon, "A purple amethyst."),
                new MuseumItem("diamond", "Diamond", "GemBundle", 1003, ItemRarity.Epic, "A sparkling diamond."),
                new MuseumItem("havenite", "Havenite", "GemBundle", 1004, ItemRarity.Legendary, "A rare havenite gem."),
                new MuseumItem("black_diamond", "Black Diamond", "GemBundle", 1005, ItemRarity.Legendary, "A mysterious black diamond."),
                new MuseumItem("dizzite", "Dizzite", "GemBundle", 10620, ItemRarity.Rare, "A shimmering dizzite gem."),
            });
            hallOfGems.Bundles.Add(gemsBundle);

            // Nel'Vari Mines Bundle
            var nelvariMinesBundle = new MuseumBundle("NelvariMinesBundle", "Nel'Vari Mines Bundle", "hall_of_gems", "Treasures from the Nel'Vari Mines.");
            nelvariMinesBundle.Items.AddRange(new[]
            {
                new MuseumItem("mana_shard", "Mana Shard (x5)", "NelvariMinesBundle", 18015, ItemRarity.Rare, "A shard of crystallized mana. Donate 5."),
                new MuseumItem("sparkling_dragon_scale", "Sparkling Dragon Scale (x5)", "NelvariMinesBundle", 1115, ItemRarity.Epic, "A sparkling dragon scale. Donate 5."),
                new MuseumItem("sharp_dragon_scale", "Sharp Dragon Scale (x5)", "NelvariMinesBundle", 1116, ItemRarity.Epic, "A sharp dragon scale. Donate 5."),
                new MuseumItem("tough_dragon_scale", "Tough Dragon Scale (x5)", "NelvariMinesBundle", 1114, ItemRarity.Epic, "A tough dragon scale. Donate 5."),
            });
            hallOfGems.Bundles.Add(nelvariMinesBundle);

            // Withergate Mines Bundle
            var withergateMinesBundle = new MuseumBundle("WithergateMinesBundle", "Withergate Mines Bundle", "hall_of_gems", "Sweet treasures from the Withergate Mines.");
            withergateMinesBundle.Items.AddRange(new[]
            {
                new MuseumItem("candy_corn_pieces", "Candy Corn Pieces (x5)", "WithergateMinesBundle", 18016, ItemRarity.Rare, "Candy corn pieces. Donate 5."),
                new MuseumItem("rock_candy_gem", "Rock Candy Gem (x5)", "WithergateMinesBundle", 3759, ItemRarity.Rare, "A rock candy gem. Donate 5."),
                new MuseumItem("jawbreaker_gem", "Jawbreaker Gem (x5)", "WithergateMinesBundle", 3761, ItemRarity.Rare, "A jawbreaker gem. Donate 5."),
                new MuseumItem("hard_butterscotch_gem", "Hard Butterscotch Gem (x5)", "WithergateMinesBundle", 3760, ItemRarity.Rare, "A hard butterscotch gem. Donate 5."),
            });
            hallOfGems.Bundles.Add(withergateMinesBundle);

            sections.Add(hallOfGems);

            // ==================== HALL OF CULTURE ====================
            // Bundle IDs and counts from game: Wish.MuseumCurator.culturalMuseumProgress
            var hallOfCulture = new MuseumSection("hall_of_culture", "The Hall of Culture", "Crops, flowers, foraging, combat, alchemy, exploration, and farming from all regions.");

            AddPlaceholderBundle(hallOfCulture, "WinterCropsBundle", "Winter Crops", 15);
            AddPlaceholderBundle(hallOfCulture, "FlowersBundle", "Flowers", 11);
            AddPlaceholderBundle(hallOfCulture, "SpringCropsBundle", "Spring Crops", 13);
            AddPlaceholderBundle(hallOfCulture, "FallCropsBundle", "Fall Crops", 9);
            AddPlaceholderBundle(hallOfCulture, "NelvariTempleBooks", "Nel'Vari Temple Books", 15);
            AddPlaceholderBundle(hallOfCulture, "SummerCropsBundle", "Summer Crops", 10);
            AddPlaceholderBundle(hallOfCulture, "ForagingBundle", "Foraging", 12);
            AddPlaceholderBundle(hallOfCulture, "CombatBundle", "Combat", 14);
            AddPlaceholderBundle(hallOfCulture, "AlchemyBundle", "Alchemy", 11);
            AddPlaceholderBundle(hallOfCulture, "ExplorationBundle", "Exploration", 10);
            AddPlaceholderBundle(hallOfCulture, "WithergateFarmingBundle", "Withergate Farming", 8);
            AddPlaceholderBundle(hallOfCulture, "NelvariFarmingBundle", "Nel'Vari Farming", 11);

            sections.Add(hallOfCulture);

            // ==================== AQUARIUM ====================
            // Bundle IDs and counts from game: Wish.MuseumCurator.aquaticMuseumProgress
            var aquarium = new MuseumSection("aquarium", "Aquarium", "Fish and aquatic life from all waters and seasons.");

            AddPlaceholderBundle(aquarium, "FishingBundle", "Fishing (Relics)", 11);
            AddPlaceholderBundle(aquarium, "MuseumAquariumBigTank", "Big Tank", 26);
            AddPlaceholderBundle(aquarium, "MuseumAquariumSpring", "Spring Tank", 9);
            AddPlaceholderBundle(aquarium, "MuseumAquariumSummer", "Summer Tank", 9);
            AddPlaceholderBundle(aquarium, "MuseumAquariumFall", "Fall Tank", 9);
            AddPlaceholderBundle(aquarium, "MuseumAquariumWinter", "Winter Tank", 9);
            AddPlaceholderBundle(aquarium, "MuseumAquariumNelvari", "Nel'Vari Tank", 14);
            AddPlaceholderBundle(aquarium, "MuseumAquariumWithergate", "Withergate Tank", 22);

            sections.Add(aquarium);

            return sections;
        }

        /// <summary>
        /// Adds a bundle with placeholder slots so counts match the game. Item names are not in code (they live in Unity assets);
        /// Sync uses the game's progress key (bundle Id) and will mark all slots when the bundle is complete.
        /// </summary>
        private static void AddPlaceholderBundle(MuseumSection section, string bundleId, string displayName, int slotCount)
        {
            var bundle = new MuseumBundle(bundleId, displayName, section.Id, $"Donate all {slotCount} items to complete this bundle.");
            for (int i = 1; i <= slotCount; i++)
            {
                bundle.Items.Add(new MuseumItem(
                    bundleId + "_slot_" + i,
                    "Item " + i,
                    bundleId,
                    -1, // Placeholder: real item IDs live in Unity assets; -1 avoids matching in FindByGameItemId
                    ItemRarity.Common,
                    "Slot " + i + " (see in-game museum for exact item)."));
            }
            section.Bundles.Add(bundle);
        }

        /// <summary>
        /// Gets a flattened list of all museum items.
        /// </summary>
        public static List<MuseumItem> GetAllItems()
        {
            var items = new List<MuseumItem>();
            foreach (var section in GetAllSections())
            {
                foreach (var bundle in section.Bundles)
                {
                    items.AddRange(bundle.Items);
                }
            }
            return items;
        }

        /// <summary>
        /// Gets all bundle IDs across all sections.
        /// </summary>
        public static List<string> GetAllBundleIds()
        {
            var ids = new List<string>();
            foreach (var section in GetAllSections())
            {
                foreach (var bundle in section.Bundles)
                {
                    ids.Add(bundle.Id);
                }
            }
            return ids;
        }

        /// <summary>
        /// Gets the game progress key for a bundle. Used to check completion status in game saves.
        /// </summary>
        public static string GetProgressKeyForBundle(string bundleId)
        {
            foreach (var section in GetAllSections())
            {
                foreach (var bundle in section.Bundles)
                {
                    if (bundle.Id == bundleId)
                        return bundle.Id;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets all items within a specific bundle.
        /// </summary>
        public static List<MuseumItem> GetItemsInBundle(string bundleId)
        {
            foreach (var section in GetAllSections())
            {
                foreach (var bundle in section.Bundles)
                {
                    if (bundle.Id == bundleId)
                        return bundle.Items;
                }
            }
            return new List<MuseumItem>();
        }

        /// <summary>
        /// Finds a bundle by its game progress key.
        /// </summary>
        public static MuseumBundle FindBundleByProgressKey(string progressKey)
        {
            foreach (var section in GetAllSections())
            {
                foreach (var bundle in section.Bundles)
                {
                    if (bundle.Id == progressKey)
                        return bundle;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds an item by its game item ID.
        /// </summary>
        public static MuseumItem FindByGameItemId(int gameItemId)
        {
            foreach (var section in GetAllSections())
            {
                foreach (var bundle in section.Bundles)
                {
                    foreach (var item in bundle.Items)
                    {
                        if (item.GameItemId == gameItemId)
                            return item;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Finds an item by its ID.
        /// </summary>
        public static MuseumItem FindById(string itemId)
        {
            foreach (var section in GetAllSections())
            {
                foreach (var bundle in section.Bundles)
                {
                    foreach (var item in bundle.Items)
                    {
                        if (item.Id == itemId)
                            return item;
                    }
                }
            }
            return null;
        }
    }
}
