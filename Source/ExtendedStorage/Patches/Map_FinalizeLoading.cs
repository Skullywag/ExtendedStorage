using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using Verse;

namespace ExtendedStorage.Patches {
    /// <summary>
    /// prioritize ES building spawn so genspawn.spawn splurge suppression patch can return a valid discriminator    
    /// </summary>
    /// <seealso cref="GenSpawn_Spawn.ShouldDisplaceOtherItems"/>
    [HarmonyPatch(typeof(Map), nameof(Map.FinalizeLoading))]
    class Map_FinalizeLoading
    {
        private static MethodInfo mi = typeof(BackCompatibility).GetMethod(nameof(BackCompatibility.PreCheckSpawnBackCompatibleThingAfterLoading));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr) {
            List<CodeInstruction> instructions = new List<CodeInstruction>(instr);

            var idxAnchor = instructions.FindIndex(ci => ci.opcode == OpCodes.Call && ci.operand == mi);
            if (idxAnchor == -1) {
                Log.Warning("Could not find Map_FinalizeLoading transpiler anchor - not patching.");
                return instructions;
            }

            instructions.InsertRange(idxAnchor +1,
                new []
                {
                    new CodeInstruction(OpCodes.Ldloc_1),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, typeof(Map_FinalizeLoading).GetMethod(nameof(PrioritySpawnStorageBuildings)))
                });

            return instructions;

        }

        public static void PrioritySpawnStorageBuildings(List<Thing> things, Map map)
        {
            var candidates = things.OfType<Building_ExtendedStorage>().ToArray();

            foreach (Building_ExtendedStorage current in candidates)
            {
                try
                {
                    GenSpawn.SpawnBuildingAsPossible(current, map, true);
                }
                catch (Exception ex2)
                {
                    Log.Error(string.Concat(new object[]
                                            {
                                                "Exception spawning loaded thing ",
                                                current.ToStringSafe<Building>(),
                                                ": ",
                                                ex2
                                            }), false);
                }

                things.Remove(current);
            }
        }
    }
}
