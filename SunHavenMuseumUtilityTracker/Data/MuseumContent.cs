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

            // Spring Crops Bundle
            var springCropsBundle = new MuseumBundle("spring_crops_bundle", "Spring Crops Bundle", "hall_of_culture", "Crops harvested in spring.");
            springCropsBundle.Items.AddRange(new[]
            {
                new MuseumItem("grapes", "Grapes", "spring_crops_bundle", 11022, ItemRarity.Common, "Fresh grapes."),
                new MuseumItem("wheat", "Wheat", "spring_crops_bundle", 11000, ItemRarity.Common, "Golden wheat."),
                new MuseumItem("tomato", "Tomato", "spring_crops_bundle", 11003, ItemRarity.Common, "A ripe tomato."),
                new MuseumItem("corn", "Corn", "spring_crops_bundle", 11001, ItemRarity.Common, "Fresh corn."),
                new MuseumItem("onion", "Onion", "spring_crops_bundle", 11009, ItemRarity.Common, "A pungent onion."),
                new MuseumItem("potato", "Potato", "spring_crops_bundle", 11002, ItemRarity.Common, "A hearty potato."),
                new MuseumItem("greenroot", "Greenroot", "spring_crops_bundle", 11010, ItemRarity.Common, "A green root vegetable."),
                new MuseumItem("carrot", "Carrot", "spring_crops_bundle", 11006, ItemRarity.Common, "An orange carrot."),
                new MuseumItem("kale", "Kale", "spring_crops_bundle", 11050, ItemRarity.Common, "Leafy kale."),
                new MuseumItem("lettuce", "Lettuce", "spring_crops_bundle", 11013, ItemRarity.Common, "Crisp lettuce."),
                new MuseumItem("cinnaberry", "Cinnaberry", "spring_crops_bundle", 11012, ItemRarity.Uncommon, "A spicy cinnaberry."),
                new MuseumItem("pepper", "Pepper", "spring_crops_bundle", 11008, ItemRarity.Common, "A fresh pepper."),
                new MuseumItem("shimmeroot", "Shimmeroot", "spring_crops_bundle", 11007, ItemRarity.Uncommon, "A shimmering root."),
            });
            hallOfCulture.Bundles.Add(springCropsBundle);

            // Summer Crops Bundle
            var summerCropsBundle = new MuseumBundle("summer_crops_bundle", "Summer Crops Bundle", "hall_of_culture", "Crops harvested in summer.");
            summerCropsBundle.Items.AddRange(new[]
            {
                new MuseumItem("armoranth", "Armoranth", "summer_crops_bundle", 11053, ItemRarity.Uncommon, "A sturdy armoranth."),
                new MuseumItem("guava_berry", "Guava Berry", "summer_crops_bundle", 11035, ItemRarity.Common, "A sweet guava berry."),
                new MuseumItem("beet", "Beet", "summer_crops_bundle", 12080, ItemRarity.Common, "A red beet."),
                new MuseumItem("lemon", "Lemon", "summer_crops_bundle", 11052, ItemRarity.Common, "A sour lemon."),
                new MuseumItem("chocoberry", "Chocoberry", "summer_crops_bundle", 11054, ItemRarity.Uncommon, "A chocolate-flavored berry."),
                new MuseumItem("pineapple", "Pineapple", "summer_crops_bundle", 11056, ItemRarity.Common, "A tropical pineapple."),
                new MuseumItem("melon", "Melon", "summer_crops_bundle", 11057, ItemRarity.Common, "A juicy melon."),
                new MuseumItem("stormelon", "Stormelon", "summer_crops_bundle", 11051, ItemRarity.Uncommon, "An electric stormelon."),
                new MuseumItem("durian", "Durian", "summer_crops_bundle", 11062, ItemRarity.Rare, "A pungent durian."),
            });
            hallOfCulture.Bundles.Add(summerCropsBundle);

            // Fall Crops Bundle
            var fallCropsBundle = new MuseumBundle("fall_crops_bundle", "Fall Crops Bundle", "hall_of_culture", "Crops harvested in fall.");
            fallCropsBundle.Items.AddRange(new[]
            {
                new MuseumItem("garlic", "Garlic", "fall_crops_bundle", 11060, ItemRarity.Common, "Pungent garlic."),
                new MuseumItem("yam", "Yam", "fall_crops_bundle", 11047, ItemRarity.Common, "A hearty yam."),
                new MuseumItem("soda_pop", "Soda Pop", "fall_crops_bundle", 11038, ItemRarity.Uncommon, "A fizzy soda pop crop."),
                new MuseumItem("fizzy_fruit", "Fizzy Fruit", "fall_crops_bundle", 11039, ItemRarity.Uncommon, "A carbonated fruit."),
                new MuseumItem("cranberry", "Cranberry", "fall_crops_bundle", 11070, ItemRarity.Common, "Tart cranberries."),
                new MuseumItem("barley", "Barley", "fall_crops_bundle", 11045, ItemRarity.Common, "Golden barley."),
                new MuseumItem("pumpkin", "Pumpkin", "fall_crops_bundle", 11036, ItemRarity.Common, "A large pumpkin."),
                new MuseumItem("ghost_pepper", "Ghost Pepper", "fall_crops_bundle", 11049, ItemRarity.Rare, "An extremely hot pepper."),
                new MuseumItem("butternut", "Butternut", "fall_crops_bundle", 11044, ItemRarity.Common, "A butternut squash."),
            });
            hallOfCulture.Bundles.Add(fallCropsBundle);

            // Winter Crops Bundle
            var winterCropsBundle = new MuseumBundle("winter_crops_bundle", "Winter Crops Bundle", "hall_of_culture", "Crops harvested in winter.");
            winterCropsBundle.Items.AddRange(new[]
            {
                new MuseumItem("tea_leaves", "Tea Leaves", "winter_crops_bundle", 11048, ItemRarity.Common, "Aromatic tea leaves."),
                new MuseumItem("turnip", "Turnip", "winter_crops_bundle", 11040, ItemRarity.Common, "A fresh turnip."),
                new MuseumItem("purple_eggplant", "Purple Eggplant", "winter_crops_bundle", 12081, ItemRarity.Common, "A purple eggplant."),
                new MuseumItem("heat_fruit", "Heat Fruit", "winter_crops_bundle", 11041, ItemRarity.Uncommon, "A warming fruit."),
                new MuseumItem("marshmallow_bean", "Marshmallow Bean", "winter_crops_bundle", 11042, ItemRarity.Uncommon, "A fluffy bean."),
                new MuseumItem("brr_nana", "Brr-Nana", "winter_crops_bundle", 12082, ItemRarity.Uncommon, "A frozen banana."),
                new MuseumItem("starfruit", "Starfruit", "winter_crops_bundle", 11066, ItemRarity.Rare, "A star-shaped fruit."),
                new MuseumItem("hexagon_berry", "Hexagon Berry", "winter_crops_bundle", 11065, ItemRarity.Rare, "A hexagonal berry."),
                new MuseumItem("snow_pea", "Snow Pea", "winter_crops_bundle", 11078, ItemRarity.Common, "A crisp snow pea."),
                new MuseumItem("snow_ball", "Snow Ball", "winter_crops_bundle", 20019, ItemRarity.Common, "A ball of snow."),
                new MuseumItem("blizzard_berry", "Blizzard Berry", "winter_crops_bundle", 11068, ItemRarity.Rare, "An icy berry."),
                new MuseumItem("balloon_fruit", "Balloon Fruit", "winter_crops_bundle", 11063, ItemRarity.Uncommon, "A floating fruit."),
                new MuseumItem("pythagorean_berry", "Pythagorean Berry", "winter_crops_bundle", 11069, ItemRarity.Rare, "A mathematically perfect berry."),
                new MuseumItem("blue_moon_fruit", "Blue Moon Fruit", "winter_crops_bundle", 11064, ItemRarity.Rare, "A rare lunar fruit."),
                new MuseumItem("candy_cane", "Candy Cane", "winter_crops_bundle", 11077, ItemRarity.Uncommon, "A sweet candy cane."),
            });
            hallOfCulture.Bundles.Add(winterCropsBundle);

            // Nel'Vari Crops Bundle
            var nelvariCropsBundle = new MuseumBundle("nelvari_crops_bundle", "Nel'Vari Crops Bundle", "hall_of_culture", "Crops from Nel'Vari.");
            nelvariCropsBundle.Items.AddRange(new[]
            {
                new MuseumItem("acorn", "Acorn", "nelvari_crops_bundle", 11031, ItemRarity.Common, "A small acorn."),
                new MuseumItem("rock_fruit", "Rock Fruit", "nelvari_crops_bundle", 11025, ItemRarity.Uncommon, "A sturdy rock fruit."),
                new MuseumItem("water_fruit", "Water Fruit", "nelvari_crops_bundle", 11023, ItemRarity.Uncommon, "A watery fruit."),
                new MuseumItem("fire_fruit", "Fire Fruit", "nelvari_crops_bundle", 11024, ItemRarity.Uncommon, "A fiery fruit."),
                new MuseumItem("walk_choy", "Walk Choy", "nelvari_crops_bundle", 11028, ItemRarity.Common, "A walking vegetable."),
                new MuseumItem("wind_chime", "Wind Chime", "nelvari_crops_bundle", 11026, ItemRarity.Uncommon, "A musical plant."),
                new MuseumItem("shiiwalki_mushroom", "Shiiwalki Mushroom", "nelvari_crops_bundle", 11029, ItemRarity.Rare, "A magical mushroom."),
                new MuseumItem("dragon_fruit", "Dragon Fruit", "nelvari_crops_bundle", 11027, ItemRarity.Rare, "A dragon-scaled fruit."),
                new MuseumItem("mana_gem", "Mana Gem", "nelvari_crops_bundle", 11033, ItemRarity.Rare, "A gem-like plant."),
                new MuseumItem("cat_tail", "Cat Tail", "nelvari_crops_bundle", 11032, ItemRarity.Common, "A fluffy cat tail plant."),
                new MuseumItem("indiglow", "Indiglow", "nelvari_crops_bundle", 11030, ItemRarity.Uncommon, "A glowing plant."),
            });
            hallOfCulture.Bundles.Add(nelvariCropsBundle);

            // Withergate Crops Bundle
            var withergateCropsBundle = new MuseumBundle("withergate_crops_bundle", "Withergate Crops Bundle", "hall_of_culture", "Crops from Withergate.");
            withergateCropsBundle.Items.AddRange(new[]
            {
                new MuseumItem("kraken_kale", "Kraken Kale", "withergate_crops_bundle", 11016, ItemRarity.Uncommon, "Tentacled kale."),
                new MuseumItem("tombmelon", "Tombmelon", "withergate_crops_bundle", 11019, ItemRarity.Uncommon, "A spooky melon."),
                new MuseumItem("suckerstem", "Suckerstem", "withergate_crops_bundle", 11018, ItemRarity.Uncommon, "A vampiric plant."),
                new MuseumItem("razorstalk", "Razorstalk", "withergate_crops_bundle", 11020, ItemRarity.Rare, "A sharp plant."),
                new MuseumItem("snappy_plant", "Snappy Plant", "withergate_crops_bundle", 11005, ItemRarity.Uncommon, "A biting plant."),
                new MuseumItem("moonplant", "Moonplant", "withergate_crops_bundle", 11017, ItemRarity.Rare, "A lunar plant."),
                new MuseumItem("eggplant", "Eggplant", "withergate_crops_bundle", 11015, ItemRarity.Common, "A dark eggplant."),
                new MuseumItem("demon_orb", "Demon Orb", "withergate_crops_bundle", 11004, ItemRarity.Rare, "A demonic fruit."),
            });
            hallOfCulture.Bundles.Add(withergateCropsBundle);

            // Flowers Bundle
            var flowersBundle = new MuseumBundle("flowers_bundle", "Flowers Bundle", "hall_of_culture", "Beautiful flowers collection.");
            flowersBundle.Items.AddRange(new[]
            {
                new MuseumItem("honey_flower", "Honey Flower", "flowers_bundle", 11101, ItemRarity.Common, "A sweet honey flower."),
                new MuseumItem("red_rose", "Red Rose", "flowers_bundle", 11107, ItemRarity.Common, "A classic red rose."),
                new MuseumItem("blue_rose", "Blue Rose", "flowers_bundle", 11108, ItemRarity.Uncommon, "A rare blue rose."),
                new MuseumItem("daisy", "Daisy", "flowers_bundle", 11130, ItemRarity.Common, "A cheerful daisy."),
                new MuseumItem("orchid", "Orchid", "flowers_bundle", 11105, ItemRarity.Uncommon, "An elegant orchid."),
                new MuseumItem("tulip", "Tulip", "flowers_bundle", 11109, ItemRarity.Common, "A colorful tulip."),
                new MuseumItem("hibiscus", "Hibiscus", "flowers_bundle", 11103, ItemRarity.Common, "A tropical hibiscus."),
                new MuseumItem("lavender", "Lavender", "flowers_bundle", 11102, ItemRarity.Common, "Fragrant lavender."),
                new MuseumItem("sunflower", "Sunflower", "flowers_bundle", 11106, ItemRarity.Common, "A tall sunflower."),
                new MuseumItem("lily", "Lily", "flowers_bundle", 11104, ItemRarity.Common, "A graceful lily."),
                new MuseumItem("lotus", "Lotus", "flowers_bundle", 11110, ItemRarity.Uncommon, "A serene lotus."),
            });
            hallOfCulture.Bundles.Add(flowersBundle);

            // Foraging Bundle
            var foragingBundle = new MuseumBundle("foraging_bundle", "Foraging Bundle", "hall_of_culture", "Items found while foraging.");
            foragingBundle.Items.AddRange(new[]
            {
                new MuseumItem("log", "Log", "foraging_bundle", 2002, ItemRarity.Common, "A wooden log."),
                new MuseumItem("apple", "Apple", "foraging_bundle", 3044, ItemRarity.Common, "A fresh apple."),
                new MuseumItem("seaweed", "Seaweed", "foraging_bundle", 3002, ItemRarity.Common, "Ocean seaweed."),
                new MuseumItem("blueberry", "Blueberry", "foraging_bundle", 3046, ItemRarity.Common, "Wild blueberries."),
                new MuseumItem("mushroom", "Mushroom", "foraging_bundle", 3001, ItemRarity.Common, "A forest mushroom."),
                new MuseumItem("orange", "Orange", "foraging_bundle", 3045, ItemRarity.Common, "A juicy orange."),
                new MuseumItem("strawberry", "Strawberry", "foraging_bundle", 3047, ItemRarity.Common, "Sweet strawberries."),
                new MuseumItem("berry", "Berry", "foraging_bundle", 16500, ItemRarity.Common, "Wild berries."),
                new MuseumItem("raspberry", "Raspberry", "foraging_bundle", 3049, ItemRarity.Common, "Fresh raspberries."),
                new MuseumItem("peach", "Peach", "foraging_bundle", 3048, ItemRarity.Common, "A ripe peach."),
                new MuseumItem("sand_dollar", "Sand Dollar", "foraging_bundle", 2102, ItemRarity.Uncommon, "A beach sand dollar."),
                new MuseumItem("starfish", "Starfish", "foraging_bundle", 2103, ItemRarity.Uncommon, "A colorful starfish."),
            });
            hallOfCulture.Bundles.Add(foragingBundle);

            // Exploration Bundle
            var explorationBundle = new MuseumBundle("exploration_bundle", "Exploration Bundle", "hall_of_culture", "Treasures found while exploring.");
            explorationBundle.Items.AddRange(new[]
            {
                new MuseumItem("petrified_log", "Petrified Log", "exploration_bundle", 20200, ItemRarity.Rare, "An ancient petrified log."),
                new MuseumItem("phoenix_feather", "Phoenix Feather", "exploration_bundle", 20201, ItemRarity.Epic, "A feather from a phoenix."),
                new MuseumItem("fairy_wings", "Fairy Wings", "exploration_bundle", 20202, ItemRarity.Rare, "Delicate fairy wings."),
                new MuseumItem("griffon_egg", "Griffon Egg", "exploration_bundle", 20203, ItemRarity.Epic, "A griffon's egg."),
                new MuseumItem("mana_sap", "Mana Sap", "exploration_bundle", 20204, ItemRarity.Rare, "Magical tree sap."),
                new MuseumItem("pumice_stone", "Pumice Stone", "exploration_bundle", 20205, ItemRarity.Uncommon, "A volcanic pumice stone."),
                new MuseumItem("mysterious_antler", "Mysterious Antler", "exploration_bundle", 20206, ItemRarity.Rare, "An antler from an unknown creature."),
                new MuseumItem("dragon_fang", "Dragon Fang", "exploration_bundle", 20207, ItemRarity.Epic, "A fang from a dragon."),
                new MuseumItem("monster_candy", "Monster Candy", "exploration_bundle", 20208, ItemRarity.Uncommon, "Candy dropped by monsters."),
                new MuseumItem("unicorn_hair_tuft", "Unicorn Hair Tuft", "exploration_bundle", 20209, ItemRarity.Legendary, "Hair from a unicorn."),
            });
            hallOfCulture.Bundles.Add(explorationBundle);

            // Combat Bundle
            var combatBundle = new MuseumBundle("combat_bundle", "Combat Bundle", "hall_of_culture", "Trophies from combat.");
            combatBundle.Items.AddRange(new[]
            {
                new MuseumItem("leafie_trinket", "Leafie Trinket", "combat_bundle", 20103, ItemRarity.Common, "A trinket from a Leafie."),
                new MuseumItem("elite_leafie_trinket", "Elite Leafie Trinket", "combat_bundle", 20104, ItemRarity.Uncommon, "A trinket from an Elite Leafie."),
                new MuseumItem("centipillar_trinket", "Centapillar Trinket", "combat_bundle", 20105, ItemRarity.Common, "A trinket from a Centapillar."),
                new MuseumItem("peppinch_green_trinket", "Peppinch-Green Trinket", "combat_bundle", 20106, ItemRarity.Common, "A trinket from a Peppinch."),
                new MuseumItem("scorpepper_trinket", "Scorpepper Trinket", "combat_bundle", 20107, ItemRarity.Uncommon, "A trinket from a Scorpepper."),
                new MuseumItem("elite_scorpepper_trinket", "Elite Scorpepper Trinket", "combat_bundle", 20108, ItemRarity.Rare, "A trinket from an Elite Scorpepper."),
                new MuseumItem("hat_crab_trinket", "Hat Crab Trinket", "combat_bundle", 20109, ItemRarity.Uncommon, "A trinket from a Hat Crab."),
                new MuseumItem("floaty_crab_trinket", "Floaty Crab Trinket", "combat_bundle", 20110, ItemRarity.Uncommon, "A trinket from a Floaty Crab."),
                new MuseumItem("bucket_crab_trinket", "Bucket Crab Trinket", "combat_bundle", 20111, ItemRarity.Uncommon, "A trinket from a Bucket Crab."),
                new MuseumItem("umbrella_crab_trinket", "Umbrella Crab Trinket", "combat_bundle", 20112, ItemRarity.Rare, "A trinket from an Umbrella Crab."),
                new MuseumItem("chimchuck_trinket", "Chimchuck Trinket", "combat_bundle", 20113, ItemRarity.Rare, "A trinket from a Chimchuck."),
                new MuseumItem("ancient_sun_haven_sword", "Ancient Sun Haven Sword", "combat_bundle", 20100, ItemRarity.Epic, "An ancient sword from Sun Haven."),
                new MuseumItem("ancient_nelvarian_sword", "Ancient Nel'Varian Sword", "combat_bundle", 20101, ItemRarity.Epic, "An ancient sword from Nel'Vari."),
                new MuseumItem("ancient_withergate_sword", "Ancient Withergate Sword", "combat_bundle", 20102, ItemRarity.Epic, "An ancient sword from Withergate."),
            });
            hallOfCulture.Bundles.Add(combatBundle);

            // Alchemy Bundle
            var alchemyBundle = new MuseumBundle("alchemy_bundle", "Alchemy Bundle", "hall_of_culture", "Potions and elixirs.");
            alchemyBundle.Items.AddRange(new[]
            {
                new MuseumItem("mana_potion", "Mana Potion", "alchemy_bundle", 3080, ItemRarity.Common, "Restores mana."),
                new MuseumItem("health_potion", "Health Potion", "alchemy_bundle", 3081, ItemRarity.Common, "Restores health."),
                new MuseumItem("attack_potion", "Attack Potion", "alchemy_bundle", 3082, ItemRarity.Common, "Boosts attack."),
                new MuseumItem("speed_potion", "Speed Potion", "alchemy_bundle", 3083, ItemRarity.Common, "Increases speed."),
                new MuseumItem("defense_potion", "Defense Potion", "alchemy_bundle", 3084, ItemRarity.Common, "Boosts defense."),
                new MuseumItem("advanced_attack_potion", "Advanced Attack Potion", "alchemy_bundle", 3085, ItemRarity.Uncommon, "Greater attack boost."),
                new MuseumItem("advanced_defense_potion", "Advanced Defense Potion", "alchemy_bundle", 3086, ItemRarity.Uncommon, "Greater defense boost."),
                new MuseumItem("advanced_spell_damage_potion", "Advanced Spell Damage Potion", "alchemy_bundle", 3087, ItemRarity.Uncommon, "Boosts spell damage."),
                new MuseumItem("incredible_spell_damage_potion", "Incredible Spell Damage Potion", "alchemy_bundle", 3766, ItemRarity.Rare, "Massive spell damage boost."),
                new MuseumItem("incredible_attack_potion", "Incredible Attack Potion", "alchemy_bundle", 3767, ItemRarity.Rare, "Massive attack boost."),
                new MuseumItem("incredible_defense_potion", "Incredible Defense Potion", "alchemy_bundle", 3768, ItemRarity.Rare, "Massive defense boost."),
            });
            hallOfCulture.Bundles.Add(alchemyBundle);

            // Nel'Vari Temple Bundle
            var nelvariTempleBundle = new MuseumBundle("nelvari_temple_bundle", "Nel'Vari Temple Bundle", "hall_of_culture", "Ancient books from the Nel'Vari Temple.");
            nelvariTempleBundle.Items.AddRange(new[]
            {
                new MuseumItem("origins_grand_tree_1", "Origins of the Grand Tree - Book I", "nelvari_temple_bundle", 6500, ItemRarity.Rare, "The first volume about the Grand Tree."),
                new MuseumItem("origins_grand_tree_2", "Origins of the Grand Tree - Book II", "nelvari_temple_bundle", 6501, ItemRarity.Rare, "The second volume about the Grand Tree."),
                new MuseumItem("origins_grand_tree_3", "Origins of the Grand Tree - Book III", "nelvari_temple_bundle", 6502, ItemRarity.Rare, "The third volume about the Grand Tree."),
                new MuseumItem("origins_grand_tree_4", "Origins of the Grand Tree - Book IV", "nelvari_temple_bundle", 6503, ItemRarity.Rare, "The fourth volume about the Grand Tree."),
                new MuseumItem("origins_grand_tree_5", "Origins of the Grand Tree - Book V", "nelvari_temple_bundle", 6504, ItemRarity.Rare, "The fifth volume about the Grand Tree."),
                new MuseumItem("origins_sun_haven_1", "Origins of Sun Haven - Book I", "nelvari_temple_bundle", 6505, ItemRarity.Rare, "The first volume about Sun Haven."),
                new MuseumItem("origins_sun_haven_2", "Origins of Sun Haven - Book II", "nelvari_temple_bundle", 6506, ItemRarity.Rare, "The second volume about Sun Haven."),
                new MuseumItem("origins_sun_haven_3", "Origins of Sun Haven - Book III", "nelvari_temple_bundle", 6507, ItemRarity.Rare, "The third volume about Sun Haven."),
                new MuseumItem("origins_sun_haven_4", "Origins of Sun Haven - Book IV", "nelvari_temple_bundle", 6508, ItemRarity.Rare, "The fourth volume about Sun Haven."),
                new MuseumItem("origins_sun_haven_5", "Origins of Sun Haven - Book V", "nelvari_temple_bundle", 6509, ItemRarity.Rare, "The fifth volume about Sun Haven."),
                new MuseumItem("origins_dynus_1", "Origins of Dynus - Book I", "nelvari_temple_bundle", 6510, ItemRarity.Rare, "The first volume about Dynus."),
                new MuseumItem("origins_dynus_2", "Origins of Dynus - Book II", "nelvari_temple_bundle", 6511, ItemRarity.Rare, "The second volume about Dynus."),
                new MuseumItem("origins_dynus_3", "Origins of Dynus - Book III", "nelvari_temple_bundle", 6512, ItemRarity.Rare, "The third volume about Dynus."),
                new MuseumItem("origins_dynus_4", "Origins of Dynus - Book IV", "nelvari_temple_bundle", 6513, ItemRarity.Rare, "The fourth volume about Dynus."),
                new MuseumItem("origins_dynus_5", "Origins of Dynus - Book V", "nelvari_temple_bundle", 6514, ItemRarity.Rare, "The fifth volume about Dynus."),
            });
            hallOfCulture.Bundles.Add(nelvariTempleBundle);

            sections.Add(hallOfCulture);

            // ==================== AQUARIUM ====================
            var aquarium = new MuseumSection("aquarium", "Aquarium", "A collection of fish and aquatic life from all waters.");

            // Fishing Bundle
            var fishingBundle = new MuseumBundle("fishing_bundle", "Fishing Bundle", "aquarium", "Treasures found while fishing.");
            fishingBundle.Items.AddRange(new[]
            {
                new MuseumItem("handmade_bobber", "Handmade Bobber", "fishing_bundle", 20150, ItemRarity.Uncommon, "A handcrafted fishing bobber."),
                new MuseumItem("ancient_magic_staff", "Ancient Magic Staff", "fishing_bundle", 20151, ItemRarity.Epic, "An ancient magical staff."),
                new MuseumItem("bronze_dragon_relic", "Bronze Dragon Relic", "fishing_bundle", 20152, ItemRarity.Legendary, "A relic of an ancient dragon."),
                new MuseumItem("old_sword_hilt", "Old Sword Hilt", "fishing_bundle", 20153, ItemRarity.Rare, "An old sword hilt."),
                new MuseumItem("nelvarian_runestone", "Nel'Varian Runestone", "fishing_bundle", 20154, ItemRarity.Rare, "A runestone from Nel'Vari."),
                new MuseumItem("ancient_elven_headdress", "Ancient Elven Headdress", "fishing_bundle", 20155, ItemRarity.Epic, "An ancient elven headdress."),
                new MuseumItem("old_mayoral_painting", "Old Mayoral Painting", "fishing_bundle", 20156, ItemRarity.Rare, "A painting of an old mayor."),
                new MuseumItem("tentacle_monster_emblem", "Tentacle Monster Emblem", "fishing_bundle", 20157, ItemRarity.Epic, "An emblem of the tentacle monster."),
                new MuseumItem("ancient_angel_quill", "Ancient Angel Quill", "fishing_bundle", 20158, ItemRarity.Epic, "A quill from an ancient angel."),
                new MuseumItem("ancient_naga_crook", "Ancient Naga Crook", "fishing_bundle", 20159, ItemRarity.Epic, "A crook from an ancient naga."),
                new MuseumItem("ancient_amari_totem", "Ancient Amari Totem", "fishing_bundle", 20160, ItemRarity.Legendary, "An ancient Amari totem."),
            });
            aquarium.Bundles.Add(fishingBundle);

            // Spring Fish Tank
            var springFishTank = new MuseumBundle("spring_fish_tank", "Spring Fish Tank", "aquarium", "Fish found in spring.");
            springFishTank.Items.AddRange(new[]
            {
                new MuseumItem("butterflyfish", "Butterflyfish", "spring_fish_tank", 15117, ItemRarity.Common, "A colorful butterflyfish."),
                new MuseumItem("sunfish", "Sunfish", "spring_fish_tank", 15116, ItemRarity.Common, "A sunny sunfish."),
                new MuseumItem("flower_flounder", "Flower Flounder", "spring_fish_tank", 15114, ItemRarity.Uncommon, "A floral flounder."),
                new MuseumItem("raincloud_ray", "Raincloud Ray", "spring_fish_tank", 15118, ItemRarity.Uncommon, "A rainy ray."),
                new MuseumItem("floral_trout", "Floral Trout", "spring_fish_tank", 15119, ItemRarity.Uncommon, "A flowery trout."),
                new MuseumItem("neon_tetra", "Neon Tetra", "spring_fish_tank", 15121, ItemRarity.Common, "A glowing tetra."),
                new MuseumItem("seahorse", "Seahorse", "spring_fish_tank", 15122, ItemRarity.Rare, "A delicate seahorse."),
                new MuseumItem("painted_egg", "Painted Egg", "spring_fish_tank", 15123, ItemRarity.Rare, "A decorated egg."),
                new MuseumItem("tadpole", "Tadpole", "spring_fish_tank", 15124, ItemRarity.Common, "A tiny tadpole."),
            });
            aquarium.Bundles.Add(springFishTank);

            // Summer Fish Tank
            var summerFishTank = new MuseumBundle("summer_fish_tank", "Summer Fish Tank", "aquarium", "Fish found in summer.");
            summerFishTank.Items.AddRange(new[]
            {
                new MuseumItem("blazeel", "Blazeel", "summer_fish_tank", 15104, ItemRarity.Rare, "A fiery eel."),
                new MuseumItem("hearth_angler", "Hearth Angler", "summer_fish_tank", 15106, ItemRarity.Rare, "A warm angler fish."),
                new MuseumItem("scorching_squid", "Scorching Squid", "summer_fish_tank", 15107, ItemRarity.Rare, "A hot squid."),
                new MuseumItem("magma_star", "Magma Star", "summer_fish_tank", 15108, ItemRarity.Epic, "A volcanic starfish."),
                new MuseumItem("tinder_turtle", "Tinder Turtle", "summer_fish_tank", 15109, ItemRarity.Uncommon, "A fiery turtle."),
                new MuseumItem("pyrelus", "Pyrelus", "summer_fish_tank", 15110, ItemRarity.Epic, "A flame fish."),
                new MuseumItem("flame_ray", "Flame Ray", "summer_fish_tank", 15111, ItemRarity.Rare, "A burning ray."),
                new MuseumItem("molten_slug", "Molten Slug", "summer_fish_tank", 15112, ItemRarity.Uncommon, "A lava slug."),
                new MuseumItem("searback", "Searback", "summer_fish_tank", 15113, ItemRarity.Rare, "A scorched fish."),
            });
            aquarium.Bundles.Add(summerFishTank);

            // Fall Fish Tank
            var fallFishTank = new MuseumBundle("fall_fish_tank", "Fall Fish Tank", "aquarium", "Fish found in fall.");
            fallFishTank.Items.AddRange(new[]
            {
                new MuseumItem("coducopia", "Coducopia", "fall_fish_tank", 15125, ItemRarity.Rare, "A bountiful fish."),
                new MuseumItem("king_salmon", "King Salmon", "fall_fish_tank", 15126, ItemRarity.Epic, "A majestic salmon."),
                new MuseumItem("hayfish", "Hayfish", "fall_fish_tank", 15127, ItemRarity.Common, "A harvest fish."),
                new MuseumItem("acorn_anchovy", "Acorn Anchovy", "fall_fish_tank", 15128, ItemRarity.Common, "An autumn anchovy."),
                new MuseumItem("vampire_piranha", "Vampire Piranha", "fall_fish_tank", 15131, ItemRarity.Rare, "A spooky piranha."),
                new MuseumItem("ghostfish", "Ghostfish", "fall_fish_tank", 15132, ItemRarity.Rare, "A spectral fish."),
                new MuseumItem("pumpkin_jelly", "Pumpkin Jelly", "fall_fish_tank", 15133, ItemRarity.Uncommon, "A pumpkin jellyfish."),
                new MuseumItem("pirate_perch", "Pirate Perch", "fall_fish_tank", 15134, ItemRarity.Uncommon, "A swashbuckling perch."),
                new MuseumItem("autumn_leaf_sole", "Autumn Leaf Sole", "fall_fish_tank", 15135, ItemRarity.Uncommon, "A leafy sole."),
            });
            aquarium.Bundles.Add(fallFishTank);

            // Winter Fish Tank
            var winterFishTank = new MuseumBundle("winter_fish_tank", "Winter Fish Tank", "aquarium", "Fish found in winter.");
            winterFishTank.Items.AddRange(new[]
            {
                new MuseumItem("frostfin", "Frostfin", "winter_fish_tank", 15094, ItemRarity.Rare, "An icy fish."),
                new MuseumItem("christmas_lightfish", "Christmas Lightfish", "winter_fish_tank", 15095, ItemRarity.Rare, "A festive fish."),
                new MuseumItem("holly_carp", "Holly Carp", "winter_fish_tank", 15096, ItemRarity.Uncommon, "A holiday carp."),
                new MuseumItem("jingle_bass", "Jingle Bass", "winter_fish_tank", 15097, ItemRarity.Uncommon, "A jingling bass."),
                new MuseumItem("frozen_tuna", "Frozen Tuna", "winter_fish_tank", 15098, ItemRarity.Rare, "A frozen tuna."),
                new MuseumItem("scarffish", "Scarffish", "winter_fish_tank", 15099, ItemRarity.Common, "A cozy fish."),
                new MuseumItem("heatfin", "Heatfin", "winter_fish_tank", 15100, ItemRarity.Uncommon, "A warming fish."),
                new MuseumItem("icicle_carp", "Icicle Carp", "winter_fish_tank", 15101, ItemRarity.Uncommon, "An icy carp."),
                new MuseumItem("blazing_herring", "Blazing Herring", "winter_fish_tank", 15102, ItemRarity.Rare, "A fiery herring."),
            });
            aquarium.Bundles.Add(winterFishTank);

            // Nel'Vari Fish Tank
            var nelvariFishTank = new MuseumBundle("nelvari_fish_tank", "Nel'Vari Fish Tank", "aquarium", "Fish from Nel'Vari waters.");
            nelvariFishTank.Items.AddRange(new[]
            {
                new MuseumItem("robed_parrotfish", "Robed Parrotfish", "nelvari_fish_tank", 15041, ItemRarity.Uncommon, "A robed parrotfish."),
                new MuseumItem("axolotl", "Axolotl", "nelvari_fish_tank", 15055, ItemRarity.Rare, "A cute axolotl."),
                new MuseumItem("frilled_betta", "Frilled Betta", "nelvari_fish_tank", 15054, ItemRarity.Uncommon, "A fancy betta."),
                new MuseumItem("horsefish", "Horsefish", "nelvari_fish_tank", 15053, ItemRarity.Uncommon, "A horse-like fish."),
                new MuseumItem("flamefish", "Flamefish", "nelvari_fish_tank", 15036, ItemRarity.Rare, "A fiery fish."),
                new MuseumItem("dragon_gulper", "Dragon Gulper", "nelvari_fish_tank", 15040, ItemRarity.Epic, "A dragon-like fish."),
                new MuseumItem("neapolitan_fish", "Neapolitan Fish", "nelvari_fish_tank", 15056, ItemRarity.Uncommon, "A tri-colored fish."),
                new MuseumItem("snobfish", "Snobfish", "nelvari_fish_tank", 15042, ItemRarity.Common, "A snooty fish."),
                new MuseumItem("kelp_eel", "Kelp Eel", "nelvari_fish_tank", 15045, ItemRarity.Common, "A seaweed eel."),
                new MuseumItem("princely_frog", "Princely Frog", "nelvari_fish_tank", 15058, ItemRarity.Rare, "A royal frog."),
                new MuseumItem("angelfin", "Angelfin", "nelvari_fish_tank", 15059, ItemRarity.Uncommon, "An angelic fish."),
                new MuseumItem("bubblefish", "Bubblefish", "nelvari_fish_tank", 15060, ItemRarity.Common, "A bubbly fish."),
                new MuseumItem("crystal_tetra", "Crystal Tetra", "nelvari_fish_tank", 15044, ItemRarity.Rare, "A crystalline tetra."),
                new MuseumItem("sky_ray", "Sky Ray", "nelvari_fish_tank", 15046, ItemRarity.Rare, "A celestial ray."),
            });
            aquarium.Bundles.Add(nelvariFishTank);

            // Withergate Fish Tank
            var withergateFishTank = new MuseumBundle("withergate_fish_tank", "Withergate Fish Tank", "aquarium", "Fish from Withergate waters.");
            withergateFishTank.Items.AddRange(new[]
            {
                new MuseumItem("kraken", "Kraken", "withergate_fish_tank", 15070, ItemRarity.Legendary, "A legendary kraken."),
                new MuseumItem("water_bear", "Water Bear", "withergate_fish_tank", 15065, ItemRarity.Rare, "A tiny water bear."),
                new MuseumItem("bonemouth_bass", "Bonemouth Bass", "withergate_fish_tank", 15028, ItemRarity.Rare, "A skeletal bass."),
                new MuseumItem("mummy_trout", "Mummy Trout", "withergate_fish_tank", 15069, ItemRarity.Rare, "A wrapped trout."),
                new MuseumItem("deadeye_shrimp", "Deadeye Shrimp", "withergate_fish_tank", 15033, ItemRarity.Uncommon, "A spooky shrimp."),
                new MuseumItem("electric_eel", "Electric Eel", "withergate_fish_tank", 15066, ItemRarity.Rare, "A shocking eel."),
                new MuseumItem("brain_jelly", "Brain Jelly", "withergate_fish_tank", 15068, ItemRarity.Rare, "A brainy jellyfish."),
                new MuseumItem("redfinned_pincher", "Redfinned Pincher", "withergate_fish_tank", 15067, ItemRarity.Uncommon, "A pinching fish."),
                new MuseumItem("sea_bat", "Sea Bat", "withergate_fish_tank", 15071, ItemRarity.Uncommon, "A batlike fish."),
                new MuseumItem("ghosthead_tuna", "Ghosthead Tuna", "withergate_fish_tank", 15073, ItemRarity.Epic, "A ghostly tuna."),
                new MuseumItem("globfish", "Globfish", "withergate_fish_tank", 15072, ItemRarity.Common, "A blobby fish."),
                new MuseumItem("living_jelly", "Living Jelly", "withergate_fish_tank", 15031, ItemRarity.Uncommon, "An animated jellyfish."),
                new MuseumItem("purrmaid", "Purrmaid", "withergate_fish_tank", 15037, ItemRarity.Rare, "A cat-like mermaid fish."),
                new MuseumItem("slime_leech", "Slime Leech", "withergate_fish_tank", 15035, ItemRarity.Common, "A slimy leech."),
                new MuseumItem("goblin_shark", "Goblin Shark", "withergate_fish_tank", 15074, ItemRarity.Epic, "A goblin-like shark."),
                new MuseumItem("moonfish", "Moonfish", "withergate_fish_tank", 15076, ItemRarity.Rare, "A lunar fish."),
                new MuseumItem("toothy_angler", "Toothy Angler", "withergate_fish_tank", 15030, ItemRarity.Rare, "A toothy angler fish."),
                new MuseumItem("vampire_squid", "Vampire Squid", "withergate_fish_tank", 15075, ItemRarity.Epic, "A vampiric squid."),
                new MuseumItem("viperfish", "Viperfish", "withergate_fish_tank", 15077, ItemRarity.Rare, "A venomous fish."),
                new MuseumItem("albino_squid", "Albino Squid", "withergate_fish_tank", 15079, ItemRarity.Epic, "A pale squid."),
                new MuseumItem("devilfin", "Devilfin", "withergate_fish_tank", 15080, ItemRarity.Rare, "A devilish fish."),
                new MuseumItem("shadow_tuna", "Shadow Tuna", "withergate_fish_tank", 15029, ItemRarity.Epic, "A shadowy tuna."),
            });
            aquarium.Bundles.Add(withergateFishTank);

            // Large Fish Tank
            var largeFishTank = new MuseumBundle("large_fish_tank", "Large Fish Tank", "aquarium", "Common fish from all waters.");
            largeFishTank.Items.AddRange(new[]
            {
                new MuseumItem("pygmy_tuna", "Pygmy Tuna", "large_fish_tank", 15023, ItemRarity.Common, "A small tuna."),
                new MuseumItem("catfish", "Catfish", "large_fish_tank", 15018, ItemRarity.Common, "A whiskered catfish."),
                new MuseumItem("gold_fish", "Gold Fish", "large_fish_tank", 15008, ItemRarity.Common, "A golden fish."),
                new MuseumItem("streamline_cod", "Streamline Cod", "large_fish_tank", 15014, ItemRarity.Common, "A sleek cod."),
                new MuseumItem("salmon", "Salmon", "large_fish_tank", 15085, ItemRarity.Common, "A pink salmon."),
                new MuseumItem("clownfish", "Clownfish", "large_fish_tank", 15083, ItemRarity.Common, "A funny clownfish."),
                new MuseumItem("black_bass", "Black Bass", "large_fish_tank", 15084, ItemRarity.Common, "A dark bass."),
                new MuseumItem("rainbow_trout", "Rainbow Trout", "large_fish_tank", 15004, ItemRarity.Common, "A colorful trout."),
                new MuseumItem("popeye_goldfish", "Popeye Goldfish", "large_fish_tank", 15082, ItemRarity.Uncommon, "A big-eyed goldfish."),
                new MuseumItem("pufferfish", "Pufferfish", "large_fish_tank", 15007, ItemRarity.Uncommon, "A spiny pufferfish."),
                new MuseumItem("ironhead_sturgeon", "Ironhead Sturgeon", "large_fish_tank", 15024, ItemRarity.Rare, "A tough sturgeon."),
                new MuseumItem("cuddlefish", "Cuddlefish", "large_fish_tank", 15022, ItemRarity.Uncommon, "A cuddly cuttlefish."),
                new MuseumItem("lobster", "Lobster", "large_fish_tank", 15088, ItemRarity.Uncommon, "A large lobster."),
                new MuseumItem("silver_carp", "Silver Carp", "large_fish_tank", 15012, ItemRarity.Common, "A silver carp."),
                new MuseumItem("tuna", "Tuna", "large_fish_tank", 15087, ItemRarity.Uncommon, "A mighty tuna."),
                new MuseumItem("blunted_swordfish", "Blunted Swordfish", "large_fish_tank", 15017, ItemRarity.Rare, "A swordfish."),
                new MuseumItem("ribbon_eel", "Ribbon Eel", "large_fish_tank", 15089, ItemRarity.Uncommon, "A ribbon-like eel."),
                new MuseumItem("tiger_trout", "Tiger Trout", "large_fish_tank", 15086, ItemRarity.Uncommon, "A striped trout."),
                new MuseumItem("eel", "Eel", "large_fish_tank", 15002, ItemRarity.Common, "A slippery eel."),
                new MuseumItem("red_snapper", "Red Snapper", "large_fish_tank", 15011, ItemRarity.Uncommon, "A red snapper."),
                new MuseumItem("carp", "Carp", "large_fish_tank", 15010, ItemRarity.Common, "A golden carp."),
                new MuseumItem("redeye_piranha", "Redeye Piranha", "large_fish_tank", 15016, ItemRarity.Uncommon, "A fierce piranha."),
                new MuseumItem("angel_fish", "Angel Fish", "large_fish_tank", 15005, ItemRarity.Common, "An angelic fish."),
                new MuseumItem("whitebelly_shark", "Whitebelly Shark", "large_fish_tank", 15013, ItemRarity.Rare, "A shark."),
                new MuseumItem("koi_fish", "Koi Fish", "large_fish_tank", 15090, ItemRarity.Rare, "A beautiful koi."),
                new MuseumItem("sandstone_fish", "Sandstone Fish", "large_fish_tank", 2118, ItemRarity.Uncommon, "A sandy fish."),
            });
            aquarium.Bundles.Add(largeFishTank);

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
