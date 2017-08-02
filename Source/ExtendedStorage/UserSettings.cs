using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;

namespace ExtendedStorage {
    public class UserSettings : StorageSettings {
        private readonly IUserSettingsOwner _owner;

        public UserSettings(IUserSettingsOwner owner) : base(owner)
        {
            _owner = owner;
        }


        public void NotifyOwnerSettingsChanged() 
        {
            _owner.Notify_UserSettingsChanged();
        }

    }
}
