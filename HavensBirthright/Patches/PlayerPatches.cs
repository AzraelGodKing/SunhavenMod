using HavensBirthright.Session;
using Wish;

namespace HavensBirthright.Patches
{
    /// <summary>
    /// Harmony entry points for player/save lifecycle. Race logic lives in <see cref="RaceDetectionService"/>.
    /// </summary>
    public static class PlayerPatches
    {
        public static void OnPlayerInitialized(Player __instance, bool host)
        {
            Plugin.EnsureRunner();
            BirthrightRunner.ResetAllStateForNewSave();
            RaceDetectionService.DetectFromPlayerInitialized();
        }

        public static void OnPlayerInitialize(Player __instance)
        {
            RaceDetectionService.DetectIfNeeded();
        }

        public static void ResetRaceDetection()
        {
            RaceDetectionService.ResetRaceDetection();
        }

        public static void RetryRaceDetection()
        {
            RaceDetectionService.RetryRaceDetection();
        }

        public static void OnGameSaveLoadCharacter(int characterNumber)
        {
            try
            {
                Plugin.Log.LogInfo($"[RaceDetection] GameSave.LoadCharacter({characterNumber}) — snapshot + resetting Birthright state");
                BirthrightGameSaveContext.RecordLoadCharacter(characterNumber);
                BirthrightRunner.ResetAllStateForNewSave();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"[RaceDetection] OnGameSaveLoadCharacter: {ex.Message}");
            }
        }
    }
}
