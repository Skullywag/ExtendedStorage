using Harmony;
using Verse;

namespace ExtendedStorage {
    [HarmonyPatch(typeof(Thing), nameof(Thing.SplitOff))]
    class Thing_SplitOff {

        public static void Postfix(Thing __instance)
        {
            StorageUtility.GetStoringBuilding(__instance)?.UpdateCachedAttributes();
        }
    }
}
