using HarmonyLib;
using Il2CppScheduleOne.Equipping;

namespace Modern_Cheat_Menu.Patches
{
    [HarmonyPatch(typeof(Equippable_TrashGrabber), nameof(Equippable_TrashGrabber.GetCapacity))]
    public static class TrashGrabberPatch
    {
        public static bool Enabled = false;

        static bool Prefix(ref int __result)
        {
            if (Enabled)
            {
                __result = 999999;
                return false; // mnodded method
            }

            return true; // original method
        }
    }
}
