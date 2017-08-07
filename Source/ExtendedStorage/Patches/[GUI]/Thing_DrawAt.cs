using Harmony;
using UnityEngine;
using Verse;

namespace ExtendedStorage
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.DrawAt))]
    internal class Thing_DrawAt
    {
        public static bool Prefix(Thing __instance, Vector3 drawLoc, bool flip)
        {
            return StorageUtility.GetStoringBuilding(__instance) == null || __instance.def.IsApparel;
        }
    }
}