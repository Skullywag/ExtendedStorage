using Harmony;
using Verse;

namespace ExtendedStorage
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.DrawGUIOverlay))]
    internal class Thing_DrawGUIOverlay
    {
        public static bool Prefix(Thing __instance)
        {
            // supress label draws for stored things
            return !(StorageUtility.GetStoringBuilding(__instance)?.OutputSlot == __instance.Position);
        }
    }
}