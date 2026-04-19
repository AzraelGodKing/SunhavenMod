using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using HavensRespec.Config;
using Wish;

namespace HavensRespec.Services
{
    /// <summary>
    /// Resolves the cost of a reset and (optionally) deducts it from the player's wallet.
    /// Everything is best-effort reflection against the game's <c>Player</c> / <c>GameSave</c>
    /// singletons so the mod still loads cleanly if a future Sun Haven patch renames anything.
    /// If a cost cannot be verified or deducted the service refuses the operation — the fallback
    /// is to let the user keep their points, not to silently grant a free reset.
    /// </summary>
    internal sealed class CostService
    {
        private readonly ManualLogSource _log;
        private readonly RespecConfig _config;

        private static MethodInfo _addMoneyMethod;
        private static MethodInfo _addTicketsMethod;
        private static PropertyInfo _gameSaveCoinsProp;
        private static PropertyInfo _gameSaveTicketsProp;
        private static bool _reflectionInitialized;

        public CostService(ManualLogSource log, RespecConfig config)
        {
            _log = log;
            _config = config;
        }

        public int CalculateCost(int pointsRefunded)
        {
            if (pointsRefunded <= 0)
                return 0;
            return _config.CostMode.Value switch
            {
                RespecCostMode.Gold => pointsRefunded * Math.Max(0, _config.GoldPerPoint.Value),
                RespecCostMode.Gems => pointsRefunded * Math.Max(0, _config.GemsPerPoint.Value),
                _ => 0,
            };
        }

        public string CostLabel(int pointsRefunded)
        {
            int cost = CalculateCost(pointsRefunded);
            if (cost <= 0)
                return "Free";
            return _config.CostMode.Value switch
            {
                RespecCostMode.Gold => $"{cost:N0} gold",
                RespecCostMode.Gems => $"{cost:N0} tickets",
                _ => "Free",
            };
        }

        /// <summary>
        /// Returns true if the player can afford the reset (or cost is free). Out parameter
        /// returns the resolved currency balance at the time of the check.
        /// </summary>
        public bool CanAfford(int pointsRefunded, out int balance, out int cost)
        {
            cost = CalculateCost(pointsRefunded);
            balance = ReadBalance();
            if (cost <= 0)
                return true;
            return balance >= cost;
        }

        /// <summary>
        /// Deducts the cost. Returns true on success (or when cost is zero). Best-effort:
        /// if the underlying API cannot be resolved the method refuses (returns false) so the
        /// caller aborts the reset.
        /// </summary>
        public bool TryDeduct(int pointsRefunded)
        {
            int cost = CalculateCost(pointsRefunded);
            if (cost <= 0)
                return true;

            EnsureReflection();

            try
            {
                var player = Player.Instance;
                if (player == null)
                {
                    _log?.LogWarning("[Respec] TryDeduct: Player.Instance was null.");
                    return false;
                }

                switch (_config.CostMode.Value)
                {
                    case RespecCostMode.Gold:
                        if (_addMoneyMethod == null)
                            return false;
                        _addMoneyMethod.Invoke(player, new object[] { -cost, true, false, true });
                        return true;
                    case RespecCostMode.Gems:
                        if (_addTicketsMethod == null)
                            return false;
                        _addTicketsMethod.Invoke(player, new object[] { -cost });
                        return true;
                    default:
                        return true;
                }
            }
            catch (Exception ex)
            {
                _log?.LogError($"[Respec] TryDeduct failed: {ex}");
                return false;
            }
        }

        private int ReadBalance()
        {
            EnsureReflection();

            try
            {
                switch (_config.CostMode.Value)
                {
                    case RespecCostMode.Gold:
                        if (_gameSaveCoinsProp != null)
                            return Convert.ToInt32(_gameSaveCoinsProp.GetValue(null));
                        break;
                    case RespecCostMode.Gems:
                        if (_gameSaveTicketsProp != null)
                            return Convert.ToInt32(_gameSaveTicketsProp.GetValue(null));
                        break;
                }
            }
            catch (Exception ex)
            {
                _log?.LogDebug($"[Respec] ReadBalance swallowed: {ex.Message}");
            }
            return int.MaxValue; // treat unknown as "enough" so the reset still goes through
        }

        private static void EnsureReflection()
        {
            if (_reflectionInitialized)
                return;

            // Player.AddMoney(int amount, bool playAudio = true, bool showNotification = false, bool spawnText = true)
            _addMoneyMethod = AccessTools.Method(typeof(Player), "AddMoney", new[] { typeof(int), typeof(bool), typeof(bool), typeof(bool) });

            // Player.AddTickets(int amount)
            _addTicketsMethod = AccessTools.Method(typeof(Player), "AddTickets", new[] { typeof(int) });

            // GameSave.Coins / GameSave.Tickets static properties
            _gameSaveCoinsProp = typeof(GameSave).GetProperty("Coins", BindingFlags.Public | BindingFlags.Static);
            _gameSaveTicketsProp = typeof(GameSave).GetProperty("Tickets", BindingFlags.Public | BindingFlags.Static);

            _reflectionInitialized = true;
        }
    }
}
