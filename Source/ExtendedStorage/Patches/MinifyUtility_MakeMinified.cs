using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;

namespace ExtendedStorage {
    [HarmonyPatch(typeof(MinifyUtility), nameof(MinifyUtility.MakeMinified))]
    class MinifyUtility_MakeMinified {

        public static void Prefix(Thing thing) {
            (thing as Building_ExtendedStorage)?.TrySplurgeStoredItems();
        }
    }
}
