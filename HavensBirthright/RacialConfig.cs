using BepInEx.Configuration;

namespace HavensBirthright
{
    /// <summary>
    /// Configuration for all racial bonuses - allows players to customize values
    /// </summary>
    public static class RacialConfig
    {
        // Human bonuses
        public static ConfigEntry<float> HumanExpBonus;
        public static ConfigEntry<float> HumanRelationshipBonus;
        public static ConfigEntry<float> HumanShopDiscount;

        // Elf bonuses
        public static ConfigEntry<float> ElfFarmingBonus;
        public static ConfigEntry<float> ElfCropQualityBonus;
        public static ConfigEntry<float> ElfForagingBonus;
        public static ConfigEntry<float> ElfManaRegenBonus;

        // Angel bonuses
        public static ConfigEntry<float> AngelMaxManaBonus;
        public static ConfigEntry<float> AngelMagicBonus;
        public static ConfigEntry<float> AngelHealthRegenBonus;
        public static ConfigEntry<float> AngelLuckBonus;

        // Demon bonuses
        public static ConfigEntry<float> DemonMeleeBonus;
        public static ConfigEntry<float> DemonCritBonus;
        public static ConfigEntry<float> DemonHealthBonus;
        public static ConfigEntry<float> DemonGoldBonus;

        // Fire Elemental bonuses
        public static ConfigEntry<float> FireElementalMeleeBonus;
        public static ConfigEntry<float> FireElementalMagicBonus;
        public static ConfigEntry<float> FireElementalAttackSpeedBonus;
        public static ConfigEntry<float> FireElementalCritBonus;

        // Water Elemental bonuses
        public static ConfigEntry<float> WaterElementalDefenseBonus;
        public static ConfigEntry<float> WaterElementalHealthRegenBonus;
        public static ConfigEntry<float> WaterElementalManaRegenBonus;
        public static ConfigEntry<float> WaterElementalFishingBonus;

        // Generic Elemental bonuses (fallback)
        public static ConfigEntry<float> ElementalMiningSpeedBonus;
        public static ConfigEntry<float> ElementalMiningYieldBonus;
        public static ConfigEntry<float> ElementalMagicBonus;
        public static ConfigEntry<float> ElementalDefenseBonus;

        // Amari bonuses (generic fallback)
        public static ConfigEntry<float> AmariSpeedBonus;
        public static ConfigEntry<float> AmariAttackSpeedBonus;
        public static ConfigEntry<float> AmariCraftingBonus;
        public static ConfigEntry<float> AmariWoodcuttingBonus;

        // Amari Cat bonuses
        public static ConfigEntry<float> AmariCatSpeedBonus;
        public static ConfigEntry<float> AmariCatAttackSpeedBonus;
        public static ConfigEntry<float> AmariCatCritBonus;
        public static ConfigEntry<float> AmariCatDodgeBonus;

        // Amari Dog bonuses
        public static ConfigEntry<float> AmariDogHealthBonus;
        public static ConfigEntry<float> AmariDogDefenseBonus;
        public static ConfigEntry<float> AmariDogRelationshipBonus;
        public static ConfigEntry<float> AmariDogExpBonus;

        // Amari Bird bonuses
        public static ConfigEntry<float> AmariBirdSpeedBonus;
        public static ConfigEntry<float> AmariBirdForagingBonus;
        public static ConfigEntry<float> AmariBirdManaRegenBonus;
        public static ConfigEntry<float> AmariBirdDodgeBonus;

        // Amari Aquatic bonuses
        public static ConfigEntry<float> AmariAquaticFishingSpeedBonus;
        public static ConfigEntry<float> AmariAquaticFishingLuckBonus;
        public static ConfigEntry<float> AmariAquaticManaRegenBonus;
        public static ConfigEntry<float> AmariAquaticHealthRegenBonus;

        // Amari Reptile bonuses
        public static ConfigEntry<float> AmariReptileDefenseBonus;
        public static ConfigEntry<float> AmariReptileMeleeBonus;
        public static ConfigEntry<float> AmariReptileHealthBonus;
        public static ConfigEntry<float> AmariReptileMiningBonus;

        // Naga bonuses
        public static ConfigEntry<float> NagaFishingSpeedBonus;
        public static ConfigEntry<float> NagaFishingLuckBonus;
        public static ConfigEntry<float> NagaDefenseBonus;
        public static ConfigEntry<float> NagaManaRegenBonus;

        // General settings
        public static ConfigEntry<bool> EnableRacialBonuses;
        public static ConfigEntry<bool> ShowBonusNotifications;

