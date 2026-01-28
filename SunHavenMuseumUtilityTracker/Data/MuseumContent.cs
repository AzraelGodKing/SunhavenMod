using System.Collections.Generic;

namespace SunHavenMuseumUtilityTracker.Data
{
    /// <summary>
    /// Defines all museum sections, bundles, and items.
    /// Item IDs can be updated to match actual game item IDs.
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

            // Precious Gems Bundle
            var preciousGems = new MuseumBundle("precious_gems", "Precious Gems", "hall_of_gems", "The finest gems in all the land.");
            preciousGems.Items.AddRange(new[]
            {
                new MuseumItem("diamond", "Diamond", "precious_gems", 1001, ItemRarity.Legendary, "A brilliant, flawless diamond."),
                new MuseumItem("ruby", "Ruby", "precious_gems", 1002, ItemRarity.Epic, "A deep red ruby."),
                new MuseumItem("emerald", "Emerald", "precious_gems", 1003, ItemRarity.Epic, "A vibrant green emerald."),
                new MuseumItem("sapphire", "Sapphire", "precious_gems", 1004, ItemRarity.Epic, "A brilliant blue sapphire."),
                new MuseumItem("topaz", "Topaz", "precious_gems", 1005, ItemRarity.Rare, "A golden topaz gem."),
                new MuseumItem("amethyst", "Amethyst", "precious_gems", 1006, ItemRarity.Rare, "A purple amethyst crystal."),
            });
            hallOfGems.Bundles.Add(preciousGems);

            // Common Minerals Bundle
            var commonMinerals = new MuseumBundle("common_minerals", "Common Minerals", "hall_of_gems", "Everyday minerals found throughout the land.");
            commonMinerals.Items.AddRange(new[]
            {
                new MuseumItem("quartz", "Quartz", "common_minerals", 1010, ItemRarity.Common, "A clear quartz crystal."),
                new MuseumItem("rose_quartz", "Rose Quartz", "common_minerals", 1011, ItemRarity.Uncommon, "A pink-tinted quartz."),
                new MuseumItem("obsidian", "Obsidian", "common_minerals", 1012, ItemRarity.Uncommon, "Volcanic glass."),
                new MuseumItem("granite", "Granite", "common_minerals", 1013, ItemRarity.Common, "A piece of granite."),
                new MuseumItem("marble", "Marble", "common_minerals", 1014, ItemRarity.Common, "Polished marble stone."),
                new MuseumItem("slate", "Slate", "common_minerals", 1015, ItemRarity.Common, "A flat piece of slate."),
            });
            hallOfGems.Bundles.Add(commonMinerals);

            // Ore Specimens Bundle
            var oreSpecimens = new MuseumBundle("ore_specimens", "Ore Specimens", "hall_of_gems", "Raw ore samples from the mines.");
            oreSpecimens.Items.AddRange(new[]
            {
                new MuseumItem("copper_ore", "Copper Ore", "ore_specimens", 1020, ItemRarity.Common, "Raw copper ore."),
                new MuseumItem("iron_ore", "Iron Ore", "ore_specimens", 1021, ItemRarity.Common, "Raw iron ore."),
                new MuseumItem("gold_ore", "Gold Ore", "ore_specimens", 1022, ItemRarity.Rare, "Raw gold ore."),
                new MuseumItem("silver_ore", "Silver Ore", "ore_specimens", 1023, ItemRarity.Uncommon, "Raw silver ore."),
                new MuseumItem("mithril_ore", "Mithril Ore", "ore_specimens", 1024, ItemRarity.Epic, "Rare mithril ore."),
                new MuseumItem("adamantine_ore", "Adamantine Ore", "ore_specimens", 1025, ItemRarity.Legendary, "Legendary adamantine ore."),
            });
            hallOfGems.Bundles.Add(oreSpecimens);

            // Magical Crystals Bundle
            var magicalCrystals = new MuseumBundle("magical_crystals", "Magical Crystals", "hall_of_gems", "Crystals imbued with magical properties.");
            magicalCrystals.Items.AddRange(new[]
            {
                new MuseumItem("mana_crystal", "Mana Crystal", "magical_crystals", 1030, ItemRarity.Rare, "A crystal pulsing with magical energy."),
                new MuseumItem("sun_crystal", "Sun Crystal", "magical_crystals", 1031, ItemRarity.Epic, "A crystal that glows like the sun."),
                new MuseumItem("moon_crystal", "Moon Crystal", "magical_crystals", 1032, ItemRarity.Epic, "A crystal that glows in the moonlight."),
                new MuseumItem("void_crystal", "Void Crystal", "magical_crystals", 1033, ItemRarity.Legendary, "A crystal from the void itself."),
                new MuseumItem("elven_crystal", "Elven Crystal", "magical_crystals", 1034, ItemRarity.Rare, "An ancient elven crystal."),
            });
            hallOfGems.Bundles.Add(magicalCrystals);

