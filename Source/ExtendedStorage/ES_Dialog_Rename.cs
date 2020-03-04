using UnityEngine;
using Verse;

namespace ExtendedStorage
{
    public class ES_Dialog_Rename : Dialog_Rename
    {
        private Building_ExtendedStorage building;

        public ES_Dialog_Rename(Building_ExtendedStorage building) => this.building = building;
        protected override void SetName(string name) => building.label = name;
    }
}