        public static void Initialize(ConfigFile config)
        {
            // General settings
            EnableRacialBonuses = config.Bind(
                "General",
                "EnableRacialBonuses",
                true,
                "Enable or disable all racial bonuses"
            );

            ShowBonusNotifications = config.Bind(
                "General",
                "ShowBonusNotifications",
                true,
                "Show notifications when racial bonuses are applied"
            );

            // Human bonuses (default: modest all-around bonuses)
            HumanExpBonus = config.Bind(
                "Human",
                "ExperienceBonus",
                10f,
                "Percentage bonus to experience gain"
            );

            HumanRelationshipBonus = config.Bind(
                "Human",
                "RelationshipBonus",
                15f,
                "Percentage bonus to relationship point gain"
            );

            HumanShopDiscount = config.Bind(
                "Human",
                "ShopDiscount",
                5f,
                "Percentage discount at shops"
            );

            // Elf bonuses (default: nature/farming focused)
            ElfFarmingBonus = config.Bind(
                "Elf",
                "FarmingSpeedBonus",
                15f,
                "Percentage bonus to farming speed"
            );

            ElfCropQualityBonus = config.Bind(
                "Elf",
                "CropQualityBonus",
                20f,
                "Percentage bonus to crop quality chance"
            );

            ElfForagingBonus = config.Bind(
                "Elf",
                "ForagingBonus",
                25f,
                "Percentage bonus to foraging find chance"
            );

            ElfManaRegenBonus = config.Bind(
                "Elf",
                "ManaRegenBonus",
                15f,
                "Percentage bonus to mana regeneration"
            );

            // Angel bonuses (default: magic/healing focused)
            AngelMaxManaBonus = config.Bind(
                "Angel",
                "MaxManaBonus",
                20f,
                "Percentage bonus to maximum mana"
            );

            AngelMagicBonus = config.Bind(
                "Angel",
                "MagicPowerBonus",
                15f,
                "Percentage bonus to magic damage"
            );

            AngelHealthRegenBonus = config.Bind(
                "Angel",
                "HealthRegenBonus",
                25f,
                "Percentage bonus to health regeneration"
            );

            AngelLuckBonus = config.Bind(
                "Angel",
                "LuckBonus",
                10f,
                "Percentage bonus to luck"
            );

            // Demon bonuses (default: combat focused)
            DemonMeleeBonus = config.Bind(
                "Demon",
                "MeleeDamageBonus",
                20f,
                "Percentage bonus to melee damage"
            );

            DemonCritBonus = config.Bind(
                "Demon",
                "CriticalChanceBonus",
                15f,
                "Percentage bonus to critical hit chance"
            );

            DemonHealthBonus = config.Bind(
                "Demon",
                "MaxHealthBonus",
                15f,
                "Percentage bonus to maximum health"
            );

            DemonGoldBonus = config.Bind(
                "Demon",
                "GoldFindBonus",
                20f,
                "Percentage bonus to gold drops"
            );

            // Fire Elemental bonuses (default: offensive/damage focused)
            FireElementalMeleeBonus = config.Bind(
                "Fire Elemental",
                "MeleeDamageBonus",
                15f,
                "Percentage bonus to melee damage"
            );

            FireElementalMagicBonus = config.Bind(
                "Fire Elemental",
                "MagicPowerBonus",
                20f,
                "Percentage bonus to magic damage"
            );

            FireElementalAttackSpeedBonus = config.Bind(
                "Fire Elemental",
                "AttackSpeedBonus",
                10f,
                "Percentage bonus to attack speed"
            );

            FireElementalCritBonus = config.Bind(
                "Fire Elemental",
                "CriticalChanceBonus",
                15f,
                "Percentage bonus to critical hit chance"
            );

            // Water Elemental bonuses (default: defensive/utility focused)
            WaterElementalDefenseBonus = config.Bind(
                "Water Elemental",
                "DefenseBonus",
                20f,
                "Percentage bonus to defense"
            );

            WaterElementalHealthRegenBonus = config.Bind(
                "Water Elemental",
                "HealthRegenBonus",
                20f,
                "Percentage bonus to health regeneration"
            );

            WaterElementalManaRegenBonus = config.Bind(
                "Water Elemental",
                "ManaRegenBonus",
                25f,
                "Percentage bonus to mana regeneration"
            );

            WaterElementalFishingBonus = config.Bind(
                "Water Elemental",
                "FishingLuckBonus",
                20f,
                "Percentage bonus to fishing luck"
            );

            // Generic Elemental bonuses (fallback if variant unknown)
            ElementalMiningSpeedBonus = config.Bind(
                "Elemental (Generic)",
                "MiningSpeedBonus",
                20f,
                "Percentage bonus to mining speed (used if Fire/Water variant cannot be detected)"
            );

            ElementalMiningYieldBonus = config.Bind(
                "Elemental (Generic)",
                "MiningYieldBonus",
                15f,
                "Percentage bonus to mining yield"
            );

            ElementalMagicBonus = config.Bind(
                "Elemental (Generic)",
                "MagicPowerBonus",
                10f,
                "Percentage bonus to magic damage"
            );

            ElementalDefenseBonus = config.Bind(
                "Elemental (Generic)",
                "DefenseBonus",
                15f,
                "Percentage bonus to defense"
            );

            // Amari bonuses (generic fallback if variant unknown)
            AmariSpeedBonus = config.Bind(
                "Amari (Generic)",
                "MovementSpeedBonus",
                15f,
                "Percentage bonus to movement speed (used if variant cannot be detected)"
            );

            AmariAttackSpeedBonus = config.Bind(
                "Amari (Generic)",
                "AttackSpeedBonus",
                15f,
                "Percentage bonus to attack speed"
            );

            AmariCraftingBonus = config.Bind(
                "Amari (Generic)",
                "CraftingSpeedBonus",
                20f,
                "Percentage bonus to crafting speed"
            );

            AmariWoodcuttingBonus = config.Bind(
                "Amari (Generic)",
                "WoodcuttingSpeedBonus",
                15f,
                "Percentage bonus to woodcutting speed"
            );

            // Amari Cat bonuses (agile hunters - speed and crits)
            AmariCatSpeedBonus = config.Bind(
                "Amari Cat",
                "MovementSpeedBonus",
                20f,
                "Percentage bonus to movement speed"
            );

            AmariCatAttackSpeedBonus = config.Bind(
                "Amari Cat",
                "AttackSpeedBonus",
                20f,
                "Percentage bonus to attack speed"
            );

            AmariCatCritBonus = config.Bind(
                "Amari Cat",
                "CriticalChanceBonus",
                15f,
                "Percentage bonus to critical hit chance"
            );

            AmariCatDodgeBonus = config.Bind(
                "Amari Cat",
                "DodgeChanceBonus",
                10f,
                "Percentage bonus to dodge chance"
            );

            // Amari Dog bonuses (loyal companions - tanky and social)
            AmariDogHealthBonus = config.Bind(
                "Amari Dog",
                "MaxHealthBonus",
                20f,
                "Percentage bonus to maximum health"
            );

            AmariDogDefenseBonus = config.Bind(
                "Amari Dog",
                "DefenseBonus",
                15f,
                "Percentage bonus to defense"
            );

            AmariDogRelationshipBonus = config.Bind(
                "Amari Dog",
                "RelationshipBonus",
                25f,
                "Percentage bonus to relationship point gain"
            );

            AmariDogExpBonus = config.Bind(
                "Amari Dog",
                "ExperienceBonus",
                10f,
                "Percentage bonus to experience gain"
            );

            // Amari Bird bonuses (free spirits - mobility and foraging)
            AmariBirdSpeedBonus = config.Bind(
                "Amari Bird",
                "MovementSpeedBonus",
                25f,
                "Percentage bonus to movement speed"
            );

            AmariBirdForagingBonus = config.Bind(
                "Amari Bird",
                "ForagingBonus",
                25f,
                "Percentage bonus to foraging find chance"
            );

            AmariBirdManaRegenBonus = config.Bind(
                "Amari Bird",
                "ManaRegenBonus",
                15f,
                "Percentage bonus to mana regeneration"
            );

            AmariBirdDodgeBonus = config.Bind(
                "Amari Bird",
                "DodgeChanceBonus",
                15f,
                "Percentage bonus to dodge chance"
            );

            // Amari Aquatic bonuses (water dwellers - fishing masters)
            AmariAquaticFishingSpeedBonus = config.Bind(
                "Amari Aquatic",
                "FishingSpeedBonus",
                25f,
                "Percentage bonus to fishing speed"
            );

            AmariAquaticFishingLuckBonus = config.Bind(
                "Amari Aquatic",
                "FishingLuckBonus",
                25f,
                "Percentage bonus to fishing luck"
            );

            AmariAquaticManaRegenBonus = config.Bind(
                "Amari Aquatic",
                "ManaRegenBonus",
                15f,
                "Percentage bonus to mana regeneration"
            );

            AmariAquaticHealthRegenBonus = config.Bind(
                "Amari Aquatic",
                "HealthRegenBonus",
                15f,
                "Percentage bonus to health regeneration"
            );

            // Amari Reptile bonuses (tough survivors - defense and mining)
            AmariReptileDefenseBonus = config.Bind(
                "Amari Reptile",
                "DefenseBonus",
                25f,
                "Percentage bonus to defense"
            );

            AmariReptileMeleeBonus = config.Bind(
                "Amari Reptile",
                "MeleeDamageBonus",
                15f,
                "Percentage bonus to melee damage"
            );

            AmariReptileHealthBonus = config.Bind(
                "Amari Reptile",
                "MaxHealthBonus",
                15f,
                "Percentage bonus to maximum health"
            );

            AmariReptileMiningBonus = config.Bind(
                "Amari Reptile",
                "MiningSpeedBonus",
                20f,
                "Percentage bonus to mining speed"
            );

            // Naga bonuses (default: fishing/water focused)
            NagaFishingSpeedBonus = config.Bind(
                "Naga",
                "FishingSpeedBonus",
                25f,
                "Percentage bonus to fishing speed"
            );

            NagaFishingLuckBonus = config.Bind(
                "Naga",
                "FishingLuckBonus",
                20f,
                "Percentage bonus to fishing luck"
            );

            NagaDefenseBonus = config.Bind(
                "Naga",
                "DefenseBonus",
                10f,
                "Percentage bonus to defense"
            );

            NagaManaRegenBonus = config.Bind(
                "Naga",
                "ManaRegenBonus",
                15f,
                "Percentage bonus to mana regeneration"
            );

            Plugin.Log.LogInfo("Configuration initialized");
        }
    }
}