            // Mana Bundle
            var manaBundle = new MuseumBundle("mana_bundle", "Mana", "hall_of_gems", "Magical mana essences and drops.");
            manaBundle.Items.AddRange(new[]
            {
                new MuseumItem("mana_drop", "Mana Drop", "mana_bundle", 60234, ItemRarity.Rare, "A shimmering drop of pure mana."),
            });
            hallOfGems.Bundles.Add(manaBundle);

            sections.Add(hallOfGems);

            // ==================== HALL OF CULTURE ====================
            var hallOfCulture = new MuseumSection("hall_of_culture", "The Hall of Culture", "Artifacts and relics from civilizations past and present.");

            // Ancient Artifacts Bundle
            var ancientArtifacts = new MuseumBundle("ancient_artifacts", "Ancient Artifacts", "hall_of_culture", "Relics from ancient civilizations.");
            ancientArtifacts.Items.AddRange(new[]
            {
                new MuseumItem("ancient_coin", "Ancient Coin", "ancient_artifacts", 2001, ItemRarity.Uncommon, "A coin from a forgotten kingdom."),
                new MuseumItem("ancient_vase", "Ancient Vase", "ancient_artifacts", 2002, ItemRarity.Rare, "A beautifully decorated vase."),
                new MuseumItem("ancient_scroll", "Ancient Scroll", "ancient_artifacts", 2003, ItemRarity.Rare, "A scroll with ancient writings."),
                new MuseumItem("ancient_statue", "Ancient Statue", "ancient_artifacts", 2004, ItemRarity.Epic, "A small stone statue."),
                new MuseumItem("ancient_tablet", "Ancient Tablet", "ancient_artifacts", 2005, ItemRarity.Epic, "A stone tablet with inscriptions."),
                new MuseumItem("ancient_crown", "Ancient Crown", "ancient_artifacts", 2006, ItemRarity.Legendary, "A crown worn by ancient royalty."),
            });
            hallOfCulture.Bundles.Add(ancientArtifacts);

            // Elven Heritage Bundle
            var elvenHeritage = new MuseumBundle("elven_heritage", "Elven Heritage", "hall_of_culture", "Treasures from the Elven kingdom.");
            elvenHeritage.Items.AddRange(new[]
            {
                new MuseumItem("elven_bow", "Elven Bow Fragment", "elven_heritage", 2010, ItemRarity.Rare, "Part of an ancient elven bow."),
                new MuseumItem("elven_pendant", "Elven Pendant", "elven_heritage", 2011, ItemRarity.Rare, "A delicate elven pendant."),
                new MuseumItem("elven_tome", "Elven Tome", "elven_heritage", 2012, ItemRarity.Epic, "A book of elven knowledge."),
                new MuseumItem("elven_ring", "Elven Ring", "elven_heritage", 2013, ItemRarity.Epic, "A finely crafted elven ring."),
                new MuseumItem("elven_crown", "Elven Crown", "elven_heritage", 2014, ItemRarity.Legendary, "The crown of an elven ruler."),
            });
            hallOfCulture.Bundles.Add(elvenHeritage);

            // Nelvari Relics Bundle
            var nelvariRelics = new MuseumBundle("nelvari_relics", "Nelvari Relics", "hall_of_culture", "Mysterious items from the Nelvari people.");
            nelvariRelics.Items.AddRange(new[]
            {
                new MuseumItem("nelvari_mask", "Nelvari Mask", "nelvari_relics", 2020, ItemRarity.Rare, "A ceremonial Nelvari mask."),
                new MuseumItem("nelvari_totem", "Nelvari Totem", "nelvari_relics", 2021, ItemRarity.Rare, "A carved wooden totem."),
                new MuseumItem("nelvari_drum", "Nelvari Drum", "nelvari_relics", 2022, ItemRarity.Uncommon, "A ritual drum."),
                new MuseumItem("nelvari_amulet", "Nelvari Amulet", "nelvari_relics", 2023, ItemRarity.Epic, "A protective amulet."),
                new MuseumItem("nelvari_spirit_stone", "Nelvari Spirit Stone", "nelvari_relics", 2024, ItemRarity.Legendary, "A stone containing ancestral spirits."),
            });
            hallOfCulture.Bundles.Add(nelvariRelics);

