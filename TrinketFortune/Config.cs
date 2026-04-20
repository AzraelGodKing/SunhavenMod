using BepInEx.Configuration;

namespace TrinketFortune
{
    public static class Config
    {
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<float> MuseumProgressBonusPercent;
        public static ConfigEntry<float> MinimumMuseumProgress;
        public static ConfigEntry<float> MaxBonusChancePercent;

        public static void Bind(BepInEx.Configuration.ConfigFile config)
        {
            Enabled = config.Bind(
                "General",
                "Enabled",
                true,
                "Enable Trinket Fortune. When enabled, odds of unowned fishing trinkets increase as you complete the aquarium.");

            MuseumProgressBonusPercent = config.Bind(
                "General",
                "MuseumProgressBonusPercent",
                5f,
                new ConfigDescription(
                    "Bonus to unowned trinket/fish odds per 10% aquarium completion (e.g. 5 = +5% per 10%). Applied on top of base drop chance.",
                    new AcceptableValueRange<float>(0f, 50f)));

            MinimumMuseumProgress = config.Bind(
                "General",
                "MinimumMuseumProgress",
                0.2f,
                new ConfigDescription(
                    "Museum progress below this has no bonus (0.2 = 20% donated). Prevents bonus at very low completion.",
                    new AcceptableValueRange<float>(0f, 1f)));

            MaxBonusChancePercent = config.Bind(
                "General",
                "MaxBonusChancePercent",
                75f,
                new ConfigDescription(
                    "Maximum bonus chance cap (%). Clamps the computed bonus regardless of aquarium progress. E.g. 75 = never more than a 75% bonus chance.",
                    new AcceptableValueRange<float>(0f, 100f)));
        }
    }
}
