namespace HavensBirthright.Patches
{
    /// <summary>
    /// Formerly contained health/mana regeneration patches.
    /// All stat modifications (including HealthRegen and ManaRegen) are now handled
    /// by StatPatches.ModifyGetStat which provides a single unified pipeline.
    ///
    /// The duplicate GetStat postfix that was here caused double-stacking of
    /// regen bonuses and contributed to game freezes on save load.
    /// </summary>
    public static class RegenPatches
    {
        // Intentionally empty - all functionality moved to StatPatches.cs
    }
}
