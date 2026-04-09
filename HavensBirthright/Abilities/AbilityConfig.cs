using BepInEx.Configuration;
using UnityEngine;

namespace HavensBirthright.Abilities
{
    /// <summary>
    /// Configuration entries for all active racial abilities, drawbacks, and synergies.
    /// </summary>
    public static class AbilityConfig
    {
        // ===================== MASTER TOGGLES =====================

        public static ConfigEntry<bool> EnableActiveAbilities;
        public static ConfigEntry<bool> EnableRacialDrawbacks;
        public static ConfigEntry<bool> EnableConditionalSynergies;
        public static ConfigEntry<KeyCode> ActiveAbilityToggleKey;

        // Debug
        public static ConfigEntry<bool> InfernalForgeVerboseLogging;

        // ===================== ACTIVE ABILITIES =====================

        // Water Elemental - Tidal Blessing
        public static ConfigEntry<bool> EnableTidalBlessing;
        public static ConfigEntry<float> TidalBlessingHPCostPercent;
        public static ConfigEntry<float> TidalBlessingHPThreshold;
        public static ConfigEntry<float> TidalBlessingCooldown;
        // Fire Elemental - Infernal Forge
        public static ConfigEntry<bool> EnableInfernalForge;
        public static ConfigEntry<int> InfernalForgeOrePerBar;
        public static ConfigEntry<float> InfernalForgeManaThreshold;
        public static ConfigEntry<float> InfernalForgeScanInterval;

        // Angel - Divine Ward
        public static ConfigEntry<bool> EnableDivineWard;
        public static ConfigEntry<float> DivineWardHPTrigger;
        public static ConfigEntry<float> DivineWardDamageReduction;
        public static ConfigEntry<float> DivineWardDuration;
        public static ConfigEntry<float> DivineWardManaCostPercent;
        public static ConfigEntry<float> DivineWardCooldown;

        // Angel - Font of Light (Celestial)
        public static ConfigEntry<bool> EnableFontOfLight;
        public static ConfigEntry<float> FontOfLightInterval;
        public static ConfigEntry<float> FontOfLightManaPercent;
        public static ConfigEntry<float> FontOfLightManaThreshold;
        public static ConfigEntry<int> FontOfLightGoldCost;

        // Demon - Blood Rage
        public static ConfigEntry<bool> EnableBloodRage;
        public static ConfigEntry<float> BloodRageHPThreshold;
        public static ConfigEntry<float> BloodRageMeleeBonus;
        public static ConfigEntry<float> BloodRageAttackSpeedBonus;

        // Demon - Soul Harvest (Celestial)
        public static ConfigEntry<bool> EnableSoulHarvest;
        public static ConfigEntry<int> SoulHarvestGoldPerKill;
        public static ConfigEntry<float> SoulHarvestHPCostPercent;

        // Elf - Nature's Bounty
        public static ConfigEntry<bool> EnableNaturesBounty;
        public static ConfigEntry<float> NaturesBountyDoubleChance;

        // Human - Quick Learner
        public static ConfigEntry<bool> EnableQuickLearner;
        public static ConfigEntry<int> QuickLearnerSkillThreshold;
        public static ConfigEntry<float> QuickLearnerBonusPerSkill;
        public static ConfigEntry<float> QuickLearnerMaxBonus;

        // Amari Cat - Nine Lives (passive: chance to survive lethal damage and heal to X% HP; not an innate power)
        public static ConfigEntry<bool> EnableNineLives;
        public static ConfigEntry<float> NineLivesChance;
        public static ConfigEntry<float> NineLivesHealPercent;

        // Amari Dog - Loyalty Aura
        public static ConfigEntry<bool> EnableLoyaltyAura;
        public static ConfigEntry<float> LoyaltyAuraBonusPerNPC;
        public static ConfigEntry<float> LoyaltyAuraMaxBonus;

        // Amari Bird - Tailwind
        public static ConfigEntry<bool> EnableTailwind;
        public static ConfigEntry<float> TailwindBonusPerMinute;
        public static ConfigEntry<float> TailwindMaxBonus;

