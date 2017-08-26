using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace ExtendedStorage {
    public class UserSettings : StorageSettings, IExposable
    {

        private static Action<ThingFilter, Action> setThingFilterSettingsChangedCallback = Access.GetFieldSetter<ThingFilter, Action>("settingsChangedCallback");


        private IUserSettingsOwner _owner;

        public UserSettings(IUserSettingsOwner owner) : base(owner)
        {
            _owner = owner;

            // needed for 'newly constructed' ES buildings
            FixupFilterChangeCallback();
        }

        private void FixupFilterChangeCallback()
        {
            // HACK: reflection instanced members seem to get shit inlined, which means out harmony patches dont apply, which means we're fucked...
            setThingFilterSettingsChangedCallback(this.filter, NotifyOwnerSettingsChanged);
        }


        public void NotifyOwnerSettingsChanged() 
        {
            _owner.Notify_UserSettingsChanged();
        }

        void IExposable.ExposeData() {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                // needed for ES buildings loaded from saves...
                FixupFilterChangeCallback();
            }
        }
    }
}
