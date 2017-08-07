using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ExtendedStorage
{
    internal static class StorageUtility
    {
        /// <summary>
        ///     Checks if a <see cref="Thing" /> is currently held as one of the <see cref="StoredThings" /> on an
        ///     <see cref="Building_ExtendedStorage" />'s
        /// </summary>
        /// <param name="t"><see cref="Thing" /> to check</param>
        public static Building_ExtendedStorage GetStoringBuilding(Thing t)
        {
            // HACK: GetSlotGroup is weird (ish)
            var b = t is Building_ExtendedStorage
                ? null
                : t.GetSlotGroup()?.parent as Building_ExtendedStorage;

            return b?.StoredThingDef == t.def
                ? b
                : null;
        }

        /// <summary>
        ///     Removes up to <see cref="ThingDef.stackLimit" /> from <paramref name="existingThing" /> and creates a
        ///     new stack of corresponding size in <paramref name="targetLocation" />.
        /// </summary>
        /// <returns>Newly created stack</returns>
        public static Thing SplitOfStackInto(Thing existingThing, IntVec3 targetLocation)
        {
            Map map = existingThing.Map;

            Thing createdThing = ThingMaker.MakeThing(existingThing.def, existingThing.Stuff);
            createdThing.HitPoints = existingThing.HitPoints;

            if (existingThing.stackCount > existingThing.def.stackLimit)
            {
                existingThing.stackCount -= existingThing.def.stackLimit;
                createdThing.stackCount = existingThing.def.stackLimit;
            }
            else
            {
                createdThing.stackCount = existingThing.stackCount;
                existingThing.Destroy();
            }

            return GenSpawn.Spawn(createdThing, targetLocation, map);
        }
    }
}