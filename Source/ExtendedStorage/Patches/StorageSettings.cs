using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using RimWorld;
using Verse;

namespace ExtendedStorage {
    [HarmonyPatch(typeof(StorageSettings), methodTryNotifyChanged)]
    class StorageSettings_TryNotifyChanged
    {

        public const string methodTryNotifyChanged = "TryNotifyChanged";

        public static bool Prefix(StorageSettings __instance)
        {
            var us = __instance as UserSettings;

            if (us != null) {
                us.NotifyOwnerSettingsChanged();
                return false;
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(StorageSettings), "set_" + nameof(StorageSettings.Priority))]
    class StorageSettings_set_Priority
    {
        public static void Postfix(StorageSettings __instance) {
            (__instance as UserSettings)?.NotifyOwnerSettingsChanged();
        }
    }
}
