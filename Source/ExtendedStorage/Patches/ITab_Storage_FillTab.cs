using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace ExtendedStorage
{
    [HarmonyPatch(typeof(RimWorld.ITab_Storage), "FillTab")]
    public class ITab_Storage_FillTab
    {

        public static bool showStoreSettings = false;

        /// <summary>
        /// Add debug dropdown to toggle displayed settings to tab. Only visible in GodMode.
        /// </summary>
        public static void Postfix(RimWorld.ITab_Storage __instance) {
            if (!DebugSettings.godMode || !(__instance is ITab_Storage))
                return;

            Rect rect = new Rect(160f+10f+5f, 10f, 100f, 29f);

            Text.Font = GameFont.Tiny;
            if (Widgets.ButtonText(rect, $"[Debug] {(showStoreSettings ? "Store" : "User")}", true, false, true)) {
                List<FloatMenuOption> list = new List<FloatMenuOption>
                                             {
                                                 new FloatMenuOption("User", () => { showStoreSettings = false; }),
                                                 new FloatMenuOption("Store", () => { showStoreSettings = true; })
                                             };
                Find.WindowStack.Add(new FloatMenu(list));
            }
        }
    }
}