using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ExtendedStorage.Patches;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ExtendedStorage
{
    public interface IUserSettingsOwner : IStoreSettingsParent
    {
        void Notify_UserSettingsChanged();
    }



    public class Building_ExtendedStorage : Building_Storage, IUserSettingsOwner {
        #region fields

        internal Graphic _gfxStoredThing;
        private string _label;

        private ThingDef _storedThingDef;

        private Func<IEnumerable<Gizmo>> Building_GetGizmos;
        private IntVec3 inputSlot;

        private IntVec3 outputSlot;
        private Action queuedTickAction;
        internal string label;

        public UserSettings userSettings;

        #endregion

        #region Properties

        public bool AtCapacity => StoredThingTotal >= ApparentMaxStorage;

        public int ApparentMaxStorage => StoredThingDef == null
            ? Int32.MaxValue
            : (int) (StoredThingDef.stackLimit*this.GetStatValue(DefReferences.Stat_ES_StorageFactor));

        public IntVec3 OutputSlot => outputSlot;

        public IntVec3 InputSlot => inputSlot;

        internal ThingDef StoredThingDef
        {
            get { return _storedThingDef; }
            set
            {
                bool changed = _storedThingDef != value;
                _storedThingDef = value;
                if (changed)
                    Notify_StoredThingDefChanged();
            }
        }

        public Thing StoredThingAtInput
        {
            get
            {
                return Map?.thingGrid.ThingsAt(inputSlot)
                           .FirstOrDefault(
                               StoredThingDef != null
                                   ? (Func<Thing, bool>) (t => t.def == StoredThingDef)
                                   : t => slotGroup.Settings.AllowedToAccept(t));
            }
        }

        public IEnumerable<Thing> StoredThings
        {
            get
            {
                if (StoredThingDef == null)
                    return Enumerable.Empty<Thing>();
                return Map?.thingGrid.ThingsAt(outputSlot).Where(t => t.def == StoredThingDef);
            }
        }

        public int StoredThingTotal => StoredThings.Sum(t => t.stackCount);

        public override string LabelNoCount => label ?? (label = InitialLabel());

        #endregion

        #region Base overrides

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            TrySplurgeStoredItems();
            base.Destroy(mode);
        }

        public override void DrawGUIOverlay()
        {
            if (Find.CameraDriver.CurrentZoom != CameraZoomRange.Closest)
                return;

            Color labelColor = new Color(1f, 1f, 0.5f, 0.75f); // yellowish white for our total stack counts - default is GenMapUI.DefaultThingLabelColor

            if (!string.IsNullOrEmpty(_label))
            {
                var thing = StoredThings.FirstOrDefault();
                if (thing != null)
                    GenMapUI.DrawThingLabel(thing, _label, labelColor);
            }
        }

        public override void Draw()
        {
            base.Draw();
            if ((true == StoredThingDef?.IsApparel) || (true == StoredThingDef?.IsWeapon) || (true == StoredThingDef?.IsCorpse))
                return;

            _gfxStoredThing?.DrawFromDef(
                GenThing.TrueCenter(OutputSlot, Rot4.North, IntVec2.One, Altitudes.AltitudeFor(AltitudeLayer.Item)),
                Rot4.North,
                StoredThingDef);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref _storedThingDef, "storedThingDef");
            Scribe_Deep.Look(ref userSettings, "userSettings", this);

            if (Scribe.mode != LoadSaveMode.Saving || this.label != null) {
                Scribe_Values.Look<string>(ref label, "label", def.label, false);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            // we can't extend the results of base.GetGizmos() because we are replacing the
            // copy/paste buttons for filter settings. Main problem then is that we also need
            // to re-add any comp gizmos, the minifiable (re-)install gizmo and the copy gizmo.
            // So instead, let's get hacky and try to call the grandparents' implementation of
            // GetGizmos.

            if (Building_GetGizmos == null)
            {
                // http://stackoverflow.com/a/32562464
                IntPtr ptr = typeof(Building).GetMethod(nameof(Building.GetGizmos), BindingFlags.Instance | BindingFlags.Public).MethodHandle.GetFunctionPointer();
                Building_GetGizmos = (Func<IEnumerable<Gizmo>>) Activator.CreateInstance(typeof(Func<IEnumerable<Gizmo>>), this, ptr);
            }

            // grandparent gizmos, skipping the base CopyPaste gizmos
            IEnumerable<Gizmo> gizmos = Building_GetGizmos();
            foreach (Gizmo gizmo in gizmos)
                yield return gizmo;


            Command_Action a = new Command_Action
                               {
                                   icon = ContentFinder<Texture2D>.Get("UI/Icons/Rename", true),
                                   defaultDesc = LanguageKeys.keyed.ExtendedStorage_Rename.Translate(this.def.label),
                                   defaultLabel = "Rename".Translate(),
                                   activateSound = SoundDef.Named("Click"),
                                   action = delegate { Find.WindowStack.Add(new Dialog_Rename(this)); },
                                   groupKey = 942608684                 // guaranteed to be random - https://xkcd.com/221/
            };
            yield return a;

            // our CopyPasta gizmos
            foreach (Gizmo gizmo in StorageSettingsClipboard.CopyPasteGizmosFor(userSettings))
                yield return gizmo;
        }

        public override string GetInspectString()
        {
            StringBuilder inspectString = new StringBuilder();
            inspectString.Append(base.GetInspectString());
            inspectString.Append(LanguageKeys.keyed.ExtendedStorage_CurrentlyStoringInspect.Translate(StoredThingDef?.LabelCap ??
                                                                                                      LanguageKeys.keyed.ExtendedStorage_Nothing.Translate()));
            return inspectString.ToString();
        }

        public override void Notify_LostThing(Thing newItem)
        {
            base.Notify_LostThing(newItem);
            Notify_SlotGroupItemsChanged();
        }

        public override void Notify_ReceivedThing(Thing newItem) {
            base.Notify_ReceivedThing(newItem);
            Notify_SlotGroupItemsChanged();
        }

        public override void PostMake()
        {
            // create 'game' storage settings
            base.PostMake();

            // create 'user' storage settings
            userSettings = new UserSettings(this);

            // copy over default filter/priority
            if (def.building.defaultStorageSettings != null)
                userSettings.CopyFrom(def.building.defaultStorageSettings);
        }


        public override void PostMapInit()
        {
            base.PostMapInit();


            // old version might have been saved in an inconsistent state - force correct StoredThingDef
            RecalculateStoredThingDef();
            // old versions will have stored giant stacks
            ChunkifyOutputSlot();

            // can't do this in postmake, since stored stacks might not yet have been created
            UpdateCachedAttributes();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            List<IntVec3> list = GenAdj.CellsOccupiedBy(this).ToList();
            inputSlot = list[0];
            outputSlot = list[1];

            if (this.label == null || this.label.Trim().Length == 0) {
                this.label = InitialLabel();
            }
        }

        private string InitialLabel()
        {
            return GenLabel.ThingLabel(this, 1, false);
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats() {
            foreach (StatDrawEntry specialDisplayStat in base.SpecialDisplayStats())
            {
                yield return specialDisplayStat;
            }

            yield return new StatDrawEntry(
                                DefReferences.StatCategory_ExtendedStorage,
                                LanguageKeys.keyed.ExtendedStorage_CurrentlyStoringStat.Translate(),
                                StoredThingDef?.LabelCap ?? LanguageKeys.keyed.ExtendedStorage_Nothing.Translate(),
                                -1);
            yield return new StatDrawEntry(
                                DefReferences.StatCategory_ExtendedStorage,
                                LanguageKeys.keyed.ExtendedStorage_UsageStat.Translate(),
                                StoredThingDef != null
                                ? LanguageKeys.keyed.ExtendedStorage_UsageStat_Value.Translate(StoredThingTotal, ApparentMaxStorage)
                                : LanguageKeys.keyed.ExtendedStorage_NA.Translate(),
                                -2);
        }


        public override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(10))
            {
                TryGrabOutputItem();

                queuedTickAction?.Invoke();
                queuedTickAction = null;

                ChunkifyOutputSlot();

                TryMoveItem();
            }
        }

        private void TryGrabOutputItem()
        {
            if (StoredThingDef == null)
            {
                StoredThingDef = Map?.thingGrid.ThingsAt(outputSlot).Where(userSettings.AllowedToAccept).FirstOrDefault()?.def;
                InvalidateThingSection(_storedThingDef);
            }
        }

        #endregion

        StorageSettings IStoreSettingsParent.GetStoreSettings()
        {
            if (DebugSettings.godMode && ITab_Storage_FillTab.showStoreSettings)
                return base.GetStoreSettings();

            return userSettings;
        }

        #region Notification handlers

        public void Notify_UserSettingsChanged()
        {

            // the vanilla StorageSettings.TryNotifyChanged will alert the SlotGroupManager that
            // storage settings have changed. We don't need this behaviour for user settings, as these
            // don't directly influence the slotgroup, and any changes we make are propagated to the
            // 'real' storage settings, which will still notify the SlotGroupManager on change.

            // check if priority changed, update if needed
            if (settings.Priority != userSettings.Priority)
                settings.Priority = userSettings.Priority;

            // we could check for changed allowances, but checking for special filters would be tricky.
            // Instead, just copy the filter over, resetting it.
            settings.filter.CopyAllowancesFrom(userSettings.filter);

            // if our current thingdef is not null and still allowed, re-apply the constraint to the filter
            if ((StoredThingDef != null) && settings.filter.Allows(StoredThingDef))
                Notify_StoredThingDefChanged();
            else
            {
                TryUnstackStoredItems();
                var storedDef = StoredThingDef;
                StoredThingDef = null;
                InvalidateThingSection(storedDef);
            }
        }

        /// <summary>
        /// Checks if the storedDef has a mapMesh painting - if so, invalidate the apppropriate SectionLayer (needed for
        /// chunks to appear immediately while game is paused &amp; exclusion by filter)
        /// </summary>
        private void InvalidateThingSection(ThingDef storedDef)
        {
            switch (storedDef?.drawerType)
            {
                case DrawerType.MapMeshOnly:
                case DrawerType.MapMeshAndRealTime:
                    Map?.mapDrawer.SectionAt(OutputSlot).RegenerateLayers(MapMeshFlag.Things);
                    break;
            }
        }

        public void Notify_StoredThingDefChanged()
        {
            // Whenever the stored thingDef changes, we need to update the 'real' storage settings,
            // the intended effect is that when something is stored, the storage will henceforth only
            // accept more of this def.
            if (StoredThingDef != null)
            {
                // disallow everything currently allowed
                // NOTE: Can't use SetDisallowAll() because that would also disallow special filters
                List<ThingDef> allowed = new List<ThingDef>(settings.filter.AllowedThingDefs);
                foreach (ThingDef def in allowed)
                    settings.filter.SetAllow(def, false);

                // allow this specific def
                settings.filter.SetAllow(StoredThingDef, true);
            }

            // When emptied, just copy over userSettings
            else
                settings.filter.CopyAllowancesFrom(userSettings.filter);

            UpdateCachedAttributes();
        }

        private void Notify_SlotGroupItemsChanged()
        {
            var oldValue = _storedThingDef;

            RecalculateStoredThingDef();

            // Updates to StoredThingDef cascade to UpdateCachedAttributes() - only need to call that if no change occured
            if (_storedThingDef == oldValue)
                UpdateCachedAttributes();
        }

        #endregion

        /// <summary>
        ///     Splits of or combines non <see cref="ThingDef.stackLimit" /> stacks matching <see cref="StoredThingDef" /> on
        ///     <see cref="OutputSlot" />.
        /// </summary>
        private void ChunkifyOutputSlot()
        {
            if (StoredThingDef == null)
                return;

            var nonLimitStacks = StoredThings.Where(t => t.stackCount != t.def.stackLimit).ToList();


            // don't need to chunkify anything if we just have one single undersize stack
            if ((nonLimitStacks.Count == 1) && (nonLimitStacks[0].stackCount < StoredThingDef.stackLimit))
                return;

            int total = nonLimitStacks.Sum(t => t.stackCount);
            float healthFactor = (float) nonLimitStacks.Sum(t => t.stackCount*t.HitPoints)/(total*StoredThingDef.BaseMaxHitPoints);

            // distribute the non-full stacks - take from the top, try merging to the bottom

            int idxDonor = nonLimitStacks.Count - 1;
            int idxRecipient = 0;
            while (idxDonor > idxRecipient)
            {
                var donor = nonLimitStacks[idxDonor];
                var recipient = nonLimitStacks[idxRecipient];

                if (!recipient.TryAbsorbStack(donor, true))
                {
                    idxRecipient++;
                }
                else
                {
                    idxDonor--;
                }
            }

            if (idxDonor == idxRecipient)
            {
                // 'overflow' case
                var donor = nonLimitStacks[idxDonor];
                while (nonLimitStacks[idxDonor].stackCount > StoredThingDef.stackLimit)
                {
                    var splitoff = donor.SplitOff(Math.Min(StoredThingDef.stackLimit, donor.stackCount - StoredThingDef.stackLimit));
                    splitoff.Position = donor.Position;
                    splitoff.SpawnSetup(donor.Map, false);
                    outputSlot.GetSlotGroup(Map)?.parent?.Notify_ReceivedThing(splitoff);
                }
            }
        }

        /// <summary>
        ///  force update <see cref="StoredThingDef"/> with actual storage situation
        /// </summary>
        private void RecalculateStoredThingDef()
        {
            if (StoredThingDef == null)
            {
                StoredThingDef = Map?.thingGrid.ThingsAt(outputSlot)
                                     .FirstOrDefault(t => settings.filter.Allows(t))
                                     ?.def;
            }
            else
            {
                if (!StoredThings.Any())
                    StoredThingDef = null;
            }
        }

        /// <summary>
        ///     Tries extracting all elements from <paramref name="things" /> around <paramref name="center" /> in stacks no larger
        ///     than each thing's def's <see cref="ThingDef.stackLimit" />.
        /// </summary>
        /// <returns>
        ///     All extracted thing stacks
        /// </returns>
        private IEnumerable<Thing> SplurgeThings(IEnumerable<Thing> things, IntVec3 center, bool forceSplurge = false)
        {
            // TODO: think about using built in logic - check ActriveDropPod.PodOpen & GenPlace.TryPlaceThing

            List<Thing> result = new List<Thing>();

            using (IEnumerator<IntVec3> cellEnumerator = GenRadial.RadialCellsAround(center, 20, false).GetEnumerator())
            {
                using (IEnumerator<Thing> thingEnumerator = things.GetEnumerator())
                {
                    while (true)
                    {
                        if (!thingEnumerator.MoveNext())
                            goto finished; // no more things

                        Thing thing = thingEnumerator.Current;

                        // skip things with quality to avoid issues with moved/split of stacks
                        if (thing.TryGetComp<CompQuality>() != null)
                            continue;

                        while (!thing.DestroyedOrNull() && (forceSplurge || (thing.stackCount > thing.def.stackLimit)))
                        {
                            IntVec3 availableCell;
                            if (!EnumUtility.TryGetNext(cellEnumerator, c => c.Standable(Map), out availableCell))
                            {
                                Log.Warning($"Ran out of cells to splurge {thing.LabelCap} - there might be issues on save/reload.");
                                goto finished; // no more cells
                            }

                            result.Add(StorageUtility.SplitOfStackInto(thing, availableCell));
                        }
                    }
                }
            }

            finished:
            return result;
        }

        private void TryMoveItem()
        {
            Thing input = StoredThingAtInput;

            if (input == null)
                return;

            ThingDef outputDef = StoredThingDef;

            if (((outputDef == null) && settings.filter.Allows(input)) || (input.def == outputDef))
            {
                // think about building as ThingOwner - can contents still be accessed then?
                int spaceRamaining = Math.Min(input.stackCount, ApparentMaxStorage - StoredThingTotal);

                if (spaceRamaining > 0)
                {
                    Thing moved = input.SplitOff(spaceRamaining);
                    moved.Position = outputSlot;
                    moved.SpawnSetup(Map, false);
                    StoredThingDef = moved.def;
                    outputSlot.GetSlotGroup(Map)?.parent?.Notify_ReceivedThing(moved);
                }
            }
        }

        internal void TrySplurgeStoredItems()
        {
            IEnumerable<Thing> storedThings = StoredThings;

            if (storedThings == null)
                return;

            SplurgeThings(storedThings, outputSlot, true);
            SoundDef.Named("DropPodOpen").PlayOneShot(new TargetInfo(outputSlot, Map, false));
            StoredThingDef = null;
        }

        /// <remarks>
        /// we can't really dump items immediately - otherwise typical use scenarios like "clear all, reselect X" would dump items immediately
        /// </remarks>
        private void TryUnstackStoredItems()
        {
            List<Thing> thingsToSplurge = StoredThings.ToList();

            // queue splurge action for next tick - action checks *on invocation* if queued thing to splurge is allowed again, skips those
            queuedTickAction += () =>
                                {
                                    Thing[] validThings = thingsToSplurge.Where(t => t.def != StoredThingDef).ToArray();
                                    SplurgeThings(validThings, outputSlot, true);

                                    if (validThings.Length != 0)
                                        SoundDef.Named("DropPodOpen").PlayOneShot(new TargetInfo(outputSlot, Map, false));
                                };

        }

        /// <summary>
        /// Update necessary data for label &amp; icon overrides
        /// </summary>
        public void UpdateCachedAttributes()
        {
            if (StoredThingDef != null)
            {
                List<Thing> items = StoredThings.ToList();

                QualityCategory[] qualityCategories = items.Select(t =>
                                                                   {
                                                                       QualityCategory c;
                                                                       return t.TryGetQuality(out c) ? c : (QualityCategory?) null;
                                                                   })
                                                           .Where(c => c != null)
                                                           .Select(c => c.Value)
                                                           .Distinct()
                                                           .OrderBy(c => c)
                                                           .ToArray();

                int total = StoredThingTotal;


                if (qualityCategories.Length != 0)
                {
                    _label = qualityCategories[0].GetLabelShort();

                    if (qualityCategories.Length > 1)
                        _label = string.Format(LanguageKeys.keyed.ExtendedStorage_MultipleQualities.Translate(), _label);
                }
                else
                {
                    _label = string.Format(LanguageKeys.keyed.ExtendedStorage_TotalCount.Translate(), total);
                }

                if ((!StoredThingDef.IsApparel) || (!StoredThingDef.IsWeapon) || (!StoredThingDef.IsCorpse))
                {
                    _gfxStoredThing = (StoredThingDef.graphic as Graphic_StackCount)
                                        ?.SubGraphicForStackCount(Math.Min(total, StoredThingDef.stackLimit), StoredThingDef)
                                        ?? StoredThingDef.graphic;
                }
                else
                {
                    _gfxStoredThing = null;
                }
            }
            else
            {
                _label = null;
                _gfxStoredThing = null;
            }
        }
    }
}
