using System;
using HarmonyLib;
using UnityEngine.Events;

namespace SunhavenMods.Shared
{
    public static class OvernightHookUtility
    {
        public static bool TryHookOvernightEvent(
            ref bool overnightHooked,
            ref UnityAction overnightCallback,
            UnityAction callback,
            Func<Type, object> singletonResolver,
            Action<string> logInfo = null,
            Action<string> logWarning = null)
        {
            if (overnightHooked)
                return true;

            try
            {
                var dayCycleType = AccessTools.TypeByName("Wish.DayCycle");
                if (dayCycleType != null)
                {
                    var onDayStartField = AccessTools.Field(dayCycleType, "OnDayStart");
                    if (onDayStartField != null)
                    {
                        var currentAction = onDayStartField.GetValue(null) as UnityAction;
                        overnightCallback = callback;

                        if (currentAction != null)
                        {
                            // Idempotent attach: remove old instance before adding.
                            currentAction -= overnightCallback;
                            currentAction += overnightCallback;
                            onDayStartField.SetValue(null, currentAction);
                        }
                        else
                        {
                            onDayStartField.SetValue(null, overnightCallback);
                        }

                        overnightHooked = true;
                        logInfo?.Invoke("Hooked into DayCycle.OnDayStart");
                        return true;
                    }
                }

                var uiHandlerType = AccessTools.TypeByName("Wish.UIHandler");
                if (uiHandlerType == null)
                    return false;

                var uiHandler = singletonResolver?.Invoke(uiHandlerType);
                if (uiHandler == null)
                    return false;

                var overnightField = AccessTools.Field(uiHandlerType, "OnCompleteOvernight");
                if (overnightField == null)
                    return false;

                var existingAction = overnightField.GetValue(uiHandler) as UnityAction;
                overnightCallback = callback;

                if (existingAction != null)
                {
                    // Idempotent attach: remove old instance before adding.
                    existingAction -= overnightCallback;
                    existingAction += overnightCallback;
                    overnightField.SetValue(uiHandler, existingAction);
                }
                else
                {
                    overnightField.SetValue(uiHandler, overnightCallback);
                }

                overnightHooked = true;
                logInfo?.Invoke("Hooked into UIHandler.OnCompleteOvernight");
                return true;
            }
            catch (Exception ex)
            {
                logWarning?.Invoke($"Failed to hook overnight event: {ex.Message}");
                return false;
            }
        }
    }
}
