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
                    HarmonyInstance.Create("com.extendedstorage.patches").PatchAll(Assembly.GetExecutingAssembly());
                    Log.Message("Extended Storage :: Harmony patches successfull");
                }
                catch(Exception ex)
                {
                    Log.Error("Extended Storage :: Caught exception: " + ex);
                }
            }
        }
    }
}