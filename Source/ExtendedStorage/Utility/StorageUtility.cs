using System;
using System.Collections.Generic;
using Verse;

namespace ExtendedStorage
{
    internal static class StorageUtility
    {
        public static bool HasSupressedOrSubstituedGraphics(Thing t, out Graphic gfx)
        {
            Building_ExtendedStorage b = GetStoringBuilding(t);
            if ((b != null) && !t.def.IsApparel)
            {
                gfx = t == b._suppressedDrawCandidate
                    ? b._gfxStoredThing
                    : null;
                return true;
            }
            gfx = null;
            return false;
        }

        /// <summary>
        ///     Checks if a <see cref="Thing" /> is currently held as one of the <see cref="StoredThings" /> on an
        ///     <see cref="Building_ExtendedStorage" />'s
        ///     <see cref="OutputSlot" />.
        /// </summary>
        /// <param name="t"><see cref="Thing" /> to check</param>
        public static Building_ExtendedStorage GetStoringBuilding(Thing t)
        {
            Building_ExtendedStorage b = t.Map?.thingGrid.ThingAt<Building_ExtendedStorage>(t.Position);

            return (b?.OutputSlot == t.Position) && (b.StoredThingDef == t.def)
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