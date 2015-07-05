using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ExtendedStorage
{
    public class Building_ExtendedStorage : Building_Storage
    {
        private IntVec3 inputSlot, outputSlot;
        private int maxStorage = 1000;
        private ThingDef storedThingDef = null;

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            maxStorage = ((ESdef)def).maxStorage;
            List<IntVec3> cells = GenAdj.CellsOccupiedBy(this).ToList();
            inputSlot = cells[0];
            outputSlot = cells[1];
        }

        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 10 == 0)
            {
                SlotGroup slotGroup = inputSlot.GetSlotGroup();
                CheckOutputSlot();
                if (!StorageFull)
                {
                    TryMoveItem();
                }
            }
        }

        private void CheckOutputSlot()
        {
            if (storedThingDef == null) return;
            if (StoredThing == null)
            {
                storedThingDef = null;
                return;
            }
            List<Thing> things = (
                from t in Find.ThingGrid.ThingsAt(outputSlot)
                where t.def == storedThingDef
                orderby t.stackCount
                select t).ToList();
            if (things.Count > 1)
            {
                Thing thing = ThingMaker.MakeThing(storedThingDef, things.First().Stuff);
                foreach (var current in things)
                {
                    thing.stackCount += current.stackCount;
                    current.Destroy(DestroyMode.Vanish);
                }
                GenSpawn.Spawn(thing, outputSlot);
            }
        }

        private void TryMoveItem()
        {
            if (storedThingDef == null)
            {
                Thing thing = StoredThingAtInput;
                if (thing != null)
                {
                    storedThingDef = thing.def;
                    Thing thing2 = ThingMaker.MakeThing(storedThingDef, thing.Stuff);
                    thing2.stackCount = thing.stackCount;
                    thing.Destroy(DestroyMode.Vanish);
                    GenSpawn.Spawn(thing2, outputSlot);
                }
                return;
            }
            else
            {
                //TODO check for remaining storage space and only take the remaining amount
                Thing thing = StoredThingAtInput;
                Thing storedThing = StoredThing;
                if (thing != null)
                {
                    if (storedThing != null)
                    {
                        int remaining = ApparentMaxStorage - storedThing.stackCount;
                        int num = UnityEngine.Mathf.Min(remaining, thing.stackCount);
                        storedThing.stackCount += num;
                        thing.stackCount -= num;
                        if (thing.stackCount <= 0)
                            thing.Destroy(DestroyMode.Vanish);
                        return;
                    }
                    Thing thing2 = ThingMaker.MakeThing(thing.def, thing.Stuff);
                    GenSpawn.Spawn(thing2, outputSlot);
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }

        public Thing StoredThingAtInput
        {
            get
            {
                if (storedThingDef != null)
                {
                    List<Thing> things = (
                        from t in Find.ThingGrid.ThingsAt(inputSlot)
                        where t.def == storedThingDef
                        select t).ToList();
                    return things.Count > 0 ? things.First() : null;
                }
                else
                {
                    List<Thing> things = (
                        from t in Find.ThingGrid.ThingsAt(inputSlot)
                        where slotGroup.Settings.AllowedToAccept(t)
                        select t).ToList();
                    return things.Count > 0 ? things.First() : null;
                }
            }
        }

        public Thing StoredThing
        {
            get
            {
                if (storedThingDef == null) return null;

                List<Thing> things = (
                    from t in Find.ThingGrid.ThingsListAt(outputSlot)
                    where t.def == storedThingDef
                    select t).ToList();
                if (things.Count <= 0) return null;
                return things.First();
            }
        }

        public bool StorageFull
        {
            get
            {
                if (storedThingDef == null) return false;
                if (StoredThing == null) return false;
                if (StoredThing.stackCount >= ApparentMaxStorage) return true;
                return false;
            }
        }

        public int ApparentMaxStorage
        {
            get
            {
                if (storedThingDef == null) return 0;
                if (storedThingDef.stuffProps != null)
                {
                    return (int)(maxStorage / storedThingDef.stuffProps.VolumePerUnit);
                }
                return maxStorage;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.LookDef(ref this.storedThingDef, "storedThingDef");
        }
    }
}