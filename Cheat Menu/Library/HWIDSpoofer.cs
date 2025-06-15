using MelonLoader;
using System.Reflection;
using UnityEngine.Device;
using static Modern_Cheat_Menu.Core;

namespace Modern_Cheat_Menu.Library
{
    public class HWIDSpoofer
    {
        #region HWID Spoofer
        public static string _generatedHwid;

        private void ViewCurrentHWID(string[] args)
        {
            try
            {
                Notifier.ShowNotification("HWID", $"Current HWID: {_generatedHwid}", NotificationSystem.NotificationType.Info);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error viewing HWID: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to view HWID", NotificationSystem.NotificationType.Error);
            }
        }

        public void GenerateNewHWID(string[] args)
        {
            try
            {
                // Use the existing HWID generation logic from InitializeHwidPatch
                var random = new System.Random(Environment.TickCount);
                var bytes = new byte[SystemInfo.deviceUniqueIdentifier.Length / 2];
                random.NextBytes(bytes);
                var newId = string.Join("", bytes.Select(it => it.ToString("x2")));

                // Update the preferences entry
                var hwidEntry = MelonPreferences.CreateEntry("CheatMenu", "HWID", "", is_hidden: true);
                hwidEntry.Value = newId;

                // Update the static _generatedHwid
                _generatedHwid = newId;

                Notifier.ShowNotification("HWID", $"Generated new HWID", NotificationSystem.NotificationType.Success);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error generating new HWID: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to generate new HWID", NotificationSystem.NotificationType.Error);
            }
        }

        public static void InitializeHwidPatch()
        {
            try
            {
                ModLogger.Info("Initializing HWID Patch...");

                // Always generate a new HWID on each game load
                var random = new System.Random(Environment.TickCount);
                var bytes = new byte[SystemInfo.deviceUniqueIdentifier.Length / 2];
                random.NextBytes(bytes);
                var newId = string.Join("", bytes.Select(it => it.ToString("x2")));

                // Save the new HWID to MelonPreferences
                var hwidEntry = MelonPreferences.CreateEntry("CheatMenu", "HWID", newId, is_hidden: true);

                // Store the generated HWID
                _generatedHwid = newId;

                var originalMethod = typeof(SystemInfo).GetProperty("deviceUniqueIdentifier")?.GetGetMethod();
                var patchMethod = typeof(HWIDSpoofer).GetMethod("GetDeviceIdPatch", BindingFlags.Static | BindingFlags.NonPublic);

                if (originalMethod == null)
                    ModLogger.Error("HWID patch: Failed to resolve original getter method for deviceUniqueIdentifier.");
                if (patchMethod == null)
                    ModLogger.Error("HWID patch: Failed to find HWIDSpoofer.GetDeviceIdPatch method.");

                if (originalMethod == null || patchMethod == null)
                    throw new Exception("HWID patch failed: Could not resolve one or more methods.");



                ModSetting._harmony.Patch(originalMethod, new HarmonyLib.HarmonyMethod(patchMethod));

                ModLogger.Info("HWID Patch integrated successfully");
                ModLogger.Info($"New HWID: {newId}");
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Failed to initialize HWID patch: {ex.Message}");
            }
        }

        // Harmony patch for SystemInfo.deviceUniqueIdentifier
        private static bool GetDeviceIdPatch(ref string __result)
        {
            __result = _generatedHwid;
            return false; // Skip the original method
        }
        #endregion
    }
}
