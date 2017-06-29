using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ExtendedStorage
{
    public class ITab_Storage : RimWorld.ITab_Storage
    {
        public Building_ExtendedStorage Building => SelThing as Building_ExtendedStorage;  
    }
}
