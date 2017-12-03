using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace ExtendedStorage
{
    public class WorkGiver_Merge: WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal( Pawn pawn )
        {
            return pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_ExtendedStorage>()
                .Where( b => !b.AtCapacity 
                        && !b.IsForbidden( pawn )
                        && b.StoredThingTotal > 0 )
                .SelectMany( b => b.StoredThings )
                .Where( t => t.TheoreticallyStackable( pawn ) );
        }

        public override bool ShouldSkip( Pawn pawn )
        {
            return !PotentialWorkThingsGlobal( pawn ).Any();
        }

        public override Job JobOnThing( Pawn pawn, Thing thing, bool forced = false )
        {
            // do some more intensive checks
            if ( !HaulAIUtility.PawnCanAutomaticallyHaulFast( pawn, thing, forced ) )
                return null;

            // try get a target cell
            IntVec3 target;
            if ( TryGetTargetCell( pawn, thing, out target ) )
                return HaulAIUtility.HaulMaxNumToCellJob( pawn, thing, target, true );

            return null;
        }

        public static bool TryGetTargetCell( Pawn pawn, Thing thing, out IntVec3 targetCell )
        {
            // get the room we're in
            var room = thing.GetRoom();

            // get the container we're in
            var source = StorageUtility.GetStoringBuilding( thing );

            // get other containers in our room
            var targets = thing.Map.listerBuildings
                .AllBuildingsColonistOfClass<Building_ExtendedStorage>()
                .Where( target => target.GetRoom() == room && target != source );

            // go over cells in order of current stored, return first that is a valid target
            foreach ( var target in targets.OrderByDescending( target => target.StoredThingTotal ) )
            {
                if ( target.CanBeMergeTargetFor( source, thing, pawn ) )
                {
                    targetCell = target.InputSlot;
                    return true;
                }
            }

            // nothing found
            targetCell = IntVec3.Zero;
            return false;
        }
    }
}