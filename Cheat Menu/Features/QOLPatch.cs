using Il2CppScheduleOne.ObjectScripts;
using HarmonyLib;
using UnityEngine;
using Il2CppScheduleOne.UI.Stations;
using Il2CppScheduleOne.NPCs.Behaviour;
using static Il2CppScheduleOne.Map.PoliceStation;

namespace Modern_Cheat_Menu.Features
{

    public class QOLStateS
    {
        public static float qolPatchLastTime;
    }

    // * Hold E(default use key) to exit Mixing Station | Brick Press | Cauldron | Chemistry Station | Drying Rack

    [HarmonyPatch(typeof(MixingStationCanvas), nameof(MixingStationCanvas.Open))]
    public static class MixingStationCanvasOpenQOLPatch
    {
        static void Postfix(MixingStationCanvas __instance)
        {
            QOLStateS.qolPatchLastTime = Time.time + 1.5f;
        }
    }

    [HarmonyPatch(typeof(Il2CppScheduleOne.UI.Stations.MixingStationCanvas), nameof(Il2CppScheduleOne.UI.Stations.MixingStationCanvas.Update))]
    public static class MixingStationQOLPatch
    {
        static void Postfix(Il2CppScheduleOne.UI.Stations.MixingStationCanvas __instance)
        {
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.E) && __instance.isOpen)
                if (Time.time >= QOLStateS.qolPatchLastTime)
                    __instance.Close(true);
        }
    }

    [HarmonyPatch(typeof(BrickPress), nameof(BrickPress.Open))]
    public static class BrickPressOpenQOLPatch
    {
        static void Postfix(BrickPress __instance)
        {
            QOLStateS.qolPatchLastTime = Time.time + 1.5f;
        }
    }

    [HarmonyPatch(typeof(BrickPressCanvas), nameof(BrickPressCanvas.Update))]
    public static class BrickPressQOLPatch
    {
        static void Postfix(BrickPressCanvas __instance)
        {
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.E) && __instance.isOpen)
                if (Time.time >= QOLStateS.qolPatchLastTime)
                    __instance.Press.Close();
        }
    }

    [HarmonyPatch(typeof(Cauldron), nameof(Cauldron.Open))]
    public static class CauldronOpenQOLPatch
    {
        static void Postfix(Cauldron __instance)
        {
            QOLStateS.qolPatchLastTime = Time.time + 1.5f;
        }
    }

    [HarmonyPatch(typeof(CauldronCanvas), nameof(CauldronCanvas.Update))]
    public static class CauldronQOLPatch
    {
        
        static void Postfix(CauldronCanvas __instance)
        {
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.E) && __instance.isOpen)
                if (Time.time >= QOLStateS.qolPatchLastTime)
                    __instance.Cauldron.Close();
        }
    }

    [HarmonyPatch(typeof(DryingRack), nameof(DryingRack.Open))]
    public static class DryingRackOpenQOLPatch
    {
        static void Postfix(DryingRack __instance)
        {
            QOLStateS.qolPatchLastTime = Time.time + 1.5f;
        }
    }

    [HarmonyPatch(typeof(DryingRackCanvas), nameof(DryingRackCanvas.Update))]
    public static class DryingRackQOLPatch
    {

        static void Postfix(DryingRackCanvas __instance)
        {
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.E) && __instance.isOpen)
                if (Time.time >= QOLStateS.qolPatchLastTime)
                    __instance.Rack.Close();
        }
    }

    [HarmonyPatch(typeof(PackagingStation), nameof(PackagingStation.Open))]
    public static class PackagingStationOpenQOLPatch
    {
        static void Postfix(PackagingStation __instance)
        {
            QOLStateS.qolPatchLastTime = Time.time + 1.5f;
        }
    }

    [HarmonyPatch(typeof(PackagingStationCanvas), nameof(PackagingStationCanvas.Update))]
    public static class PackagingStationUpdateQOLPatch
    {
        static void Postfix(PackagingStationCanvas __instance)
        {
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.E) && __instance.isOpen)
                if (Time.time >= QOLStateS.qolPatchLastTime)
                    __instance.PackagingStation.Close();
        }
    }

    [HarmonyPatch(typeof(ChemistryStation), nameof(ChemistryStation.Open))]
    public static class ChemistryStationOpenQOLPatch
    {
        static void Postfix(ChemistryStation __instance)
        {
            QOLStateS.qolPatchLastTime = Time.time + 1.5f;
        }
    }

    [HarmonyPatch(typeof(ChemistryStationCanvas), nameof(ChemistryStationCanvas.Update))]
    public static class ChemistryStationQOLPatch
    {
        static void Postfix(ChemistryStationCanvas __instance)
        {
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.E) && __instance.isOpen)
                if (Time.time >= QOLStateS.qolPatchLastTime)
                    __instance.ChemistryStation.Close();
        }
    }

}
