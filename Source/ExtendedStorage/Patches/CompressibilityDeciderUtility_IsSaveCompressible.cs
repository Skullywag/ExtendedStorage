using System.Reflection;
using Harmony;
using RimWorld;
using Verse;

namespace ExtendedStorage {

    [HarmonyPatch(typeof(CompressibilityDeciderUtility), nameof(CompressibilityDeciderUtility.IsSaveCompressible))]
    class CompressibilityDeciderUtility_IsSaveCompressible {

        public static void Postfix(ref bool __result, Thing t)
        {
            __result = __result && !(t.GetSlotGroup()?.parent is Building_ExtendedStorage);
        }
    }
}
