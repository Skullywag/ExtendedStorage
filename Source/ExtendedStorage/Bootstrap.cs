using System;
using System.Reflection;
using Verse;
using RimWorld;
using Harmony;

namespace ExtendedStorage
{
    [StaticConstructorOnStartup]
    class Bootstrap : Def
    {
        private const BindingFlags UniversalBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        static Bootstrap()
        {
            {
                try
                {
                    MethodInfo method1 = typeof(Thing).GetMethod("SpawnSetup", BindingFlags.Instance | BindingFlags.Public);
                    MethodInfo transpiler = typeof(Patches).GetMethod("Transpiler");
                    HarmonyInstance.Create("com.extendedstorage.patches").Patch(method1, null, null, new HarmonyMethod(transpiler));
                    Log.Message("Extended Storage :: Harmony patch successful (" + method1 + ") Transpiler (gets rid of the unnecessary stack count truncation)");
                }
                catch(Exception ex)
                {
                    Log.Error("Extended Storage :: Caught exception: " + ex);
                }
            }
        }
    }
}