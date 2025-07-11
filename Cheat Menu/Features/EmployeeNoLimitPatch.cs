using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Modern_Cheat_Menu.Library;
using UnityEngine;
using UnityEngine.UIElements;

// * WIP * \\

namespace Modern_Cheat_Menu.Patches
{
    [HarmonyPatch(typeof(Il2CppScheduleOne.Property.Property), nameof(Il2CppScheduleOne.Property.Property.Start))]
    public static class EmployeeNoLimitPatch
    {
        static void Postfix(Il2CppScheduleOne.Property.Property __instance)
        {
            try
            {
                var current = __instance.EmployeeIdlePoints;
                int oldLength = current?.Length ?? 0;
                int targetLength = 50; // Set max employees (Has to create an idle point per each property)

                Matrix4x4 propertyIdleMatrix = __instance.EmployeeContainer.transform.localToWorldMatrix;

                if (current == null || current.Count == 0)
                {
                    ModLogger.Warn($"[EmployeeIdleExpandOnLoadPatch] Skipped property '{__instance.name}' due to no existing idle points.");
                    return;
                }

                Vector3 origin = current[current.Count - 1].transform.position;
                Vector3 right = Vector3.right;    // World +X
                Vector3 forward = Vector3.forward; // World +Z

                int columns = 5;
                float spacing = 2f;

                __instance.EmployeeCapacity = targetLength; // Force set all properties to max 50 employees first!

                if (oldLength >= targetLength) return;

                var expanded = new Il2CppReferenceArray<Transform>(targetLength);

                int totalNew = targetLength - oldLength;
                int totalRows = Mathf.CeilToInt((float)totalNew / columns);

                for (int i = 0; i < oldLength; i++) // copy existing
                    expanded[i] = current[i];

                Vector3 centeringOffset = -Vector3.right * ((columns - 5) * spacing / 2f) + Vector3.forward * ((totalRows - 5) * spacing / 2f);

                for (int j = oldLength; j < targetLength; j++)
                {
                    int row = (j - oldLength) / columns;
                    int col = (j - oldLength) % columns;

                    Vector3 offset = (Vector3.right * col + Vector3.forward * row) * spacing;
                    Vector3 worldPos = origin + offset + centeringOffset;

                    GameObject go = new GameObject($"IdlePoint_{j}");
                    go.transform.position = worldPos;
                    expanded[j] = go.transform;
                }

                __instance.EmployeeIdlePoints = expanded;
                ModLogger.Info($"[EmployeeIdleExpandOnLoadPatch] Expanded IdlePoints from {oldLength} to {targetLength} on property: {__instance.name}.");
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"[EmployeeIdleExpandOnLoadPatch] Failed: {ex}");
            }
        }
    }
}