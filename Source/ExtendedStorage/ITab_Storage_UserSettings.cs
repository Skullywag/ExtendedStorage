using ExtendedStorage.Patches;
using RimWorld;
using Verse;

namespace ExtendedStorage
{
    public class ITab_Storage : RimWorld.ITab_Storage
    {
        public Building_ExtendedStorage Building => SelThing as Building_ExtendedStorage;

        protected override IStoreSettingsParent SelStoreSettingsParent{
            get
            {
                if (DebugSettings.godMode && ITab_Storage_FillTab.showStoreSettings)
                    return base.SelStoreSettingsParent;

                return Building;
            }
        }
    }
}