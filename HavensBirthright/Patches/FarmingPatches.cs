namespace HavensBirthright.Patches
{
    /// <summary>
    /// Formerly contained farming/gathering stat patches.
    /// All stat modifications are now handled by StatPatches.ModifyGetStat which provides
    /// a single unified pipeline (base bonuses + abilities + drawbacks + synergies).
    ///
    /// The duplicate GetStat postfix and skill level getter patches that were here
    /// caused triple-stacking of bonuses and game freezes on save load.
    /// </summary>
    public static class FarmingPatches
    {
        // Intentionally empty - all functionality moved to StatPatches.cs
    }
}
