using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using RimWorld;
using Verse;

namespace ExtendedStorage {
    [HarmonyPatch(typeof(StorageSettings), "set_" + nameof(StorageSettings.Priority))]
    class StorageSettings_set_Priority
    {
        public static void Postfix(StorageSettings __instance) {
            (__instance as UserSettings)?.NotifyOwnerSettingsChanged();
        }
    }
}
