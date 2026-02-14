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

            // Mana Bundle (1 item, requires 20)
            var manaBundle = new MuseumBundle("mana_bundle", "Mana Bundle", "hall_of_gems", "Collect 20 Mana Drops.");
            manaBundle.Items.AddRange(new[]
            {
                new MuseumItem("mana_drop", "Mana Drop (x20)", "mana_bundle", 60234, ItemRarity.Rare, "A shimmering drop of pure mana. Donate 20."),
            });
            hallOfGems.Bundles.Add(manaBundle);

            // Money Bundle
            var moneyBundle = new MuseumBundle("money_bundle", "Money Bundle", "hall_of_gems", "Wealth and currency collection.");
            moneyBundle.Items.AddRange(new[]
            {
                new MuseumItem("coins", "Coins (x25,000)", "money_bundle", 60000, ItemRarity.Common, "Gold coins. Donate 25,000."),
                new MuseumItem("mana_orbs", "Mana Orbs (x1,000)", "money_bundle", 60001, ItemRarity.Uncommon, "Magical mana orbs. Donate 1,000."),
                new MuseumItem("tickets", "Tickets (x1,000)", "money_bundle", 60002, ItemRarity.Uncommon, "Event tickets. Donate 1,000."),
            });
            hallOfGems.Bundles.Add(moneyBundle);

            // Golden Bundle
            var goldenBundle = new MuseumBundle("golden_bundle", "Golden Bundle", "hall_of_gems", "Rare golden items collection.");
            goldenBundle.Items.AddRange(new[]
            {
                new MuseumItem("golden_milk", "Golden Milk", "golden_bundle", 2920, ItemRarity.Rare, "Milk with a golden hue."),
                new MuseumItem("golden_egg", "Golden Egg", "golden_bundle", 3052, ItemRarity.Rare, "A shimmering golden egg."),
                new MuseumItem("golden_wool", "Golden Wool", "golden_bundle", 2113, ItemRarity.Rare, "Luxurious golden wool."),
                new MuseumItem("golden_pomegranate", "Golden Pomegranate", "golden_bundle", 3053, ItemRarity.Rare, "A golden pomegranate."),
                new MuseumItem("golden_log", "Golden Log", "golden_bundle", 2114, ItemRarity.Rare, "A log of golden wood."),
                new MuseumItem("golden_feather", "Golden Feather", "golden_bundle", 2115, ItemRarity.Rare, "A brilliant golden feather."),
                new MuseumItem("golden_silk", "Golden Silk", "golden_bundle", 2116, ItemRarity.Rare, "Fine golden silk."),
                new MuseumItem("golden_apple", "Golden Apple", "golden_bundle", 3054, ItemRarity.Rare, "A golden apple."),
                new MuseumItem("golden_orange", "Golden Orange", "golden_bundle", 3055, ItemRarity.Rare, "A golden orange."),
                new MuseumItem("golden_strawberry", "Golden Strawberry", "golden_bundle", 3057, ItemRarity.Rare, "A golden strawberry."),
                new MuseumItem("golden_blueberry", "Golden Blueberry", "golden_bundle", 3056, ItemRarity.Rare, "A golden blueberry."),
                new MuseumItem("golden_peach", "Golden Peach", "golden_bundle", 3058, ItemRarity.Rare, "A golden peach."),
                new MuseumItem("golden_raspberry", "Golden Raspberry", "golden_bundle", 3059, ItemRarity.Rare, "A golden raspberry."),
            });
            hallOfGems.Bundles.Add(goldenBundle);

            // Bars Bundle
            var barsBundle = new MuseumBundle("bars_bundle", "Bars Bundle", "hall_of_gems", "Metal bars collection.");
            barsBundle.Items.AddRange(new[]
            {
                new MuseumItem("copper_bar", "Copper Bar", "bars_bundle", 1200, ItemRarity.Common, "A bar of copper."),
                new MuseumItem("iron_bar", "Iron Bar", "bars_bundle", 1201, ItemRarity.Common, "A bar of iron."),
                new MuseumItem("adamant_bar", "Adamant Bar", "bars_bundle", 1202, ItemRarity.Uncommon, "A bar of adamant."),
                new MuseumItem("mithril_bar", "Mithril Bar", "bars_bundle", 1203, ItemRarity.Rare, "A bar of mithril."),
                new MuseumItem("sunite_bar", "Sunite Bar", "bars_bundle", 1204, ItemRarity.Epic, "A bar of sunite."),
                new MuseumItem("gold_bar", "Gold Bar", "bars_bundle", 1205, ItemRarity.Rare, "A bar of gold."),
                new MuseumItem("glorite_bar", "Glorite Bar", "bars_bundle", 1206, ItemRarity.Legendary, "A bar of glorite."),
                new MuseumItem("elven_steel_bar", "Elven Steel Bar", "bars_bundle", 1207, ItemRarity.Epic, "A bar of elven steel."),
            });
            hallOfGems.Bundles.Add(barsBundle);

            // Gems Bundle
            var gemsBundle = new MuseumBundle("gems_bundle", "Gems Bundle", "hall_of_gems", "Precious gems collection.");
            gemsBundle.Items.AddRange(new[]
            {
                new MuseumItem("sapphire", "Sapphire", "gems_bundle", 1000, ItemRarity.Rare, "A brilliant blue sapphire."),
                new MuseumItem("ruby", "Ruby", "gems_bundle", 1001, ItemRarity.Rare, "A deep red ruby."),
                new MuseumItem("amethyst", "Amethyst", "gems_bundle", 1002, ItemRarity.Uncommon, "A purple amethyst."),
                new MuseumItem("diamond", "Diamond", "gems_bundle", 1003, ItemRarity.Epic, "A sparkling diamond."),
                new MuseumItem("havenite", "Havenite", "gems_bundle", 1004, ItemRarity.Legendary, "A rare havenite gem."),
                new MuseumItem("black_diamond", "Black Diamond", "gems_bundle", 1005, ItemRarity.Legendary, "A mysterious black diamond."),
                new MuseumItem("dizzite", "Dizzite", "gems_bundle", 10620, ItemRarity.Rare, "A shimmering dizzite gem."),
            });
            hallOfGems.Bundles.Add(gemsBundle);

            // Nel'Vari Mines Bundle
            var nelvariMinesBundle = new MuseumBundle("nelvari_mines_bundle", "Nel'Vari Mines Bundle", "hall_of_gems", "Treasures from the Nel'Vari Mines.");
            nelvariMinesBundle.Items.AddRange(new[]
            {
                new MuseumItem("mana_shard", "Mana Shard (x5)", "nelvari_mines_bundle", 18015, ItemRarity.Rare, "A shard of crystallized mana. Donate 5."),
                new MuseumItem("sparkling_dragon_scale", "Sparkling Dragon Scale (x5)", "nelvari_mines_bundle", 1115, ItemRarity.Epic, "A sparkling dragon scale. Donate 5."),
                new MuseumItem("sharp_dragon_scale", "Sharp Dragon Scale (x5)", "nelvari_mines_bundle", 1116, ItemRarity.Epic, "A sharp dragon scale. Donate 5."),
                new MuseumItem("tough_dragon_scale", "Tough Dragon Scale (x5)", "nelvari_mines_bundle", 1114, ItemRarity.Epic, "A tough dragon scale. Donate 5."),
            });
            hallOfGems.Bundles.Add(nelvariMinesBundle);

            // Withergate Mines Bundle
            var withergateMinesBundle = new MuseumBundle("withergate_mines_bundle", "Withergate Mines Bundle", "hall_of_gems", "Sweet treasures from the Withergate Mines.");
            withergateMinesBundle.Items.AddRange(new[]
            {
                new MuseumItem("candy_corn_pieces", "Candy Corn Pieces (x5)", "withergate_mines_bundle", 18016, ItemRarity.Rare, "Candy corn pieces. Donate 5."),
                new MuseumItem("rock_candy_gem", "Rock Candy Gem (x5)", "withergate_mines_bundle", 3759, ItemRarity.Rare, "A rock candy gem. Donate 5."),
                new MuseumItem("jawbreaker_gem", "Jawbreaker Gem (x5)", "withergate_mines_bundle", 3761, ItemRarity.Rare, "A jawbreaker gem. Donate 5."),
                new MuseumItem("hard_butterscotch_gem", "Hard Butterscotch Gem (x5)", "withergate_mines_bundle", 3760, ItemRarity.Rare, "A hard butterscotch gem. Donate 5."),
            });
            hallOfGems.Bundles.Add(withergateMinesBundle);

            sections.Add(hallOfGems);

            // ==================== HALL OF CULTURE ====================
            var hallOfCulture = new MuseumSection("hall_of_culture", "The Hall of Culture", "Artifacts and relics from civilizations past and present.");

            // Ancient Artifacts Bundle
            var ancientArtifacts = new MuseumBundle("ancient_artifacts", "Ancient Artifacts", "hall_of_culture", "Relics from ancient civilizations.");
            ancientArtifacts.Items.AddRange(new[]
            {
                new MuseumItem("coins", "Ancient Coins", "ancient_artifacts", 60000, ItemRarity.Uncommon, "A pile of ancient coins."),
                new MuseumItem("blue_elven_vase", "Blue Elven Vase", "ancient_artifacts", 10160, ItemRarity.Rare, "A beautifully decorated elven vase."),
                new MuseumItem("red_elven_vase", "Red Elven Vase", "ancient_artifacts", 10161, ItemRarity.Rare, "A red elven vase."),
                new MuseumItem("green_elven_vase", "Green Elven Vase", "ancient_artifacts", 10162, ItemRarity.Rare, "A green elven vase."),
                new MuseumItem("paper_scroll", "Paper Scroll", "ancient_artifacts", 50337, ItemRarity.Common, "An ancient paper scroll."),
                new MuseumItem("starfish_artifact", "Starfish", "ancient_artifacts", 2103, ItemRarity.Common, "A preserved starfish."),
            });
            hallOfCulture.Bundles.Add(ancientArtifacts);

            // Nelvari Relics Bundle
            var nelvariRelics = new MuseumBundle("nelvari_relics", "Nelvari Relics", "hall_of_culture", "Mysterious items from the Nelvari people.");
            nelvariRelics.Items.AddRange(new[]
            {
                new MuseumItem("nelvari_totem", "Nelvari Totem", "nelvari_relics", 10697, ItemRarity.Rare, "A carved Nelvari totem."),
                new MuseumItem("ancient_amari_totem", "Ancient Amari Totem", "nelvari_relics", 20160, ItemRarity.Epic, "An ancient Amari totem."),
                new MuseumItem("exploration_totem", "Exploration Totem", "nelvari_relics", 10692, ItemRarity.Rare, "A totem for exploration."),
                new MuseumItem("combat_totem", "Combat Totem", "nelvari_relics", 10699, ItemRarity.Rare, "A totem of combat."),
                new MuseumItem("irisss_enchanted_totem", "Iris's Enchanted Totem", "nelvari_relics", 6218, ItemRarity.Legendary, "A magically enchanted totem."),
            });
            hallOfCulture.Bundles.Add(nelvariRelics);

            // Withergate Remnants Bundle
            var withergateRemnants = new MuseumBundle("withergate_remnants", "Withergate Remnants", "hall_of_culture", "Dark artifacts from the Withergate era.");
            withergateRemnants.Items.AddRange(new[]
            {
                new MuseumItem("withergate_totem", "Withergate Totem", "withergate_remnants", 10693, ItemRarity.Rare, "A dark totem from Withergate."),
                new MuseumItem("withergate_mask", "Withergate Mask", "withergate_remnants", 5100, ItemRarity.Epic, "A mask from the Withergate."),
                new MuseumItem("bronze_dragon_relic", "Bronze Dragon Relic", "withergate_remnants", 20152, ItemRarity.Legendary, "A relic of an ancient dragon."),
            });
            hallOfCulture.Bundles.Add(withergateRemnants);

            // Seasonal Totems Bundle
            var seasonalTotems = new MuseumBundle("seasonal_totems", "Seasonal Totems", "hall_of_culture", "Totems representing the seasons.");
            seasonalTotems.Items.AddRange(new[]
            {
                new MuseumItem("spring_totem", "Spring Totem", "seasonal_totems", 10735, ItemRarity.Uncommon, "A totem of spring."),
                new MuseumItem("summer_totem", "Summer Totem", "seasonal_totems", 10736, ItemRarity.Uncommon, "A totem of summer."),
                new MuseumItem("fall_totem", "Fall Totem", "seasonal_totems", 10734, ItemRarity.Uncommon, "A totem of fall."),
                new MuseumItem("winter_totem", "Winter Totem", "seasonal_totems", 10737, ItemRarity.Uncommon, "A totem of winter."),
                new MuseumItem("sun_haven_totem", "Sun Haven Totem", "seasonal_totems", 10739, ItemRarity.Rare, "A totem of Sun Haven."),
            });
            hallOfCulture.Bundles.Add(seasonalTotems);

            sections.Add(hallOfCulture);

            // ==================== AQUARIUM ====================
            var aquarium = new MuseumSection("aquarium", "Aquarium", "A collection of fish and aquatic life from all waters.");

            // Freshwater Fish Bundle
            var freshwaterFish = new MuseumBundle("freshwater_fish", "Freshwater Fish", "aquarium", "Fish from rivers and lakes.");
            freshwaterFish.Items.AddRange(new[]
            {
                new MuseumItem("rainbow_trout", "Rainbow Trout", "freshwater_fish", 15004, ItemRarity.Common, "A colorful rainbow trout."),
                new MuseumItem("carp", "Carp", "freshwater_fish", 15010, ItemRarity.Common, "A golden carp."),
                new MuseumItem("silver_carp", "Silver Carp", "freshwater_fish", 15012, ItemRarity.Common, "A silver carp."),
                new MuseumItem("catfish", "Catfish", "freshwater_fish", 15018, ItemRarity.Common, "A whiskered catfish."),
                new MuseumItem("salmon", "Salmon", "freshwater_fish", 15085, ItemRarity.Uncommon, "A pink salmon."),
                new MuseumItem("northern_pike", "Northern Pike", "freshwater_fish", 15093, ItemRarity.Uncommon, "A northern pike."),
                new MuseumItem("bashful_pike", "Bashful Pike", "freshwater_fish", 15015, ItemRarity.Rare, "A bashful pike."),
                new MuseumItem("ironhead_sturgeon", "Ironhead Sturgeon", "freshwater_fish", 15024, ItemRarity.Rare, "An ironhead sturgeon."),
            });
            aquarium.Bundles.Add(freshwaterFish);

            // Saltwater Fish Bundle
            var saltwaterFish = new MuseumBundle("saltwater_fish", "Saltwater Fish", "aquarium", "Fish from the ocean.");
            saltwaterFish.Items.AddRange(new[]
            {
                new MuseumItem("sea_bass", "Sea Bass", "saltwater_fish", 15006, ItemRarity.Common, "A sea bass."),
                new MuseumItem("tuna", "Tuna", "saltwater_fish", 15087, ItemRarity.Uncommon, "A mighty tuna."),
                new MuseumItem("red_snapper", "Red Snapper", "saltwater_fish", 15011, ItemRarity.Uncommon, "A red snapper."),
                new MuseumItem("flower_flounder", "Flower Flounder", "saltwater_fish", 15114, ItemRarity.Uncommon, "A flower flounder."),
                new MuseumItem("blunted_swordfish", "Blunted Swordfish", "saltwater_fish", 15017, ItemRarity.Rare, "A blunted swordfish."),
                new MuseumItem("king_salmon", "King Salmon", "saltwater_fish", 15126, ItemRarity.Epic, "A majestic king salmon."),
            });
            aquarium.Bundles.Add(saltwaterFish);

            // Bass Collection Bundle
            var bassCollection = new MuseumBundle("bass_collection", "Bass Collection", "aquarium", "Various bass species.");
            bassCollection.Items.AddRange(new[]
            {
                new MuseumItem("averagemouth_bass", "Averagemouth Bass", "bass_collection", 15020, ItemRarity.Common, "An averagemouth bass."),
                new MuseumItem("black_bass", "Black Bass", "bass_collection", 15084, ItemRarity.Common, "A black bass."),
                new MuseumItem("rock_bass", "Rock Bass", "bass_collection", 15092, ItemRarity.Uncommon, "A rock bass."),
                new MuseumItem("blooming_bass", "Blooming Bass", "bass_collection", 15115, ItemRarity.Rare, "A blooming bass."),
                new MuseumItem("bonemouth_bass", "Bonemouth Bass", "bass_collection", 15028, ItemRarity.Rare, "A bonemouth bass."),
            });
            aquarium.Bundles.Add(bassCollection);

            // Exotic Fish Bundle
            var exoticFish = new MuseumBundle("exotic_fish", "Exotic Fish", "aquarium", "Rare and unusual fish species.");
            exoticFish.Items.AddRange(new[]
            {
                new MuseumItem("pufferfish", "Pufferfish", "exotic_fish", 15007, ItemRarity.Rare, "A spiny pufferfish."),
                new MuseumItem("seahorse", "Seahorse", "exotic_fish", 15122, ItemRarity.Epic, "A delicate seahorse."),
                new MuseumItem("crystal_tetra", "Crystal Tetra", "exotic_fish", 15044, ItemRarity.Rare, "A crystal tetra."),
                new MuseumItem("koi_fish", "Koi Fish", "exotic_fish", 15090, ItemRarity.Rare, "A beautiful koi fish."),
                new MuseumItem("royal_koi", "Royal Koi", "exotic_fish", 15057, ItemRarity.Legendary, "A legendary royal koi."),
                new MuseumItem("golden_carp", "Golden Carp", "exotic_fish", 15026, ItemRarity.Epic, "A shimmering golden carp."),
            });
            aquarium.Bundles.Add(exoticFish);

            // Legendary Sea Creatures Bundle
            var legendaryCreatures = new MuseumBundle("legendary_creatures", "Legendary Sea Creatures", "aquarium", "Mythical creatures of the deep.");
            legendaryCreatures.Items.AddRange(new[]
            {
                new MuseumItem("shadow_tuna", "Shadow Tuna", "legendary_creatures", 15029, ItemRarity.Epic, "A mysterious shadow tuna."),
                new MuseumItem("ghosthead_tuna", "Ghosthead Tuna", "legendary_creatures", 15073, ItemRarity.Epic, "A ghosthead tuna."),
                new MuseumItem("spiked_salmon", "Spiked Salmon", "legendary_creatures", 15061, ItemRarity.Epic, "A spiked salmon."),
                new MuseumItem("vampire_squid", "Vampire Squid", "legendary_creatures", 15075, ItemRarity.Legendary, "A vampire squid from the deep."),
                new MuseumItem("albino_squid", "Albino Squid", "legendary_creatures", 15079, ItemRarity.Legendary, "A rare albino squid."),
                new MuseumItem("geometric_squid", "Geometric Squid", "legendary_creatures", 15146, ItemRarity.Legendary, "A geometric squid."),
            });
            aquarium.Bundles.Add(legendaryCreatures);

            // Shellfish & Crustaceans Bundle
            var shellfish = new MuseumBundle("shellfish", "Shellfish & Crustaceans", "aquarium", "Shelled creatures from the waters.");
            shellfish.Items.AddRange(new[]
            {
                new MuseumItem("crab", "Crab", "shellfish", 15001, ItemRarity.Common, "A red crab."),
                new MuseumItem("lobster", "Lobster", "shellfish", 15088, ItemRarity.Uncommon, "A large lobster."),
                new MuseumItem("deadeye_shrimp", "Deadeye Shrimp", "shellfish", 15033, ItemRarity.Rare, "A deadeye shrimp."),
                new MuseumItem("clam", "Clam", "shellfish", 2104, ItemRarity.Common, "A fresh clam."),
                new MuseumItem("dumbo_octopus", "Dumbo Octopus", "shellfish", 15062, ItemRarity.Epic, "An adorable dumbo octopus."),
                new MuseumItem("scorching_squid", "Scorching Squid", "shellfish", 15107, ItemRarity.Rare, "A scorching squid."),
            });
            aquarium.Bundles.Add(shellfish);

            sections.Add(aquarium);

            return sections;
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
