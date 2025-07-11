using HarmonyLib;
using static Modern_Cheat_Menu.Core;
using Modern_Cheat_Menu.Library;
using System.Reflection;

namespace Modern_Cheat_Menu.Features
{
    public class CrosshairHandler
    {
        // Add this method to handle toggling the feature
        public static void ToggleAlwaysVisibleCrosshair(string[] args)
        {
            try
            {
                ModStateS._forceCrosshairAlwaysVisible = !ModStateS._forceCrosshairAlwaysVisible;

                // Apply patch if turning on, remove patch if turning off
                if (ModStateS._forceCrosshairAlwaysVisible)
                {
                    ApplyCrosshairPatch();
                    Notifier.ShowNotification("Crosshair", "Always visible crosshair enabled", NotificationSystem.NotificationType.Success);
                }
                else
                {
                    RemoveCrosshairPatch();
                    Notifier.ShowNotification("Crosshair", "Always visible crosshair disabled", NotificationSystem.NotificationType.Info);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error toggling always visible crosshair: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to toggle crosshair visibility", NotificationSystem.NotificationType.Error);
            }
        }

        // Add the Harmony patch setup
        private static void ApplyCrosshairPatch()
        {
            try
            {
                // Find the HUD.SetCrosshairVisible method
                var hudType = typeof(Il2CppScheduleOne.UI.HUD);
                var setCrosshairMethod = hudType.GetMethod("SetCrosshairVisible",BindingFlags.Public | BindingFlags.Instance);

                if (setCrosshairMethod == null)
                {
                    ModLogger.Error("Could not find SetCrosshairVisible method!");
                    return;
                }

                // Create and apply the prefix patch
                var patchMethod = typeof(Core).GetMethod("CrosshairVisibilityPatch",BindingFlags.Static | BindingFlags.NonPublic);

                if (patchMethod == null)
                {
                    ModLogger.Error("CrosshairVisibilityPatch method not found!");
                    return;
                }

                ModSetting._harmony.Patch(setCrosshairMethod, prefix: new HarmonyLib.HarmonyMethod(patchMethod));
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error applying crosshair patch: {ex.Message}");
            }
        }

        // Method to remove the patch
        private static void RemoveCrosshairPatch()
        {
            try
            {
                // Find the HUD.SetCrosshairVisible method
                var hudType = typeof(Il2CppScheduleOne.UI.HUD);
                var setCrosshairMethod = hudType.GetMethod("SetCrosshairVisible",BindingFlags.Public | BindingFlags.Instance);

                if (setCrosshairMethod == null)
                {
                    ModLogger.Error("Could not find SetCrosshairVisible method!");
                    return;
                }

                // Remove the patch
                ModSetting._harmony.Unpatch(setCrosshairMethod, HarmonyPatchType.Prefix, ModSetting._harmony.Id);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error removing crosshair patch: {ex.Message}");
            }
        }

        // The Harmony prefix patch method (must be static)
        private static bool CrosshairVisibilityPatch(ref bool vis)
        {
            // If called with false, modify to true to keep crosshair visible
            if (!vis)
                vis = true;
            // Return true to allow original method to run (with our modified parameter)
            return true;
        }
    }
}
