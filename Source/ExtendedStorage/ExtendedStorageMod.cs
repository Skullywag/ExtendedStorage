using System;
using System.Reflection;
using Verse;
using RimWorld;
using HarmonyLib;

namespace ExtendedStorage
{
    partial class ExtendedStorageMod : Mod {
        public ExtendedStorageMod(ModContentPack content) : base(content)
        {
            try {
                Harmony instance = new Harmony("com.extendedstorage.patches");
                instance.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message($"Extended Storage {typeof(ExtendedStorageMod).Assembly.GetName().Version} - Harmony patches successful");
            } catch (Exception ex) {
                Log.Error("Extended Storage :: Caught exception: " + ex);
            }
        }
    }
        
}