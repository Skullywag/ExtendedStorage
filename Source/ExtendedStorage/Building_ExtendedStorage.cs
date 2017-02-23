using System;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
namespace ExtendedStorage
{
    public class Building_ExtendedStorage : Building_Storage
    {
        private IntVec3 inputSlot;
        private IntVec3 outputSlot;
        private int maxStorage = 1000;
        private ThingDef _storedThingDef;

        private ThingDef StoredThingDef
        {
            get { return _storedThingDef; }
            set
            {
                if ( _storedThingDef != value )
                    Notify_StoredThingDefChanged( value );
                _storedThingDef = value;
            }
        }
        public StorageSettings userSettings;
        public Thing StoredThingAtInput
        {
            get
            {
                if (this.StoredThingDef != null)
                {
                    List<Thing> list = (
                        from t in Find.VisibleMap.thingGrid.ThingsAt(this.inputSlot)
                        where t.def == this.StoredThingDef
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
                        from t in Find.VisibleMap.thingGrid.ThingsAt(this.inputSlot)
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
                if (this.StoredThingDef == null)
                {
                    return null;
                }
                List<Thing> list = (
                    from t in Find.VisibleMap.thingGrid.ThingsAt(this.outputSlot)
                    where t.def == this.StoredThingDef
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
                return this.StoredThingDef != null && this.StoredThing != null && this.StoredThing.stackCount >= this.ApparentMaxStorage;
            }
        }

        public int ApparentMaxStorage
        {
            get
            {
                if (this.StoredThingDef == null)
                {
                    return 0;
                }
                if (this.StoredThingDef.smallVolume)
                {
                    return (int)((float)this.maxStorage / 0.2f);
                }
                return this.maxStorage;
            }
        }

        private Func<IEnumerable<Gizmo>> Building_GetGizmos;
        
        public override IEnumerable<Gizmo> GetGizmos()
        {
            // we can't extend the results of base.GetGizmos() because we are replacing the 
            // copy/paste buttons for filter settings. Main problem then is that we also need 
            // to re-add any comp gizmos, the minifiable (re-)install gizmo and the copy gizmo.
            // So instead, let's get hacky and try to call the grandparents' implementation of 
            // GetGizmos.

            if ( Building_GetGizmos == null )
            {
                // http://stackoverflow.com/a/32562464
                var ptr = typeof( Building ).GetMethod( "GetGizmos", BindingFlags.Instance | BindingFlags.Public ).MethodHandle.GetFunctionPointer();
                Building_GetGizmos = (Func<IEnumerable<Gizmo>>)Activator.CreateInstance( typeof( Func<IEnumerable<Gizmo>> ), this, ptr );
            }

            // grandparent gizmos, skipping the base CopyPaste gizmos
            var gizmos = Building_GetGizmos();
            foreach ( Gizmo gizmo in gizmos )
                yield return gizmo;

            // our CopyPasta gizmos
            foreach ( Gizmo gizmo in StorageSettingsClipboard.CopyPasteGizmosFor( userSettings ) )
                yield return gizmo;
        }

        public override void PostMake()
        {
            // create 'game' storage settings
            base.PostMake();

            // create 'user' storage settings
            userSettings = new StorageSettings( this );

            // copy over default filter/priority
            if ( def.building.defaultStorageSettings != null )
                userSettings.CopyFrom( this.def.building.defaultStorageSettings );

            // change callback to point to our custom logic
            SetCallback( userSettings.filter, Notify_UserSettingsChanged );
        }

        public void Notify_UserSettingsChanged()
        {
            // the vanilla StorageSettings.TryNotifyChanged will alert the SlotGroupManager that 
            // storage settings have changed. We don't need this behaviour for user settings, as these
            // don't directly influence the slotgroup, and any changes we make are propagated to the 
            // 'real' storage settings, which will still notify the SlotGroupManager on change.
            
            // check if priority changed, update if needed
            if ( settings.Priority != userSettings.Priority )
                settings.Priority = userSettings.Priority;

            // we could check for changed allowances, but checking for special filters would be tricky.
            // Instead, just copy the filter over, resetting it.
            settings.filter.CopyAllowancesFrom( userSettings.filter );

            // if our current thingdef is not null and still allowed, re-apply the constraint to the filter.
            if ( StoredThingDef != null && settings.filter.Allows( StoredThingDef ) )
                Notify_StoredThingDefChanged( StoredThingDef );
        }

        public void Notify_StoredThingDefChanged( ThingDef newDef )
        {
            // Whenever the stored thingDef changes, we need to update the 'real' storage settings,
            // the intended effect is that when something is stored, the storage will henceforth only
            // accept more of this def.
            if ( newDef != null )
            {
                // disallow everything currently allowed
                // NOTE: Can't use SetDisallowAll() because that would also disallow special filters
                List<ThingDef> allowed = new List<ThingDef>( settings.filter.AllowedThingDefs );
                foreach ( ThingDef def in allowed )
                    settings.filter.SetAllow( def, false );

                // allow this specific def
                settings.filter.SetAllow( newDef, true );
            }

            // When emptied, just copy over userSettings
            else
                settings.filter.CopyAllowancesFrom( userSettings.filter );
        }

        public override string GetInspectString()
        {
            StringBuilder inspectString = new StringBuilder();
            inspectString.Append( base.GetInspectString() );
            inspectString.Append(
                                 "ExtendedStorage.CurrentlyStoring".Translate( StoredThingDef?.LabelCap ??
                                                                               "ExtendedStorage.Nothing".Translate() ) );
            return inspectString.ToString();
        }
        

        public override void SpawnSetup(Map map)
        {
            base.SpawnSetup(map);
            this.maxStorage = ((ESdef)this.def).maxStorage;
            List<IntVec3> list = GenAdj.CellsOccupiedBy(this).ToList<IntVec3>();
            this.inputSlot = list[0];
            this.outputSlot = list[1];
        }
        public override void Tick()
        {
            base.Tick();
            if ( this.IsHashIntervalTick( 10 ) )
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
            if (this.StoredThingDef == null)
            {
                return;
            }
            if (this.StoredThing == null)
            {
                this.StoredThingDef = null;
                return;
            }
            List<Thing> list = (
                from t in Find.VisibleMap.thingGrid.ThingsAt(this.outputSlot)
                where t.def == this.StoredThingDef
                orderby t.stackCount
                select t).ToList<Thing>();
            if (list.Count > 1 && !StoredThingDef.IsApparel)
            {
                Thing thing = ThingMaker.MakeThing(this.StoredThingDef, list.First<Thing>().Stuff);
                foreach (Thing current in list)
                {
                    thing.stackCount += current.stackCount;
                    current.Destroy(0);
                }
                GenSpawn.Spawn(thing, this.outputSlot, Find.VisibleMap);
            }
        }
        private void TryMoveItem()
        {
            if (this.StoredThingDef == null)
            {
                Thing storedThingAtInput = this.StoredThingAtInput;
                if (storedThingAtInput != null)
                {
                    Log.Error("shouldnt be here");
                    this.StoredThingDef = storedThingAtInput.def;
                    Thing thing = ThingMaker.MakeThing(this.StoredThingDef, storedThingAtInput.Stuff);
                    thing.stackCount = storedThingAtInput.stackCount;
                    storedThingAtInput.Destroy(0);
                    GenSpawn.Spawn(thing, this.outputSlot, Find.VisibleMap);
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
                    if (StoredThingDef.IsApparel)
                    {
                        Log.Error("should be here");
                        storedThingAtInput2.Position = this.outputSlot;
                        SlotGroup slotGroup = outputSlot.GetSlotGroup(this.Map);
                        if (slotGroup != null && slotGroup.parent != null)
                        {
                            slotGroup.parent.Notify_ReceivedThing(storedThingAtInput2);
                        }
                        return;
                    }
                    else
                    {
                        int num = Mathf.Min(a, storedThingAtInput2.stackCount);
                        storedThing.stackCount += num;
                        storedThingAtInput2.stackCount -= num;
                        if (storedThingAtInput2.stackCount <= 0)
                        {
                            storedThingAtInput2.Destroy(0);
                        }
                        return;
                    }
                }
                Log.Error("shouldnt be here");
                Thing thing2 = ThingMaker.MakeThing(storedThingAtInput2.def, storedThingAtInput2.Stuff);
                GenSpawn.Spawn(thing2, this.outputSlot, Find.VisibleMap);
                storedThingAtInput2.Destroy(0);
            }
        }

        private FieldInfo _settingsChangedCallback_FI = typeof( ThingFilter ).GetField( "settingsChangedCallback",
                                                                                     BindingFlags.NonPublic |
                                                                                     BindingFlags.Instance );

        protected void SetCallback( ThingFilter filter, Action callback )
        {
            if ( _settingsChangedCallback_FI == null )
                throw new ArgumentNullException( "_settingsChangedCallback FieldInfo" );

            _settingsChangedCallback_FI.SetValue( filter, callback );
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.LookDef<ThingDef>(ref _storedThingDef, "storedThingDef" );
            Scribe_Deep.LookDeep( ref userSettings, "userSettings" );
            
            // we need to re-apply our callback on the userSettings after load.
            // in addition, we need some migration code for handling mid-save upgrades.
            // todo: the migration part of this can be removed on the A17 update.
            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                // migration
                if ( userSettings == null )
                {
                    // create 'user' storage settings
                    userSettings = new StorageSettings( this );
                    
                    // copy over previous filter/priority
                    userSettings.filter.CopyAllowancesFrom( settings.filter );
                    userSettings.Priority = settings.Priority;

                    // apply currently stored logic
                    Notify_StoredThingDefChanged( StoredThingDef );
                } 

                // re-apply callback
                SetCallback( userSettings.filter, Notify_UserSettingsChanged );
            }
        }
    }
}
