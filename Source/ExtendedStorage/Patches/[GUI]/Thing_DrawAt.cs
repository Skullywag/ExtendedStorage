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
            Graphic g;
            if (StorageUtility.HasSupressedOrSubstituedGraphics(__instance, out g))
            {
                // substitute draws for stored things
                g?.DrawWorker(
                    drawLoc,
                    flip ? __instance.Rotation.Opposite : __instance.Rotation,
                    __instance.def,
                    __instance);
                return false;
            }

            return true;
        }
    }
}