            // Withergate Remnants Bundle
            var withergateRemnants = new MuseumBundle("withergate_remnants", "Withergate Remnants", "hall_of_culture", "Dark artifacts from the Withergate era.");
            withergateRemnants.Items.AddRange(new[]
            {
                new MuseumItem("withergate_shard", "Withergate Shard", "withergate_remnants", 2030, ItemRarity.Rare, "A shard from the Withergate."),
                new MuseumItem("cursed_medallion", "Cursed Medallion", "withergate_remnants", 2031, ItemRarity.Rare, "A medallion with dark energy."),
                new MuseumItem("shadow_orb", "Shadow Orb", "withergate_remnants", 2032, ItemRarity.Epic, "An orb filled with shadows."),
                new MuseumItem("withergate_key", "Withergate Key", "withergate_remnants", 2033, ItemRarity.Legendary, "A key to the Withergate."),
            });
            hallOfCulture.Bundles.Add(withergateRemnants);

            // Fossils Bundle
            var fossils = new MuseumBundle("fossils", "Fossils", "hall_of_culture", "Preserved remains of ancient creatures.");
            fossils.Items.AddRange(new[]
            {
                new MuseumItem("trilobite_fossil", "Trilobite Fossil", "fossils", 2040, ItemRarity.Common, "A fossilized trilobite."),
                new MuseumItem("ammonite_fossil", "Ammonite Fossil", "fossils", 2041, ItemRarity.Common, "A spiral shell fossil."),
                new MuseumItem("fern_fossil", "Fern Fossil", "fossils", 2042, ItemRarity.Common, "An ancient fern impression."),
                new MuseumItem("bone_fossil", "Bone Fossil", "fossils", 2043, ItemRarity.Uncommon, "An ancient bone."),
                new MuseumItem("skull_fossil", "Skull Fossil", "fossils", 2044, ItemRarity.Rare, "A mysterious creature's skull."),
                new MuseumItem("dragon_fossil", "Dragon Fossil", "fossils", 2045, ItemRarity.Legendary, "The fossil of a dragon."),
            });
            hallOfCulture.Bundles.Add(fossils);

            sections.Add(hallOfCulture);

            // ==================== AQUARIUM ====================
            var aquarium = new MuseumSection("aquarium", "Aquarium", "A collection of fish and aquatic life from all waters.");

            // Freshwater Fish Bundle
            var freshwaterFish = new MuseumBundle("freshwater_fish", "Freshwater Fish", "aquarium", "Fish from rivers and lakes.");
            freshwaterFish.Items.AddRange(new[]
            {
                new MuseumItem("bass", "Bass", "freshwater_fish", 3001, ItemRarity.Common, "A common freshwater bass."),
                new MuseumItem("trout", "Trout", "freshwater_fish", 3002, ItemRarity.Common, "A spotted trout."),
                new MuseumItem("salmon", "Salmon", "freshwater_fish", 3003, ItemRarity.Uncommon, "A pink salmon."),
                new MuseumItem("catfish", "Catfish", "freshwater_fish", 3004, ItemRarity.Common, "A whiskered catfish."),
                new MuseumItem("carp", "Carp", "freshwater_fish", 3005, ItemRarity.Common, "A golden carp."),
                new MuseumItem("pike", "Pike", "freshwater_fish", 3006, ItemRarity.Uncommon, "A northern pike."),
                new MuseumItem("sturgeon", "Sturgeon", "freshwater_fish", 3007, ItemRarity.Rare, "An ancient sturgeon."),
            });
            aquarium.Bundles.Add(freshwaterFish);