        // Amari Aquatic - Water's Gift
        public static ConfigEntry<bool> EnableWatersGift;
        public static ConfigEntry<float> WatersGiftBonusFishChance;

        // Amari Reptile - Hardened Scales
        public static ConfigEntry<bool> EnableHardenedScales;
        public static ConfigEntry<float> HardenedScalesDefensePerStack;
        public static ConfigEntry<int> HardenedScalesMaxStacks;
        public static ConfigEntry<float> HardenedScalesDecayTime;

        // Naga - Serpent's Grace
        public static ConfigEntry<bool> EnableSerpentGrace;
        public static ConfigEntry<float> SerpentGraceFishingMultiplier;

        // Generic Elemental - Elemental Resonance
        public static ConfigEntry<bool> EnableElementalResonance;
        public static ConfigEntry<float> ElementalResonanceMiningDamageBonus;
        public static ConfigEntry<float> ElementalResonanceMiningSpeedBonus;

        // ===================== DRAWBACKS =====================

        // Fire Elemental
        public static ConfigEntry<float> FireHydrophobicFishingPenalty;

        // Water Elemental
        public static ConfigEntry<float> WaterFragileFormHPPenalty;

        // Angel
        public static ConfigEntry<float> AngelPacifistMeleePenalty;

        // Demon
        public static ConfigEntry<float> DemonDistrustedRelationshipPenalty;

        // Elf
        public static ConfigEntry<float> ElfFragileHPPenalty;
        public static ConfigEntry<float> ElfFragileDefensePenalty;

        // Amari Cat
        public static ConfigEntry<float> CatGlassCannonHPPenalty;

        // Amari Dog
        public static ConfigEntry<float> DogSlowStarterSpeedPenalty;

        // Amari Bird
        public static ConfigEntry<float> BirdHollowBonesHPPenalty;
        public static ConfigEntry<float> BirdHollowBonesDefensePenalty;

        // Amari Aquatic
        public static ConfigEntry<float> AquaticLandSlugSpeedPenalty;

        // Amari Reptile
        public static ConfigEntry<float> ReptileColdBloodedAttackSpeedPenalty;

        // Naga
        public static ConfigEntry<float> NagaLandlockedSpeedPenalty;
        public static ConfigEntry<float> NagaLandlockedFarmingPenalty;

        // ===================== CONDITIONAL SYNERGIES =====================

        // Time-of-day
        public static ConfigEntry<float> AngelSolarPowerBonus;
        public static ConfigEntry<float> DemonNightStalkerBonus;

        // Season
        public static ConfigEntry<float> ElfSpringAwakeningBonus;
        public static ConfigEntry<float> FireSummerFuryBonus;
        public static ConfigEntry<float> WaterWinterEmbraceBonus;

        // Health Threshold (overlaps with active abilities)
        public static ConfigEntry<float> ReptileLastStandDefenseBonus;
        public static ConfigEntry<float> ReptileLastStandHPThreshold;
        public static ConfigEntry<float> AngelMartyrRegenMultiplier;
        public static ConfigEntry<float> AngelMartyrHPThreshold;

