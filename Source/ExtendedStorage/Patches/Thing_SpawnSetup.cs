using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace ExtendedStorage.Patches
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.SpawnSetup))]
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class Thing_SpawnSetup
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction instruction = instructionsList[i];
                yield return instruction;
                if ((instruction.opcode == OpCodes.Ble) &&
                    (instructionsList[i - 1].operand == typeof(ThingDef).GetField(nameof(ThingDef.stackLimit))))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, typeof(Thing_SpawnSetup).GetMethod(nameof(VerifyThingShouldNotBeTruncated)));
                    yield return new CodeInstruction(OpCodes.Brtrue, instruction.operand);
                }
            }
        }

        public static bool VerifyThingShouldNotBeTruncated(Thing t, Map map)
        {
            Building_ExtendedStorage b = map.thingGrid.ThingsListAt(t.Position).FirstOrDefault(o => o is Building_ExtendedStorage) as Building_ExtendedStorage;
            return (b != null) && (b.OutputSlot == t.Position);
        }
    }
}