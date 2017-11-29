using System.Linq;
using RimWorld;
using Verse;

namespace ExtendedStorage
{
    public static class Extensions_StackMerging
    {
        public static bool TheoreticallyStackable( this Thing thing, Pawn pawn = null )
        {
            return
                // thing is not forbidden
                !thing.IsForbidden( pawn?.Faction ?? Faction.OfPlayer )
                
                // thing is not going to be moved somewhere else
                && thing.IsInValidBestStorage();
        }

        public static bool CanBeMergeTargetFor(
            this Building_ExtendedStorage target, 
            Building_ExtendedStorage source,
            Thing thing,
            Pawn pawn )
        {
            return
                // thingdefs match
                thing.def == target.StoredThingDef

                // target is not full
                && target.StoredThingTotal < target.ApparentMaxStorage

                // source stack is smaller or equal in size to target
                && source.StoredThingTotal <= target.StoredThingTotal

                // target input is a valid storage cell (no blockers, can be reserved/reached, not on fire, etc.)
                && StoreUtility.IsGoodStoreCell( target.InputSlot, target.Map, thing, pawn,
                    pawn?.Faction ?? Faction.OfPlayer )

                // target contents are not waiting to be moved, and it does have contents
                && ( target.StoredThings.First()?.IsInValidBestStorage() ?? false );
        }
    }
}