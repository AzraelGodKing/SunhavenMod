namespace HavensBirthright
{
    /// <summary>
    /// All playable races in Sun Haven
    /// </summary>
    public enum Race
    {
        Human,
        Elf,
        Angel,
        Demon,
        Elemental,      // Base type (will check variant for Fire/Water)
        FireElemental,  // Fire variant
        WaterElemental, // Water variant
        Amari,          // Base type (will check variant for Cat/Dog/Bird/Aquatic/Reptile)
        AmariCat,       // Cat variant
        AmariDog,       // Dog variant
        AmariBird,      // Bird variant
        AmariAquatic,   // Aquatic variant
        AmariReptile,   // Reptile variant
        Naga
    }

    /// <summary>
    /// Elemental variant types
    /// </summary>
    public enum ElementalVariant
    {
        None,
        Fire,
        Water
    }

    /// <summary>
    /// Amari variant types
    /// </summary>
    public enum AmariVariant
    {
        None,
        Cat,
        Dog,
        Bird,
        Aquatic,
        Reptile
    }

    /// <summary>
    /// Types of bonuses that can be applied to races
    /// </summary>
    public enum BonusType
    {
        // Combat bonuses
        MeleeStrength,
        MagicPower,
        Defense,
        CriticalChance,
        AttackSpeed,

        // Farming bonuses
        FarmingSpeed,
        CropQuality,
        CropYield,
        WateringEfficiency,

        // Gathering bonuses
        MiningSpeed,
        MiningYield,
        WoodcuttingSpeed,
        WoodcuttingYield,
        FishingSpeed,
        FishingLuck,
        ForagingChance,

        // Crafting bonuses
        CraftingSpeed,
        CraftingQuality,

        // Social bonuses
        RelationshipGain,
        ShopDiscount,

        // Misc bonuses
        MovementSpeed,
        MaxHealth,
        MaxMana,
        ManaRegen,
        HealthRegen,
        ExperienceGain,
        GoldFind,
        LuckBonus
    }

    /// <summary>
    /// Represents a single racial bonus
    /// </summary>
    public class RacialBonus
    {
        public BonusType Type { get; set; }
        public float Value { get; set; }
        public bool IsPercentage { get; set; }
        public string Description { get; set; }

        public RacialBonus(BonusType type, float value, bool isPercentage, string description)
        {
            Type = type;
            Value = value;
            IsPercentage = isPercentage;
            Description = description;
        }

        public string GetFormattedValue()
        {
            if (IsPercentage)
            {
                return Value >= 0 ? $"+{Value}%" : $"{Value}%";
            }
            return Value >= 0 ? $"+{Value}" : $"{Value}";
        }
    }
}