        public static void Initialize(ConfigFile config)
        {
            // ===================== MASTER TOGGLES =====================

            EnableActiveAbilities = config.Bind(
                "Active Abilities",
                "EnableActiveAbilities",
                true,
                "Master toggle for all active racial abilities"
            );

            EnableRacialDrawbacks = config.Bind(
                "Drawbacks",
                "EnableRacialDrawbacks",
                false,
                "Master toggle for racial drawbacks (penalties). Disabled by default."
            );

            EnableConditionalSynergies = config.Bind(
                "Synergies",
                "EnableConditionalSynergies",
                true,
                "Master toggle for conditional synergies (time-of-day, season, HP threshold)"
            );

            ActiveAbilityToggleKey = config.Bind(
                "Active Abilities",
                "ToggleKey",
                KeyCode.F9,
                "Key to turn your race's F9-toggleable ability ON or OFF (press any time; flip as often as you like). " +
                "Those abilities start OFF each new session/character until you press this key the first time."
            );

            InfernalForgeVerboseLogging = config.Bind(
                "Debug",
                "InfernalForgeVerboseLogging",
                false,
                "Enable verbose logging for Infernal Forge to debug issues. " +
                "Logs every decision point when ore is picked up."
            );

            // ===================== WATER ELEMENTAL - TIDAL BLESSING =====================

            EnableTidalBlessing = config.Bind(
                "Active Abilities - Water Elemental",
                "EnableTidalBlessing",
                true,
                "Automatically water nearby tilled plots at the cost of HP"
            );

            TidalBlessingHPCostPercent = config.Bind(
                "Active Abilities - Water Elemental",
                "HPCostPercent",
                3f,
                "Percentage of max HP consumed per plot watered"
            );

            TidalBlessingHPThreshold = config.Bind(
                "Active Abilities - Water Elemental",
                "HPSafetyThreshold",
                20f,
                "Won't activate below this HP percentage"
            );

            TidalBlessingCooldown = config.Bind(
                "Active Abilities - Water Elemental",
                "Cooldown",
                2f,
                "Seconds between activations"
            );


            // ===================== FIRE ELEMENTAL - INFERNAL FORGE =====================

            EnableInfernalForge = config.Bind(
                "Active Abilities - Fire Elemental",
                "EnableInfernalForge",
                true,
                "Automatically smelt raw ore into bars when picked up"
            );

            InfernalForgeOrePerBar = config.Bind(
                "Active Abilities - Fire Elemental",
                "OrePerBar",
                3,
                "Number of ore required to produce 1 bar (game default is 3)"
            );

            InfernalForgeManaThreshold = config.Bind(
                "Active Abilities - Fire Elemental",
                "ManaSafetyThreshold",
                10f,
                "Won't activate below this mana percentage"
            );

            InfernalForgeScanInterval = config.Bind(
                "Active Abilities - Fire Elemental",
                "ScanIntervalSeconds",
                60f,
                "Seconds between inventory scans for ore to smelt into bars. " +
                "Default 60s (1 minute) to simulate actual smelting time."
            );

            // ===================== ANGEL - DIVINE WARD =====================

            EnableDivineWard = config.Bind(
                "Active Abilities - Angel",
                "EnableDivineWard",
                true,
                "Auto-activate damage shield when HP drops low"
            );

            DivineWardHPTrigger = config.Bind(
                "Active Abilities - Angel",
                "HPTriggerPercent",
                25f,
                "HP percentage that triggers the ward"
            );

            DivineWardDamageReduction = config.Bind(
                "Active Abilities - Angel",
                "DamageReduction",
                50f,
                "Percentage of damage reduced while ward is active"
            );

            DivineWardDuration = config.Bind(
                "Active Abilities - Angel",
                "Duration",
                8f,
                "Duration of the ward in seconds"
            );

            DivineWardManaCostPercent = config.Bind(
                "Active Abilities - Angel",
                "ManaCostPercent",
                30f,
                "Percentage of max mana consumed on activation"
            );

            DivineWardCooldown = config.Bind(
                "Active Abilities - Angel",
                "Cooldown",
                120f,
                "Cooldown in seconds"
            );

            // ===================== ANGEL - FONT OF LIGHT (Celestial) =====================

            EnableFontOfLight = config.Bind(
                "Active Abilities - Angel",
                "EnableFontOfLight",
                true,
                "Periodic mana restore at the cost of gold (Celestial ability). Only affects Angels."
            );

            FontOfLightInterval = config.Bind(
                "Active Abilities - Angel",
                "FontOfLightInterval",
                45f,
                "Seconds between Font of Light triggers"
            );

            FontOfLightManaPercent = config.Bind(
                "Active Abilities - Angel",
                "FontOfLightManaPercent",
                5f,
                "Percentage of max mana restored each trigger"
            );

            FontOfLightManaThreshold = config.Bind(
                "Active Abilities - Angel",
                "FontOfLightManaThreshold",
                80f,
                "Only trigger when current mana is below this percentage"
            );

            FontOfLightGoldCost = config.Bind(
                "Active Abilities - Angel",
                "FontOfLightGoldCost",
                10,
                "Gold deducted from the player each time Font of Light triggers"
            );

            // ===================== DEMON - BLOOD RAGE =====================

            EnableBloodRage = config.Bind(
                "Active Abilities - Demon",
                "EnableBloodRage",
                true,
                "Gain combat bonuses when HP is low"
            );

            BloodRageHPThreshold = config.Bind(
                "Active Abilities - Demon",
                "HPThreshold",
                30f,
                "HP percentage threshold to activate Blood Rage"
            );

            BloodRageMeleeBonus = config.Bind(
                "Active Abilities - Demon",
                "MeleeDamageBonus",
                25f,
                "Percentage bonus to melee damage while active"
            );

            BloodRageAttackSpeedBonus = config.Bind(
                "Active Abilities - Demon",
                "AttackSpeedBonus",
                15f,
                "Percentage bonus to attack speed while active"
            );

            // ===================== DEMON - SOUL HARVEST (Celestial) =====================

            EnableSoulHarvest = config.Bind(
                "Active Abilities - Demon",
                "EnableSoulHarvest",
                true,
                "Bonus gold when defeating enemies, at the cost of HP per kill (Celestial ability). Only affects Demons."
            );

            SoulHarvestGoldPerKill = config.Bind(
                "Active Abilities - Demon",
                "SoulHarvestGoldPerKill",
                15,
                "Bonus gold granted when you defeat an enemy"
            );

            SoulHarvestHPCostPercent = config.Bind(
                "Active Abilities - Demon",
                "SoulHarvestHPCostPercent",
                1f,
                "Percentage of max HP lost per enemy kill (cost)"
            );

            // ===================== ELF - NATURE'S BOUNTY =====================

            EnableNaturesBounty = config.Bind(
                "Active Abilities - Elf",
                "EnableNaturesBounty",
                true,
                "Chance for double crop harvest"
            );

            NaturesBountyDoubleChance = config.Bind(
                "Active Abilities - Elf",
                "DoubleHarvestChance",
                15f,
                "Percentage chance for double crop yield"
            );

            // ===================== HUMAN - QUICK LEARNER =====================

            EnableQuickLearner = config.Bind(
                "Active Abilities - Human",
                "EnableQuickLearner",
                true,
                "XP bonus scales with leveled skills"
            );

            QuickLearnerSkillThreshold = config.Bind(
                "Active Abilities - Human",
                "SkillLevelThreshold",
                5,
                "Minimum skill level to count as 'leveled'"
            );

            QuickLearnerBonusPerSkill = config.Bind(
                "Active Abilities - Human",
                "BonusPerSkill",
                5f,
                "Percentage XP bonus per qualifying skill"
            );

            QuickLearnerMaxBonus = config.Bind(
                "Active Abilities - Human",
                "MaxBonus",
                25f,
                "Maximum XP bonus from Quick Learner"
            );

            EnableNineLives = config.Bind(
                "Amari Cat",
                "EnableNineLives",
                true,
                "Passive: When taking lethal damage, chance to survive and heal to a percentage of max HP instead of dying"
            );

            NineLivesChance = config.Bind(
                "Amari Cat",
                "NineLivesChance",
                10f,
                "Passive: Percent chance to trigger Nine Lives on lethal damage (max 60%)"
            );

            NineLivesHealPercent = config.Bind(
                "Amari Cat",
                "NineLivesHealPercent",
                50f,
                "Passive: When Nine Lives triggers, heal to this percentage of max HP"
            );

            // ===================== AMARI DOG - LOYALTY AURA =====================

            EnableLoyaltyAura = config.Bind(
                "Active Abilities - Amari Dog",
                "EnableLoyaltyAura",
                true,
                "Stat bonus based on max-friendship NPCs"
            );

            LoyaltyAuraBonusPerNPC = config.Bind(
                "Active Abilities - Amari Dog",
                "BonusPerNPC",
                3f,
                "Percentage stat bonus per max-friendship NPC"
            );

            LoyaltyAuraMaxBonus = config.Bind(
                "Active Abilities - Amari Dog",
                "MaxBonus",
                15f,
                "Maximum stat bonus from Loyalty Aura"
            );

            // ===================== AMARI BIRD - TAILWIND =====================

            EnableTailwind = config.Bind(
                "Active Abilities - Amari Bird",
                "EnableTailwind",
                true,
                "Movement speed increases while outdoors"
            );

            TailwindBonusPerMinute = config.Bind(
                "Active Abilities - Amari Bird",
                "BonusPerMinute",
                2f,
                "Percentage movement speed bonus per minute outdoors"
            );

            TailwindMaxBonus = config.Bind(
                "Active Abilities - Amari Bird",
                "MaxBonus",
                20f,
                "Maximum movement speed bonus from Tailwind"
            );

            // ===================== AMARI AQUATIC - WATER'S GIFT =====================

            EnableWatersGift = config.Bind(
                "Active Abilities - Amari Aquatic",
                "EnableWatersGift",
                true,
                "Chance for bonus fish when catching"
            );

            WatersGiftBonusFishChance = config.Bind(
                "Active Abilities - Amari Aquatic",
                "BonusFishChance",
                10f,
                "Percentage chance to catch a bonus fish"
            );

            // ===================== AMARI REPTILE - HARDENED SCALES =====================

            EnableHardenedScales = config.Bind(
                "Active Abilities - Amari Reptile",
                "EnableHardenedScales",
                true,
                "Taking hits grants stacking defense"
            );

            HardenedScalesDefensePerStack = config.Bind(
                "Active Abilities - Amari Reptile",
                "DefensePerStack",
                2f,
                "Percentage defense bonus per stack"
            );

            HardenedScalesMaxStacks = config.Bind(
                "Active Abilities - Amari Reptile",
                "MaxStacks",
                10,
                "Maximum number of stacks"
            );

            HardenedScalesDecayTime = config.Bind(
                "Active Abilities - Amari Reptile",
                "DecayTime",
                10f,
                "Seconds before stacks reset if not hit"
            );

            // ===================== NAGA - SERPENT'S GRACE =====================

            EnableSerpentGrace = config.Bind(
                "Active Abilities - Naga",
                "EnableSerpentGrace",
                true,
                "Double fishing bonuses during rain or winter"
            );

            SerpentGraceFishingMultiplier = config.Bind(
                "Active Abilities - Naga",
                "FishingMultiplier",
                2f,
                "Multiplier for fishing bonuses during rain/winter"
            );

            // ===================== GENERIC ELEMENTAL - ELEMENTAL RESONANCE =====================

            EnableElementalResonance = config.Bind(
                "Active Abilities - Elemental",
                "EnableElementalResonance",
                true,
                "Mining bonuses while in underground/mine scenes"
            );

            ElementalResonanceMiningDamageBonus = config.Bind(
                "Active Abilities - Elemental",
                "MiningDamageBonus",
                20f,
                "Percentage mining damage bonus in mines"
            );

            ElementalResonanceMiningSpeedBonus = config.Bind(
                "Active Abilities - Elemental",
                "MiningSpeedBonus",
                15f,
                "Percentage mining speed bonus in mines"
            );

            // ===================== DRAWBACKS =====================

            FireHydrophobicFishingPenalty = config.Bind(
                "Drawbacks - Fire Elemental",
                "FishingPenalty",
                20f,
                "Percentage reduction to fishing speed and luck"
            );

            WaterFragileFormHPPenalty = config.Bind(
                "Drawbacks - Water Elemental",
                "MaxHPPenalty",
                10f,
                "Percentage reduction to maximum HP"
            );

            AngelPacifistMeleePenalty = config.Bind(
                "Drawbacks - Angel",
                "MeleeDamagePenalty",
                10f,
                "Percentage reduction to melee damage"
            );

            DemonDistrustedRelationshipPenalty = config.Bind(
                "Drawbacks - Demon",
                "RelationshipPenalty",
                15f,
                "Percentage reduction to relationship gain"
            );

            ElfFragileHPPenalty = config.Bind(
                "Drawbacks - Elf",
                "MaxHPPenalty",
                10f,
                "Percentage reduction to maximum HP"
            );

            ElfFragileDefensePenalty = config.Bind(
                "Drawbacks - Elf",
                "DefensePenalty",
                5f,
                "Percentage reduction to defense"
            );

            CatGlassCannonHPPenalty = config.Bind(
                "Drawbacks - Amari Cat",
                "MaxHPPenalty",
                15f,
                "Percentage reduction to maximum HP"
            );

            DogSlowStarterSpeedPenalty = config.Bind(
                "Drawbacks - Amari Dog",
                "MovementSpeedPenalty",
                10f,
                "Percentage reduction to movement speed"
            );

            BirdHollowBonesHPPenalty = config.Bind(
                "Drawbacks - Amari Bird",
                "MaxHPPenalty",
                15f,
                "Percentage reduction to maximum HP"
            );

            BirdHollowBonesDefensePenalty = config.Bind(
                "Drawbacks - Amari Bird",
                "DefensePenalty",
                10f,
                "Percentage reduction to defense"
            );

            AquaticLandSlugSpeedPenalty = config.Bind(
                "Drawbacks - Amari Aquatic",
                "MovementSpeedPenalty",
                10f,
                "Percentage reduction to movement speed on land"
            );

            ReptileColdBloodedAttackSpeedPenalty = config.Bind(
                "Drawbacks - Amari Reptile",
                "AttackSpeedPenalty",
                10f,
                "Percentage reduction to attack speed"
            );

            NagaLandlockedSpeedPenalty = config.Bind(
                "Drawbacks - Naga",
                "MovementSpeedPenalty",
                10f,
                "Percentage reduction to movement speed"
            );

            NagaLandlockedFarmingPenalty = config.Bind(
                "Drawbacks - Naga",
                "FarmingPenalty",
                10f,
                "Percentage reduction to farming speed"
            );

            // ===================== CONDITIONAL SYNERGIES =====================

            AngelSolarPowerBonus = config.Bind(
                "Synergies - Time of Day",
                "AngelSolarPowerBonus",
                10f,
                "Percentage magic damage bonus for Angels during daytime"
            );

            DemonNightStalkerBonus = config.Bind(
                "Synergies - Time of Day",
                "DemonNightStalkerBonus",
                10f,
                "Percentage melee damage bonus for Demons during nighttime"
            );

            ElfSpringAwakeningBonus = config.Bind(
                "Synergies - Season",
                "ElfSpringAwakeningBonus",
                15f,
                "Percentage farming bonus for Elves during Spring"
            );

            FireSummerFuryBonus = config.Bind(
                "Synergies - Season",
                "FireSummerFuryBonus",
                10f,
                "Percentage combat bonus for Fire Elementals during Summer"
            );

            WaterWinterEmbraceBonus = config.Bind(
                "Synergies - Season",
                "WaterWinterEmbraceBonus",
                15f,
                "Percentage defense/regen bonus for Water Elementals during Winter"
            );

            ReptileLastStandDefenseBonus = config.Bind(
                "Synergies - Health Threshold",
                "ReptileLastStandDefense",
                25f,
                "Percentage defense bonus for Amari Reptile when HP is low"
            );

            ReptileLastStandHPThreshold = config.Bind(
                "Synergies - Health Threshold",
                "ReptileLastStandThreshold",
                40f,
                "HP percentage threshold for Reptile Last Stand"
            );

            AngelMartyrRegenMultiplier = config.Bind(
                "Synergies - Health Threshold",
                "AngelMartyrRegenMultiplier",
                3f,
                "Health regen multiplier for Angels when HP is very low"
            );

            AngelMartyrHPThreshold = config.Bind(
                "Synergies - Health Threshold",
                "AngelMartyrThreshold",
                20f,
                "HP percentage threshold for Angel Martyr's Light"
            );
        }
    }
}
