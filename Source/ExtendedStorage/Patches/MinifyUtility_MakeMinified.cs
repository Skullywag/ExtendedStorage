using Harmony;
using RimWorld;
using Verse;

namespace ExtendedStorage
{
    [HarmonyPatch(typeof(MinifyUtility), nameof(MinifyUtility.MakeMinified))]
    internal class MinifyUtility_MakeMinified
    {
        public static void Prefix(Thing thing)
        {
            (thing as Building_ExtendedStorage)?.TrySplurgeStoredItems();
        }
    }
}