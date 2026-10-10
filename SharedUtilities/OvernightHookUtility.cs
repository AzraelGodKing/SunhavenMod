using System;
using System.Reflection;
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
                    if (onDayStartField != null && TryCombineCallback(onDayStartField, null, callback, logWarning))
                    {
                        overnightCallback = callback;
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

                if (!TryCombineCallback(overnightField, uiHandler, callback, logWarning))
                    return false;

                overnightCallback = callback;
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

        /// <summary>
        /// Removes <paramref name="overnightCallback"/> from both DayCycle.OnDayStart and
        /// UIHandler.OnCompleteOvernight so a later re-hook cannot dual-fire morning logic.
        /// Clears <paramref name="overnightHooked"/> and the callback ref on success or best-effort.
        /// </summary>
        public static void TryUnhookOvernightEvent(
            ref bool overnightHooked,
            ref UnityAction overnightCallback,
            Func<Type, object> singletonResolver = null,
            Action<string> logInfo = null,
            Action<string> logWarning = null)
        {
            var callback = overnightCallback;
            try
            {
                if (callback != null)
                {
                    try
                    {
                        var dayCycleType = AccessTools.TypeByName("Wish.DayCycle");
                        var onDayStartField = dayCycleType != null
                            ? AccessTools.Field(dayCycleType, "OnDayStart")
                            : null;
                        if (onDayStartField != null)
                            TryRemoveCallback(onDayStartField, null, callback);
                    }
                    catch (Exception ex)
                    {
                        logWarning?.Invoke($"Failed to unhook DayCycle.OnDayStart: {ex.Message}");
                    }

                    try
                    {
                        var uiHandlerType = AccessTools.TypeByName("Wish.UIHandler");
                        if (uiHandlerType != null)
                        {
                            var uiHandler = singletonResolver?.Invoke(uiHandlerType);
                            var overnightField = AccessTools.Field(uiHandlerType, "OnCompleteOvernight");
                            if (uiHandler != null && overnightField != null)
                                TryRemoveCallback(overnightField, uiHandler, callback);
                        }
                    }
                    catch (Exception ex)
                    {
                        logWarning?.Invoke($"Failed to unhook UIHandler.OnCompleteOvernight: {ex.Message}");
                    }
                }

                logInfo?.Invoke("Overnight hook cleared");
            }
            finally
            {
                overnightHooked = false;
                overnightCallback = null;
            }
        }

        /// <summary>
        /// Appends <paramref name="callback"/> without dropping listeners already on the field.
        /// A failed <c>as UnityAction</c> used to take the null branch and replace the whole
        /// invocation list, which removed another mod's day-start hook (AZR-437).
        /// </summary>
        private static bool TryCombineCallback(FieldInfo field, object instance, UnityAction callback, Action<string> logWarning)
        {
            if (field == null || callback == null)
                return false;

            object raw = field.GetValue(instance);
            Delegate combined;
            if (raw == null)
            {
                combined = callback;
            }
            else if (raw is Delegate existing)
            {
                combined = Delegate.Combine(Delegate.Remove(existing, callback), callback);
            }
            else
            {
                logWarning?.Invoke($"{field.DeclaringType?.Name}.{field.Name} is set but is not a delegate; left unchanged.");
                return false;
            }

            field.SetValue(instance, combined);
            return true;
        }

        private static void TryRemoveCallback(FieldInfo field, object instance, UnityAction callback)
        {
            if (field == null || callback == null)
                return;

            if (field.GetValue(instance) is not Delegate existing)
                return;

            field.SetValue(instance, Delegate.Remove(existing, callback));
        }
    }
}
