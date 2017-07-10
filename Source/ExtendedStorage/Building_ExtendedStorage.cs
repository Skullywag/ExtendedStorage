using System;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ExtendedStorage
{
    public class Building_ExtendedStorage : Building_Storage
    {
        private IntVec3 inputSlot;
        private IntVec3 outputSlot;
        private int maxStorage = 1000;
        private ThingDef _storedThingDef;
        private Action queuedTickAction;
        internal string label;

        public IntVec3 OutputSlot { get { return outputSlot; } }

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
            get {
                return Find.VisibleMap.thingGrid.ThingsAt(this.inputSlot)
                           .FirstOrDefault(
                               this.StoredThingDef != null
                                   ? (Func<Thing, bool>) (t => t.def == StoredThingDef)
                                   : t => slotGroup.Settings.AllowedToAccept(t));
            }
        }
        public Thing StoredThing
        {
            get
            {
                return this.StoredThingDef == null
                    ? null
                    : Find.VisibleMap.thingGrid.ThingsAt(this.outputSlot)
                          .FirstOrDefault(t => t.def == StoredThingDef);
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

        public override string LabelNoCount => label;

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
                var ptr = typeof( Building ).GetMethod( nameof(Building.GetGizmos), BindingFlags.Instance | BindingFlags.Public ).MethodHandle.GetFunctionPointer();
                Building_GetGizmos = (Func<IEnumerable<Gizmo>>)Activator.CreateInstance( typeof( Func<IEnumerable<Gizmo>> ), this, ptr );
            }

            // grandparent gizmos, skipping the base CopyPaste gizmos
            var gizmos = Building_GetGizmos();
            foreach ( Gizmo gizmo in gizmos )
                yield return gizmo;


            Command_Action a = new Command_Action
                               {
                                   icon = ContentFinder<Texture2D>.Get("UI/Icons/Rename", true),
                                   defaultDesc = "ExtendedStorage.Rename".Translate(this.def.label),
                                   defaultLabel = "Rename".Translate(),
                                   activateSound = SoundDef.Named("Click"),
                                   action = delegate { Find.WindowStack.Add(new Dialog_Rename(this)); }
                               };
            yield return a;

            // our CopyPasta gizmos
            foreach ( Gizmo gizmo in StorageSettingsClipboard.CopyPasteGizmosFor( userSettings ) )
                yield return gizmo;
        }

        public override void PostMake()
        {
            // create 'game' storage settings
            base.PostMake();

            label = GenLabel.ThingLabel(this.def, this.Stuff, 1);

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

            // if our current thingdef is not null and still allowed, re-apply the constraint to the filter
            if (StoredThingDef != null && settings.filter.Allows(StoredThingDef))
                Notify_StoredThingDefChanged(StoredThingDef);
            else {
                TryUnstackStoredItems();
                StoredThingDef = null;
            }
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
        

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.maxStorage = ((ESdef)this.def).maxStorage;
            List<IntVec3> list = GenAdj.CellsOccupiedBy(this).ToList<IntVec3>();
            this.inputSlot = list[0];
            this.outputSlot = list[1];
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish) {
            TrySplurgeStoredItems();
            base.Destroy(mode);
        }

        internal void TrySplurgeStoredItems() {
            Thing storedThing = StoredThing;

            if (storedThing == null || storedThing.stackCount <= storedThing.def.stackLimit)
                return;

            SplurgeThings(new[] {storedThing}, outputSlot);
            SoundDef.Named("DropPodOpen").PlayOneShot(new TargetInfo(outputSlot, base.Map, false));
        }

        /// <summary>
        /// Tries extracting all elements from <paramref name="things"/> around <paramref name="center" /> in stacks no larger than each 
        /// thing's def's <see cref="ThingDef.stackLimit"/>.
        /// </summary>
        /// <returns>
        /// All extracted thing stacks
        /// </returns>
        private IEnumerable<Thing> SplurgeThings(IEnumerable<Thing> things, IntVec3 center, bool forceSplurge = false) {
            // TODO: think about using built in logic - check ActriveDropPod.PodOpen & GenPlace.TryPlaceThing

            List<Thing> result = new List<Thing>();

            using (IEnumerator<IntVec3> cellEnumerator = GenRadial.RadialCellsAround(this.outputSlot, 20, false).GetEnumerator())
            using (IEnumerator<Thing> thingEnumerator = things.GetEnumerator()) {
                while (true) {
                    if (!thingEnumerator.MoveNext())            
                        goto finished;                              // no more things

                    Thing thing = thingEnumerator.Current;

                    while (!thing.DestroyedOrNull() && (forceSplurge || thing.stackCount > thing.def.stackLimit)) {
                        IntVec3 availableCell;
                        if (!TryGetNext(cellEnumerator, c => c.Standable(Map), out availableCell)) {
                            Log.Warning($"Ran out of cells to splurge {thing.LabelCap} - there might be issues on save/reload.");
                            goto finished;                          // no more cells
                        }
                        result.Add(SplitOfStackInto(thing, availableCell));
                    }
                }
            }

            finished:
            return result;
        }


        /// <summary>
        /// Removes up to <see cref="ThingDef.stackLimit"/> from <paramref name="existingThing"/> and creates a
        /// new stack of corresponding size in <paramref name="targetLocation"/>.
        /// </summary>
        /// <returns>Newly created stack</returns>
        private Thing SplitOfStackInto(Thing existingThing, IntVec3 targetLocation) {
            Thing createdThing = ThingMaker.MakeThing(existingThing.def, existingThing.Stuff);
            if (existingThing.stackCount > existingThing.def.stackLimit) {
                existingThing.stackCount -= existingThing.def.stackLimit;
                createdThing.stackCount = existingThing.def.stackLimit;
            } else {
                createdThing.stackCount = existingThing.stackCount;
                existingThing.Destroy();
            }

            return GenSpawn.Spawn(createdThing, targetLocation, Map);
        }


        // we can't really dump items immediately - otherwise typical use scenarios like "clear all, reselect X" would dump items immediately
        private void TryUnstackStoredItems() {
            Thing storedThing = StoredThing;

            if (storedThing == null)
                return;

            List<Thing> thingsToSplurge = new List<Thing>();

            // IMMEDIATELY split one big stack into n stacks of stackLimit size each.
            while (storedThing.stackCount > storedThing.def.stackLimit) {
                thingsToSplurge.Add(SplitOfStackInto(storedThing, outputSlot));
            }

            // queue splurge action for next tick - action checks *on invocation* if queued thing to splurge is allowed again, skips those
            queuedTickAction += () => {
                                    SplurgeThings(thingsToSplurge.Where(t => t.def != StoredThingDef), outputSlot, true);
                                    SoundDef.Named("DropPodOpen").PlayOneShot(new TargetInfo(outputSlot, base.Map, false));
                                };
        }


        public static bool TryGetNext<T>(IEnumerator<T> e, Predicate<T> predicate, out T value) {
            while (true) {
                if (!e.MoveNext()) {
                    value = default(T);
                    return false;
                }
                value = e.Current;
                if (predicate(value))
                    return true;
            }
        }


        public override void Tick()
        {
            base.Tick();
            if ( this.IsHashIntervalTick( 10 ) ) {

                queuedTickAction?.Invoke();
                queuedTickAction = null;

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
                from t in Map.thingGrid.ThingsAt(this.outputSlot)
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
                GenSpawn.Spawn(thing, this.outputSlot, Map);
            }
        }
        private void TryMoveItem()
        {
            if (this.StoredThingDef == null)
            {
                Thing storedThingAtInput = this.StoredThingAtInput;
                if (storedThingAtInput != null)
                {
                    this.StoredThingDef = storedThingAtInput.def;
                    Thing thing = ThingMaker.MakeThing(this.StoredThingDef, storedThingAtInput.Stuff);
                    thing.stackCount = storedThingAtInput.stackCount;
                    storedThingAtInput.Destroy(0);
                    GenSpawn.Spawn(thing, this.outputSlot, Map);
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
                Thing thing2 = ThingMaker.MakeThing(storedThingAtInput2.def, storedThingAtInput2.Stuff);
                GenSpawn.Spawn(thing2, this.outputSlot, Map);
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
            Scribe_Defs.Look<ThingDef>(ref _storedThingDef, "storedThingDef" );
            Scribe_Deep.Look( ref userSettings, "userSettings" );

            string label = this.Label;
            if (Scribe.mode != LoadSaveMode.Saving || this.label != null) {
                Scribe_Values.Look<string>(ref label, "label", base.def.label, false);
            }

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
