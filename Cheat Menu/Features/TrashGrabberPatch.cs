using HarmonyLib;
using static Modern_Cheat_Menu.Core;
using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.Trash;
using Il2CppScheduleOne.UI;
using Modern_Cheat_Menu.Library;
using UnityEngine;

namespace Modern_Cheat_Menu.Patches
{
    // Inf trashgrabber
    [HarmonyPatch(typeof(Equippable_TrashGrabber), nameof(Equippable_TrashGrabber.GetCapacity))]
    public static class TrashGrabberPatch
    {
        public static bool Enabled = false;
        static bool Prefix(ref int __result)
        {
            if (Enabled)
            {
                __result = 999999;
                return false;
            }
            return true;
        }
    }

    // Auto trash grabber pickup with debug box Hold E(default use key)
    [HarmonyPatch(typeof(Equippable_TrashGrabber), nameof(Equippable_TrashGrabber.Update))]
    public static class TrashGrabbPatch
    {
        static float lastPickupTime;
        static bool Prefix(Equippable_TrashGrabber __instance)
        {
            if (__instance.DropTime != 0.01f)
                __instance.DropTime = 0.01f;

            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.E))
            {
                float now = Time.time;

                if (now - lastPickupTime >= 0.1f) // limit to every 0.1 sec
                {
                    lastPickupTime = now;

                    float radius = ModStateS.trashGrabberAutoRadius;
                    var trashgrabber = __instance.transform;

                    Vector3 boxCenter = trashgrabber.position + trashgrabber.forward * 2f;
                    Vector3 halfExtents = new Vector3(ModStateS.trashGrabberAutoRadius, ModStateS.trashGrabberAutoRadius, ModStateS.trashGrabberAutoRadius); // width, height, depth
                    Quaternion orientation = trashgrabber.rotation;
                    var hits = Physics.OverlapBox(boxCenter, halfExtents, orientation);
                    
                    if (ModStateS.drawTrashGrabberPickup)
                        GameplayUtils.DrawBoxInGame(boxCenter, halfExtents, orientation, Color.green, 0.2f);

                    foreach (var hit in hits) // this is some ugly shit I did.
                    {
                        var trashItem = GameplayUtils.GetComponentInSelfOrParents<Il2CppScheduleOne.Trash.TrashItem>(hit.transform);
                        if (trashItem != null)
                        {
                            __instance.PickupTrash(trashItem);
                            break; // 1 at a time....
                        }
                    }
                }
            }
            return true;
        }
    }

    // This is some hacky bullshit for litter/trash cap to be modded. The max that is set is the new cap.

    [HarmonyPatch(typeof(Il2CppScheduleOne.Trash.TrashManager), nameof(Il2CppScheduleOne.Trash.TrashManager.CreateAndReturnTrashItem))]
    public static class TrashLimitAddNetworkPatch
    {
        static bool Prefix()
        {
            return Il2CppScheduleOne.PlayerScripts.Player.PlayerList.Count > 1
                ? TrashLimitHelper.CanSpawnTrash()
                : true;
        }
    }

    [HarmonyPatch(typeof(Il2CppScheduleOne.Trash.TrashManager), nameof(Il2CppScheduleOne.Trash.TrashManager.CreateTrashItem))]
    [HarmonyPatch(new Type[] { typeof(string), typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(string), typeof(bool) })]
    public static class TrashLimitAddPatch
    {
        static bool Prefix()
        {
            return Il2CppScheduleOne.PlayerScripts.Player.PlayerList.Count <= 1
                ? TrashLimitHelper.CanSpawnTrash()
                : true;
        }
    }

    [HarmonyPatch(typeof(Il2CppScheduleOne.Trash.TrashManager), nameof(Il2CppScheduleOne.Trash.TrashManager.DestroyTrash))]
    [HarmonyPatch(new Type[] { typeof(Il2CppScheduleOne.Trash.TrashItem) })]
    public static class TrashLimitSubPatch
    {
        static bool Prefix(Il2CppScheduleOne.Trash.TrashManager __instance, Il2CppScheduleOne.Trash.TrashItem __0)
        {
            return !ModStateS.patchSleepTrashLimit;
        }
    }

    [HarmonyPatch(typeof(Il2CppScheduleOne.Trash.TrashItem), nameof(Il2CppScheduleOne.Trash.TrashItem.OnDestroy))]
    public static class TrashLimitSubCachePatch
    {
        static void Postfix(Il2CppScheduleOne.Trash.TrashItem __instance)
        {
            if (ModStateS.trashEstCache > 0)
                ModStateS.trashEstCache--;
        }
        static bool Prefix(Il2CppScheduleOne.Trash.TrashItem __instance)
        {
            return !ModStateS.patchSleepTrashLimit;
        }
    }

    [HarmonyPatch(typeof(Il2CppScheduleOne.UI.SleepCanvas), nameof(Il2CppScheduleOne.UI.SleepCanvas.SleepStart))]
    public static class TrashLimitStartSleepPatch
    {
        static void Prefix(Il2CppScheduleOne.UI.SleepCanvas __instance)
        {
            TrashLimitHelper.SetIsSleeping(true);
        }
    }

    [HarmonyPatch(typeof(Il2CppScheduleOne.UI.SleepCanvas), nameof(Il2CppScheduleOne.UI.SleepCanvas.SetIsOpen))]
    public static class TrashLimitStopSleepPatch
    {
        static void Postfix(Il2CppScheduleOne.UI.SleepCanvas __instance, bool __0)
        {
            if (!__0) // Make sure it's false!
                TrashLimitHelper.SetIsSleeping(false);
        }
    }

}