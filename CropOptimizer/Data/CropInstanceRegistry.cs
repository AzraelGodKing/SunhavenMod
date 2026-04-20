using System.Collections.Generic;
using UnityEngine;

namespace CropOptimizer.Data
{
    internal static class CropInstanceRegistry
    {
        private static readonly HashSet<int> ActiveCropIds = new HashSet<int>();

        public static void Register(Object cropObject)
        {
            if (cropObject == null)
                return;
            ActiveCropIds.Add(cropObject.GetInstanceID());
        }

        public static void Unregister(Object cropObject)
        {
            if (cropObject == null)
                return;
            ActiveCropIds.Remove(cropObject.GetInstanceID());
        }

        public static void UnregisterById(int instanceId)
        {
            if (instanceId != 0)
                ActiveCropIds.Remove(instanceId);
        }

        public static bool IsKnownActive(int instanceId)
        {
            return ActiveCropIds.Contains(instanceId);
        }
    }
}
