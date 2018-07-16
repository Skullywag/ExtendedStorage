using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Harmony;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ExtendedStorage.Patches
{
    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class FloatMenuMakerMap_AddHumanlikeOrders
    {
        /// <remarks>
        ///     Special case ClothingRack - add in 'Equip XYZ' options for all stored elements, not just the first
        /// </remarks>
        public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            IntVec3 c = IntVec3.FromVector3(clickPos);

            Building_ExtendedStorage storage = pawn.Map.thingGrid.ThingAt<Building_ExtendedStorage>(c);
            if (storage?.def.defName == @"Storage_Locker")
            {
                List<Apparel> apparels = pawn.Map.thingGrid.ThingsAt(c).OfType<Apparel>().ToList();

                if (apparels.Count > 1)
                {
                    FloatMenuOption baseOption = CreateMenuOption(pawn, apparels[0]);
                    int baseIndex = opts.FirstIndexOf(mo => mo.Label == baseOption.Label); // maybe this is hinky.... can this ever get the wrong option if comparing just by label???

                    IEnumerable<FloatMenuOption> extraOptions = apparels.Skip(1).Select(a => CreateMenuOption(pawn, a));

                    if (baseIndex == -1)
                        opts.AddRange(extraOptions);
                    else
                        opts.InsertRange(baseIndex + 1, extraOptions);
                }
            }
        }

        private static FloatMenuOption CreateMenuOption(Pawn pawn, Apparel apparel)
        {
            // original code taken from FloatMenuMakerMap_AddHumanlikeOrders
            if (!pawn.CanReach(apparel, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                return new FloatMenuOption("CannotWear".Translate(apparel.Label) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
            if (!ApparelUtility.HasPartsToWear(pawn, apparel.def))
                return new FloatMenuOption("CannotWear".Translate(apparel.Label) + " (" + "CannotWearBecauseOfMissingBodyParts".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f,
                                           null, null);
            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    "ForceWear".Translate(apparel.LabelShort),
                    () =>
                    {
                        apparel.SetForbidden(false, true);
                        Job job = new Job(JobDefOf.Wear, apparel);
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    },
                    MenuOptionPriority.High,
                    null,
                    null,
                    0f,
                    null,
                    null),
                pawn,
                apparel,
                "ReservedBy");
        }
    }
}