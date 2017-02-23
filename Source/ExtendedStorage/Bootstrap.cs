using System;
using System.Reflection;
using Verse;
using RimWorld;

namespace ExtendedStorage
{
    class Bootstrap : Def
    {
        // CommunityCoreLibrary.DetourInjector
        private const BindingFlags UniversalBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        static Bootstrap()
        {
            {
                MethodInfo method1= typeof(Thing).GetMethod("SpawnSetup", BindingFlags.Instance | BindingFlags.Public);
                MethodInfo method2 = typeof(_Thing_ExtendedStorage).GetMethod("_SpawnSetup", BindingFlags.Instance | BindingFlags.NonPublic);
                Log.Message("Attempting detour from " + method1 + "to " + method2);
                if (!Detours.TryDetourFromTo(method1, method2))
                {
                    Log.Error("Extended Storage Detour failed");
                    return;
                }
                Log.Message("Extended Storage Detour Successful");
            }

            Assembly Assembly_CSharp = Assembly.Load("Assembly-CSharp.dll");
        }
    }
}