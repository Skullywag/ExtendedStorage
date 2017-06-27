using System;
using System.Reflection;
using Verse;
using RimWorld;
using Harmony;

namespace ExtendedStorage
{
    class Bootstrap : Def
    {
        private const BindingFlags UniversalBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        static Bootstrap()
        {
            {
                try
                {
                    HarmonyInstance instance = HarmonyInstance.Create("com.extendedstorage.patches");
                    instance.PatchAll(Assembly.GetExecutingAssembly());
                    Log.Message($"Extended Storage :: Harmony patches successful");
                }
                catch(Exception ex)
                {
                    Log.Error("Extended Storage :: Caught exception: " + ex);
                }
            }
        }
    }
}