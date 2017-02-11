// Karel Kroeze
// WorkGiver_ExtendedStorage.cs
// 2017-02-11

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace ExtendedStorage
{
    public class WorkGiver_StackMerger: WorkGiver_Scanner
    {
        public static void LogIfDebug(string message)
        { 
#if DEBUG
            Log.Message(message);
#endif
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_ExtendedStorage>()
                       .Where( s => !s.StorageFull 
                                 && TheoreticallyStackable( s.StoredThing ) )
                       .Select( s => s.StoredThing );
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            LogIfDebug($"{pawn.NameStringShort} is trying to skip merging, ShouldSkip is {!PotentialWorkThingsGlobal(pawn).Any()}...");
            return !PotentialWorkThingsGlobal(pawn).Any();
        }

        public override Job JobOnThing(Pawn pawn, Thing thing)
        {
            LogIfDebug($"{pawn.NameStringShort} is trying to merge {thing.Label}...");

            // standard hauling checks
            if (!HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, thing))
                return null;

            LogIfDebug($"{thing.LabelCap} can be hauled...");

            // find better place, and haul there
            IntVec3 target;
            if ( TryGetTargetCell(pawn, thing, out target))
            {
                if (pawn.Map.reservationManager.CanReserve(pawn, target, 1))
                {
                    LogIfDebug($"Hauling {thing.Label} to {target}...");
                    return HaulAIUtility.HaulMaxNumToCellJob(pawn, thing, target, true);
                }
                LogIfDebug($"Couldn't reserve {target}...");
            }

            return null;
        }

        public bool TryGetTargetCell(Pawn pawn, Thing thing, out IntVec3 target)
        {
            // get other storage buildings in the room
            var room = thing.GetRoom();
            var targets = thing.Map.listerBuildings
                               .AllBuildingsColonistOfClass<Building_ExtendedStorage>()
                               .Where( s => s.GetRoom() == room
                                            && CanBeStackTarget( s, thing, pawn ) );

            // select valid cell with the current highest count, if any
            if ( targets != null && targets.Any() )
            {
                target = targets.MaxBy( t => t.StoredThing.stackCount ).InputSlot;
                return true;
            }
            
            // no targets :(
            target = IntVec3.Invalid;
            return false;
        }

        private static bool CanBeStackTarget(Building_ExtendedStorage target, Thing thing, Pawn pawn = null)
        {
            var targetThing = target?.StoredThing;

            // todo; gives null refs when target == null
            //LogIfDebug($"CanBeStackTarget:" +
            //            $"\n\t target: {target}" +
            //            $"\n\t thing: {thing}" +
            //            $"\n\t pawn: {pawn}" +
            //            $"\n\t not same thing: {targetThing != target}" +
            //            $"\n\t canStackWith: {targetThing.CanStackWith(thing)}" +
            //            $"\n\t larger: {targetThing.stackCount >= thing.stackCount}" +
            //            $"\n\t full: {target.StorageFull}" +
            //            $"\n\t isGoodStoreCell: {StoreUtility.IsGoodStoreCell(target.InputSlot, target.Map, thing, pawn, pawn?.Faction ?? Faction.OfPlayer)}" +
            //            $"\n\t isInValidBestStorage: {targetThing.IsInValidBestStorage()}");

            return target != null && thing != null && targetThing != null
                   && targetThing != thing
                   && targetThing.CanStackWith(thing)
                   // only move stuff to larger stacks
                   && targetThing.stackCount >= thing.stackCount
                   && !target.StorageFull
                   // is a good storage cell (no blockers, can be reserved, reachable, no fires, etc)
                   && StoreUtility.IsGoodStoreCell(target.InputSlot, target.Map, thing, pawn,
                                                    pawn?.Faction ?? Faction.OfPlayer)
                   // is not waiting to be moved to a better storage
                   && targetThing.IsInValidBestStorage();
        }

        private static bool TheoreticallyStackable(Thing thing, Pawn pawn = null)
        {
            //LogIfDebug($"TheoreticallyStackable:" +
            //            $"\n\tSlotGroup: {thing?.GetSlotGroup()}" +
            //            $"\n\talwaysHaulable: {thing.def.alwaysHaulable}" +
            //            $"\n\tforbidden: {thing.IsForbidden(pawn?.Faction ?? Faction.OfPlayer)}" +
            //            $"\n\tinValidBestStorage: {thing.IsInValidBestStorage()}");

            // stack still exists, is not full yet, and doesn't need to be hauled to a different storage
            return thing?.GetSlotGroup() != null // includes thing.Spawned
                   && thing.def.alwaysHaulable
                   // if pawn is not given, assume player faction
                   && !thing.IsForbidden(pawn?.Faction ?? Faction.OfPlayer)
                   && thing.IsInValidBestStorage();
        }

        public override int LocalRegionsToScanFirst => 4;
    }
}