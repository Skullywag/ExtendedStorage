using System.Diagnostics.CodeAnalysis;
using Harmony;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace ExtendedStorage.Patches {

    [HarmonyPatch(typeof(CompressibilityDeciderUtility), nameof(CompressibilityDeciderUtility.IsSaveCompressible))]
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class CompressibilityDeciderUtility_IsSaveCompressible {

        public static void Postfix(ref bool __result, Thing t)
        {
            __result = __result && !(t.GetSlotGroup()?.parent is Building_ExtendedStorage);
        }
    }
}
