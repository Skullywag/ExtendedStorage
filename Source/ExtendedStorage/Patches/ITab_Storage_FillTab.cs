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
        private static readonly PropertyInfo piSelStoreSettingsParent;

        public static bool showStoreSettings = false;

        static ITab_Storage_FillTab()
        {
            // accessor for private field
            piSelStoreSettingsParent = typeof(RimWorld.ITab_Storage)
                .GetProperty("SelStoreSettingsParent", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private static IStoreSettingsParent GetSelStoreSettingsParent(RimWorld.ITab_Storage tab)
        {
            return (IStoreSettingsParent) piSelStoreSettingsParent.GetValue(tab, null);
        }


        /// <summary>
        ///     Wrapper that get's the <see cref="IStoreSettingsParent" /> &amp; <see cref="StorageSettings" /> for
        ///     a <see cref="RimWorld.ITab_Storage" /> <paramref name="tab" />.
        ///     Special cases ExtendedStorage's <see cref="ExtendedStorage.ITab_Storage" /> tabs.
        /// </summary>
        /// <remarks>
        ///     Wrapper has been constructed somewhat convoluted to minimize IL-byte size in invocation.
        ///     A tupled return value (instead of using out parameters) would be cleaner, but that would necessitate
        ///     retrievals from properties/fields afterwards (which would mean more bytes of IL).
        ///     Also the <paramref name="parent" /> parameter is as many <c>out</c> uses as are possible, since
        ///     the <see cref="StorageSettings" /> is actually wrapped inside an anonymous compiler generated type
        ///     (which is inaccessible to us).
        /// </remarks>
        public static StorageSettings GetSettings(RimWorld.ITab_Storage tab, out IStoreSettingsParent parent)
        {
            ITab_Storage extended = tab as ITab_Storage;

            if (extended == null || (DebugSettings.godMode && showStoreSettings))
            {
                parent = GetSelStoreSettingsParent(tab);
                return parent.GetStoreSettings();
            }
            Building_ExtendedStorage building = extended.Building;

            parent = building;
            return building?.userSettings;
        }

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


        /// <remarks>
        ///     Changes the IL code so the patched method beginning is functionally changed from
        ///     <code>
        ///     IStoreSettingsParent selStoreSettingsParent = this.SelStoreSettingsParent;
        ///     StorageSettings settings = selStoreSettingsParent.GetStoreSettings();
        ///     ...
        /// </code>
        ///     to
        ///     <code>
        ///     IStoreSettingsParent selStoreSettingsParent;
        ///     StorageSettings settings = ITab_Storage_FillTab.GetSettings(this, out selStoreSettingsParent);
        ///     ...
        /// </code>
        ///     (with some IL Opcode padding &amp; minor Harmony witchery)
        /// </remarks>
        /// <seealso cref="GetSettings" />
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator ilgen)
        {                                    
            /*  Don't completely replace the base implementation. Only change the absolutely bare minimum.
             *  
             *  For us, this is a different value of the <c>IStoreSettingsParent</c> & <c>StorageSettings</c> local
             *  variable inside the base implementation.
             *  
             *  So use a wrapper (<see cref="GetSettings" />) that incorporates the base retrievel for those locals and 
             *  special cases our type.
             * 
             */

            List<CodeInstruction> instructions = new List<CodeInstruction>(instr);

            ConstructorInfo ciAnonWrapper = (ConstructorInfo) instructions[0].operand;

            var tAnonWrapper = ciAnonWrapper.DeclaringType;
            var fiSettings = tAnonWrapper.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).First(fi => fi.FieldType == typeof(StorageSettings));

            var idxStFld = instructions.FindIndex(2, ci => ci.opcode == OpCodes.Stfld && ci.operand == fiSettings);
            if (idxStFld == -1)
            {
                Log.Warning("Could not find anchor for ITab_Storage.FillTab anchor - not transpiling code.");
                return instructions;
            }

            // remove vanilla instructions
            for (int idx = idxStFld - 1; idx > 1; idx--) {
                instructions.RemoveAt(idx);
            }
            // after removing the intermediate stuff the 'stfld' is now at index #2
            instructions.InsertRange(2,
                                     new[]
                                     {
                                         new CodeInstruction(OpCodes.Ldarg_0), 
                                         new CodeInstruction(OpCodes.Ldloca_S, 1),
                                         new CodeInstruction(OpCodes.Call, typeof(ITab_Storage_FillTab).GetMethod(nameof(GetSettings)))
                                     });

            instructions.Insert(1, new CodeInstruction(OpCodes.Dup));

            return instructions;
        }
    }
}