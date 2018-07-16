using System.Diagnostics.CodeAnalysis;
using Harmony;
using JetBrains.Annotations;
using Verse;

namespace ExtendedStorage.Patches {
    [HarmonyPatch(typeof(Thing), nameof(Thing.SplitOff))]
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class Thing_SplitOff {

        public static void Postfix(Thing __instance)
        {
            StorageUtility.GetStoringBuilding(__instance)?.UpdateCachedAttributes();
        }
    }
}
