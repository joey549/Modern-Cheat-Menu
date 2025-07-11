using Il2CppScheduleOne.UI.Stations;
using HarmonyLib;
using UnityEngine;

namespace Modern_Cheat_Menu.Features
{
    [HarmonyPatch(typeof(Il2CppScheduleOne.PlayerScripts.PlayerInventory), nameof(Il2CppScheduleOne.PlayerScripts.PlayerInventory.SetEquippable))]
    public static class PlayerInventorySelectionPatch
    {
        static void Postfix(Il2CppScheduleOne.PlayerScripts.PlayerInventory __instance, Il2CppScheduleOne.Equipping.Equippable __0)
        {
            // To be used????? maybe???
        }
    }
}