            // Saltwater Fish Bundle
            var saltwaterFish = new MuseumBundle("saltwater_fish", "Saltwater Fish", "aquarium", "Fish from the ocean.");
            saltwaterFish.Items.AddRange(new[]
            {
                new MuseumItem("tuna", "Tuna", "saltwater_fish", 3010, ItemRarity.Uncommon, "A mighty tuna."),
                new MuseumItem("mackerel", "Mackerel", "saltwater_fish", 3011, ItemRarity.Common, "A striped mackerel."),
                new MuseumItem("flounder", "Flounder", "saltwater_fish", 3012, ItemRarity.Common, "A flat flounder."),
                new MuseumItem("sea_bass", "Sea Bass", "saltwater_fish", 3013, ItemRarity.Common, "A sea bass."),
                new MuseumItem("red_snapper", "Red Snapper", "saltwater_fish", 3014, ItemRarity.Uncommon, "A red snapper."),
                new MuseumItem("swordfish", "Swordfish", "saltwater_fish", 3015, ItemRarity.Rare, "A majestic swordfish."),
                new MuseumItem("marlin", "Marlin", "saltwater_fish", 3016, ItemRarity.Epic, "A blue marlin."),
            });
            aquarium.Bundles.Add(saltwaterFish);

            // Exotic Fish Bundle
            var exoticFish = new MuseumBundle("exotic_fish", "Exotic Fish", "aquarium", "Rare and unusual fish species.");
            exoticFish.Items.AddRange(new[]
            {
                new MuseumItem("angelfish", "Angelfish", "exotic_fish", 3020, ItemRarity.Rare, "A beautiful angelfish."),
                new MuseumItem("clownfish", "Clownfish", "exotic_fish", 3021, ItemRarity.Rare, "A colorful clownfish."),
                new MuseumItem("pufferfish", "Pufferfish", "exotic_fish", 3022, ItemRarity.Rare, "A spiny pufferfish."),
                new MuseumItem("lionfish", "Lionfish", "exotic_fish", 3023, ItemRarity.Epic, "A venomous lionfish."),
                new MuseumItem("seahorse", "Seahorse", "exotic_fish", 3024, ItemRarity.Epic, "A delicate seahorse."),
                new MuseumItem("manta_ray", "Manta Ray", "exotic_fish", 3025, ItemRarity.Epic, "A graceful manta ray."),
            });
            aquarium.Bundles.Add(exoticFish);

            // Legendary Sea Creatures Bundle
            var legendaryCreatures = new MuseumBundle("legendary_creatures", "Legendary Sea Creatures", "aquarium", "Mythical creatures of the deep.");
            legendaryCreatures.Items.AddRange(new[]
            {
                new MuseumItem("ghost_fish", "Ghost Fish", "legendary_creatures", 3030, ItemRarity.Epic, "A translucent ghost fish."),
                new MuseumItem("moonfish", "Moonfish", "legendary_creatures", 3031, ItemRarity.Epic, "A fish that glows like the moon."),
                new MuseumItem("sun_fish", "Sun Fish", "legendary_creatures", 3032, ItemRarity.Epic, "A massive ocean sunfish."),
                new MuseumItem("void_fish", "Void Fish", "legendary_creatures", 3033, ItemRarity.Legendary, "A fish from another dimension."),
                new MuseumItem("golden_koi", "Golden Koi", "legendary_creatures", 3034, ItemRarity.Legendary, "A legendary golden koi."),
                new MuseumItem("leviathan_scale", "Leviathan Scale", "legendary_creatures", 3035, ItemRarity.Legendary, "A scale from the legendary leviathan."),
            });
            aquarium.Bundles.Add(legendaryCreatures);

            // Shellfish & Crustaceans Bundle
            var shellfish = new MuseumBundle("shellfish", "Shellfish & Crustaceans", "aquarium", "Shelled creatures from the waters.");
            shellfish.Items.AddRange(new[]
            {
                new MuseumItem("crab", "Crab", "shellfish", 3040, ItemRarity.Common, "A red crab."),
                new MuseumItem("lobster", "Lobster", "shellfish", 3041, ItemRarity.Uncommon, "A large lobster."),
                new MuseumItem("shrimp", "Shrimp", "shellfish", 3042, ItemRarity.Common, "A small shrimp."),
                new MuseumItem("oyster", "Oyster", "shellfish", 3043, ItemRarity.Common, "An oyster shell."),
                new MuseumItem("clam", "Clam", "shellfish", 3044, ItemRarity.Common, "A fresh clam."),
                new MuseumItem("nautilus", "Nautilus", "shellfish", 3045, ItemRarity.Rare, "A living fossil."),
                new MuseumItem("giant_squid", "Giant Squid", "shellfish", 3046, ItemRarity.Legendary, "A massive deep-sea squid."),
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
