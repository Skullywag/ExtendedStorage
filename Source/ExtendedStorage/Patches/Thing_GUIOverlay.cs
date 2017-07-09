using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Verse;

namespace ExtendedStorage {
    [HarmonyPatch(typeof(Thing), nameof(Thing.DrawGUIOverlay))]
    class Thing_DrawGUIOverlay {

        static bool Prefix(Thing __instance) {
            var b = Find.VisibleMap.thingGrid.ThingAt<Building_ExtendedStorage>(__instance.Position);

            // suppress label draws for allowed things on output stack
            if (b?.OutputSlot == __instance.Position && b?.StoredThingDef == __instance.def) {
                return false;
            }
            return true;
        }
    }
}
