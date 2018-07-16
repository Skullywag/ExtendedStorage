using System.Diagnostics.CodeAnalysis;
using Harmony;
using JetBrains.Annotations;
using RimWorld;

namespace ExtendedStorage.Patches {
    [HarmonyPatch(typeof(StorageSettings), "set_" + nameof(StorageSettings.Priority))]
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class StorageSettings_set_Priority
    {
        public static void Postfix(StorageSettings __instance) {
            (__instance as UserSettings)?.NotifyOwnerSettingsChanged();
        }
    }
}
