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
<<<<<<< HEAD
                try
                {
                    HarmonyInstance instance = HarmonyInstance.Create("com.extendedstorage.patches");
                    instance.PatchAll(Assembly.GetExecutingAssembly());
                    Log.Message($"Extended Storage :: Harmony patches successful");               
=======
                try
                {
                    HarmonyInstance.Create("com.extendedstorage.patches").PatchAll(Assembly.GetExecutingAssembly());
                    Log.Message("Extended Storage :: Harmony patches successfull");
>>>>>>> itemsplurge
                }
                catch(Exception ex)
                {
                    Log.Error("Extended Storage :: Caught exception: " + ex);
                }
            }
        }
    }
}