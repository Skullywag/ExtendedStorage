using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace ExtendedStorage.Patches
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.DrawGUIOverlay))]
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class Thing_DrawGUIOverlay
    {
        public static bool Prefix(Thing __instance)
        {
            // supress label draws for stored things
            return !(StorageUtility.GetStoringBuilding(__instance)?.OutputSlot == __instance.Position);
        }
    }
}