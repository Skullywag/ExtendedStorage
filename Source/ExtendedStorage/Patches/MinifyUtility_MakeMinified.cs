using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace ExtendedStorage.Patches
{
    [HarmonyPatch(typeof(MinifyUtility), nameof(MinifyUtility.MakeMinified))]
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class MinifyUtility_MakeMinified
    {
        public static void Prefix(Thing thing)
        {
            (thing as Building_ExtendedStorage)?.TrySplurgeStoredItems();
        }
    }
}