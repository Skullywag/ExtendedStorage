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


        /// <summary>
        /// Change the <see cref="StorageSettings.Priority"/> setter implementation to
        /// <code>
        /// set {
        ///     this.priorityInt = value;
        ///     if (Current.ProgramState == ProgramState.Playing) {
        ///         this.TryNotifyChanged();
        ///     }
        /// }
        ///  </code>
        /// </summary>
        /// <remarks>
        /// This is functionally equivalent to the vanilla implementation <em>but</em> allows 
        /// patching though the <see cref="StorageSettings.TryNotifyChanged"/> method.
        /// </remarks>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator ilGen)
        {
            // this.priorityInt = value;
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Stfld, typeof(StorageSettings).GetField("priorityInt", BindingFlags.NonPublic | BindingFlags.Instance));


            // if (Current.ProgramState == ProgramState.Playing) {
            var end = ilGen.DefineLabel();
            yield return new CodeInstruction(OpCodes.Call, typeof(Current).GetProperty(nameof(Current.ProgramState), BindingFlags.Static | BindingFlags.Public).GetGetMethod());
            yield return new CodeInstruction(OpCodes.Ldc_I4, (int) ProgramState.Playing);  
            yield return new CodeInstruction(OpCodes.Bne_Un, end);

            // this.TryNotifyChanged();
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Call, typeof(StorageSettings).GetMethod(StorageSettings_TryNotifyChanged.methodTryNotifyChanged, BindingFlags.NonPublic | BindingFlags.Instance));

            // }
            yield return new CodeInstruction(OpCodes.Ret)
                         {
                             labels = new List<Label> {end}
                         };

        }
    }
}
