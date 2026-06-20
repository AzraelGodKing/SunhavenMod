using System;
using System.IO;
using System.Reflection;
using BepInEx.Bootstrap;

namespace CropOptimizer.Integration
{
    /// <summary>
    /// Optional The Vault integration without a compile-time reference to TheVault.Abstractions
    /// (that assembly is only present when The Vault is installed).
    /// </summary>
    internal static class VaultReflection
    {
        private const string VaultPluginGuid = "com.azraelgodking.thevault";
        private const string BridgeTypeName = "TheVault.Modding.VaultModApiBridge";

        public static bool IsVaultPluginPresent =>
            Chainloader.PluginInfos != null &&
            Chainloader.PluginInfos.ContainsKey(VaultPluginGuid);

        public static Type GetBridgeType()
        {
            if (!IsVaultPluginPresent)
                return null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(BridgeTypeName, throwOnError: false);
                if (type != null)
                    return type;
            }

            if (!Chainloader.PluginInfos.TryGetValue(VaultPluginGuid, out var info) ||
                string.IsNullOrEmpty(info?.Location))
                return null;

            try
            {
                string vaultDir = Path.GetDirectoryName(info.Location);
                if (string.IsNullOrEmpty(vaultDir))
                    return null;

                string abstractionsPath = Path.Combine(vaultDir, "TheVault.Abstractions.dll");
                if (!File.Exists(abstractionsPath))
                    return null;

                var abstractionsAssembly = Assembly.LoadFrom(abstractionsPath);
                return abstractionsAssembly.GetType(BridgeTypeName, throwOnError: false);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[CropOptimizer] Could not load TheVault.Abstractions: {ex.Message}");
                return null;
            }
        }

        public static object GetBridgeInstance(Type bridgeType)
        {
            if (bridgeType == null)
                return null;

            return bridgeType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        }

        public static bool IsVaultReady(object bridgeInstance)
        {
            if (bridgeInstance == null)
                return false;

            return bridgeInstance.GetType()
                       .GetProperty("IsVaultReady", BindingFlags.Public | BindingFlags.Instance)
                       ?.GetValue(bridgeInstance) is bool ready &&
                   ready;
        }

        public static bool TryRegisterCustomCurrency(object bridgeInstance, string id, string displayName, int gameItemId, bool enableAutoDeposit)
        {
            if (bridgeInstance == null)
                return false;

            var method = bridgeInstance.GetType().GetMethod(
                "RegisterCustomCurrency",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(string), typeof(string), typeof(int), typeof(bool) },
                modifiers: null);

            if (method == null)
                return false;

            return method.Invoke(bridgeInstance, new object[] { id, displayName, gameItemId, enableAutoDeposit }) is bool ok && ok;
        }
    }
}
