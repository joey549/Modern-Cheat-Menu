using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Modern_Cheat_Menu.Library;
using UnityEngine;

namespace Modern_Cheat_Menu.Patches
{
    [HarmonyPatch(typeof(Il2CppScheduleOne.Property.Property), nameof(Il2CppScheduleOne.Property.Property.SetOwned))] // just in-case
    [HarmonyPatch(typeof(Il2CppScheduleOne.Property.Property), nameof(Il2CppScheduleOne.Property.Property.Start))]
    public static class EmployeeNoLimitPatch
    {
        static void Postfix(Il2CppScheduleOne.Property.Property __instance)
        {
            try
            {
                var current = __instance.EmployeeIdlePoints;
                int oldLength = current?.Length ?? 0;
                int targetLength = 50; // 50 is large enough... each property can have 50 employees

                Matrix4x4 propertyIdleMatrix = __instance.EmployeeContainer.transform.localToWorldMatrix;

                Vector3 origin = propertyIdleMatrix.GetColumn(3); // mainPos
                Vector3 right = propertyIdleMatrix.GetColumn(0).normalized;  // X
                Vector3 up = propertyIdleMatrix.GetColumn(1).normalized;  // Y
                Vector3 fwd = propertyIdleMatrix.GetColumn(2).normalized;  // Z

                float spacing = 0.5f; // needs adjusting

                __instance.EmployeeCapacity = targetLength; // Force set all properties to max 50 employees first!

                if (oldLength >= targetLength) return;

                var expanded = new Il2CppReferenceArray<Transform>(targetLength);

                // Copy existing
                for (int i = 0; i < oldLength; i++)
                    expanded[i] = current[i];

                for (int j = oldLength; j < targetLength; j++)
                {
                    int row = j / 5;
                    int col = j % 5;

                    Vector3 localOffset = (right * col + fwd * row) * spacing;
                    Vector3 worldPos = origin + localOffset;
                    
                    GameObject go = new GameObject($"GameObject ({j})");
                    go.transform.position = worldPos;
                    expanded[j] = go.transform;
                }

                __instance.EmployeeIdlePoints = expanded; // New objects + existing
                ModLogger.Info($"[EmployeeIdleExpandOnLoadPatch] Expanded IdlePoints from {oldLength} to {targetLength}.");
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"[EmployeeIdleExpandOnLoadPatch] Failed: {ex}");
            }
        }
    }
}