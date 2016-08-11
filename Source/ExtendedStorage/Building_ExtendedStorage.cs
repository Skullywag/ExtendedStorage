using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
namespace ExtendedStorage
{
    public class Building_ExtendedStorage : Building_Storage
    {
        private IntVec3 inputSlot;
        private IntVec3 outputSlot;
        private int maxStorage = 1000;
        private ThingDef storedThingDef;
        public Thing StoredThingAtInput
        {
            get
            {
                if (this.storedThingDef != null)
                {
                    List<Thing> list = (
                        from t in Find.ThingGrid.ThingsAt(this.inputSlot)
                        where t.def == this.storedThingDef
                        select t).ToList<Thing>();
                    if (list.Count <= 0)
                    {
                        return null;
                    }
                    return list.First<Thing>();
                }
                else
                {
                    List<Thing> list2 = (
                        from t in Find.ThingGrid.ThingsAt(this.inputSlot)
                        where this.slotGroup.Settings.AllowedToAccept(t)
                        select t).ToList<Thing>();
                    if (list2.Count <= 0)
                    {
                        return null;
                    }
                    return list2.First<Thing>();
                }
            }
        }
        public Thing StoredThing
        {
            get
            {
                if (this.storedThingDef == null)
                {
                    return null;
                }
                List<Thing> list = (
                    from t in Find.ThingGrid.ThingsListAt(this.outputSlot)
                    where t.def == this.storedThingDef
                    select t).ToList<Thing>();
                if (list.Count <= 0)
                {
                    return null;
                }
                return list.First<Thing>();
            }
        }
        public bool StorageFull
        {
            get
            {
                return this.storedThingDef != null && this.StoredThing != null && this.StoredThing.stackCount >= this.ApparentMaxStorage;
            }
        }
        public int ApparentMaxStorage
        {
            get
            {
                if (this.storedThingDef == null)
                {
                    return 0;
                }
                if (this.storedThingDef.smallVolume)
                {
                    return (int)((float)this.maxStorage / 0.2f);
                }
                return this.maxStorage;
            }
        }
        public override void SpawnSetup()
        {
            base.SpawnSetup();
            this.maxStorage = ((ESdef)this.def).maxStorage;
            List<IntVec3> list = GenAdj.CellsOccupiedBy(this).ToList<IntVec3>();
            this.inputSlot = list[0];
            this.outputSlot = list[1];
        }
        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 10 == 0)
            {
                this.CheckOutputSlot();
                if (!this.StorageFull)
                {
                    this.TryMoveItem();
                }
            }
        }
        private void CheckOutputSlot()
        {
            if (this.storedThingDef == null)
            {
                Log.Error("storedthingdef is null");
                return;
            }
            if (this.StoredThing == null)
            {
                this.storedThingDef = null;
                Log.Error("storedthing is null so setting storedthingdef to null");
                return;
            }
            List<Thing> list = (
                from t in Find.ThingGrid.ThingsAt(this.outputSlot)
                where t.def == this.storedThingDef
                orderby t.stackCount
                select t).ToList<Thing>();
            if (list.Count > 1)
            {
                Log.Error("storedthingdef in checkoutput is "+this.storedThingDef);
                Thing thing = ThingMaker.MakeThing(this.storedThingDef, list.First<Thing>().Stuff);
                foreach (Thing current in list)
                {
                    thing.stackCount += current.stackCount;
                    current.Destroy(0);
                }
                GenSpawn.Spawn(thing, this.outputSlot);
            }
        }
        private void TryMoveItem()
        {
            if (this.storedThingDef == null)
            {
                Thing storedThingAtInput = this.StoredThingAtInput;
                if (storedThingAtInput != null)
                {
                    this.storedThingDef = storedThingAtInput.def;
                    Thing thing = ThingMaker.MakeThing(this.storedThingDef, storedThingAtInput.Stuff);
                    thing.stackCount = storedThingAtInput.stackCount;
                    storedThingAtInput.Destroy(0);
                    GenSpawn.Spawn(thing, this.outputSlot);
                }
                return;
            }
            Thing storedThingAtInput2 = this.StoredThingAtInput;
            Thing storedThing = this.StoredThing;
            if (storedThingAtInput2 != null)
            {
                if (storedThing != null)
                {
                    int a = this.ApparentMaxStorage - storedThing.stackCount;
                    int num = Mathf.Min(a, storedThingAtInput2.stackCount);
                    storedThing.stackCount += num;
                    storedThingAtInput2.stackCount -= num;
                    if (storedThingAtInput2.stackCount <= 0)
                    {
                        storedThingAtInput2.Destroy(0);
                    }
                    return;
                }
                Thing thing2 = ThingMaker.MakeThing(storedThingAtInput2.def, storedThingAtInput2.Stuff);
                Log.Error("thing2 = "+thing2.def.defName);
                GenSpawn.Spawn(thing2, this.outputSlot);
                storedThingAtInput2.Destroy(0);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.LookDef<ThingDef>(ref this.storedThingDef, "storedThingDef");
        }
    }
}
