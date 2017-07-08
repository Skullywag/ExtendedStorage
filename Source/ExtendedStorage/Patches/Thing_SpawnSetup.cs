using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse.AI;
using Verse;
using System.Reflection;
using Harmony;
using System.Reflection.Emit;

namespace ExtendedStorage
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.SpawnSetup))]
    class Thing_SpawnSetup
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                var instruction = instructionsList[i];
                yield return instruction;
                if (instruction.opcode == OpCodes.Ble &&
                    instructionsList[i - 1].operand == typeof(ThingDef).GetField(nameof(ThingDef.stackLimit)))
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
            var b = map.thingGrid.ThingsListAt(t.Position).FirstOrDefault(o => o is Building_ExtendedStorage) as Building_ExtendedStorage;
            return b != null && b.OutputSlot == t.Position;
        }
    }
}
