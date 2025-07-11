using static Modern_Cheat_Menu.Core;
using Modern_Cheat_Menu.Library;
using Il2CppScheduleOne.GameTime;
using UnityEngine;
using Il2CppInterop.Runtime;
using Il2CppScheduleOne.Equipping;
using Modern_Cheat_Menu.Features;
using Modern_Cheat_Menu.Patches;


namespace Modern_Cheat_Menu.Commands
{
    public class WorldCommand
    {
        public static void ClearTrash(string[] args)
        {
            try
            {
                //TrashLimitSubPatch.allowTrashDestroyTemp = true;
                var command = new Il2CppScheduleOne.Console.ClearTrash();
                command.Execute(null);
                Notifier.ShowNotification("World", "Cleared all trash", NotificationSystem.NotificationType.Success);
                //TrashLimitSubPatch.allowTrashDestroyTemp = false;
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error clearing world trash: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to clear trash", NotificationSystem.NotificationType.Error);
            }
        }

        public static void EndTutorial(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.EndTutorial();
                command.Execute(null);
                Notifier.ShowNotification("Tutorial", "Tutorial ended", NotificationSystem.NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error ending tutorial: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to end tutorial", NotificationSystem.NotificationType.Error);
            }
        }

        public static void GrowPlants(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.GrowPlants();
                command.Execute(null);

                Notifier.ShowNotification("World", "All weed plants have been instantly grown!", NotificationSystem.NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error calling GrowPlants: {ex.Message}");
            }
        }

        public static void ForceGameSave(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.Save();
                command.Execute(null);
                Notifier.ShowNotification("Game", "Save completed", NotificationSystem.NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error forcing game save: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to save game", NotificationSystem.NotificationType.Error);
            }
        }

        public static void SetWorldTime(string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int time))
            {
                ModLogger.Error("Invalid scale! Please enter a number.");
                Notifier.ShowNotification("Error", "Invalid time scale value", NotificationSystem.NotificationType.Error);
                return;
            }

            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(time.ToString());

                var cmd = new Il2CppScheduleOne.Console.SetTimeCommand();
                cmd.Execute(commandList);

                Notifier.ShowNotification("World Time", $"Set world time to {time}!", NotificationSystem.NotificationType.Success);
            }

            catch (System.Exception ex)
            {
                ModLogger.Error($"Unable to set time: {ex.Message}");
            }
        }

        public static void SetTimeScale(string[] args)
        {
            if (args.Length < 1 || !float.TryParse(args[0], out float scale))
            {
                ModLogger.Error("Invalid scale! Please enter a number.");
                Notifier.ShowNotification("Error", "Invalid time scale value", NotificationSystem.NotificationType.Error);
                return;
            }

            try
            {
                // Clamp to reasonable range
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(scale.ToString());

                var cmd = new Il2CppScheduleOne.Console.SetTimeScale();
                cmd.Execute(commandList);

                Notifier.ShowNotification("Time Scale", $"Set to {scale}.", NotificationSystem.NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Unable to set time scale: {ex.Message}");
            }
        }

        public static void ToggleFreeCam(string[] args)
        {
            try
            {
                ModStateS._freeCamEnabled = !ModStateS._freeCamEnabled;

                ModSetting.ToggleUI(false);

                if (Il2CppScheduleOne.PlayerScripts.PlayerCamera.Instance != null)
                    Il2CppScheduleOne.PlayerScripts.PlayerCamera.Instance.SetCanLook(true);

                if (Il2CppScheduleOne.PlayerScripts.PlayerMovement.Instance != null)
                    Il2CppScheduleOne.PlayerScripts.PlayerMovement.Instance.canMove = false;

                if (Il2CppScheduleOne.GameInput.Instance != null && Il2CppScheduleOne.GameInput.Instance.PlayerInput != null)
                    Il2CppScheduleOne.GameInput.Instance.PlayerInput.m_InputActive = true;

                Il2CppScheduleOne.PlayerScripts.PlayerCamera.Instance.SetFreeCam(ModStateS._freeCamEnabled);

                Notifier.ShowNotification("Free Camera", ModStateS._freeCamEnabled ? "Enabled" : "Disabled", ModStateS._freeCamEnabled ? NotificationSystem.NotificationType.Success : NotificationSystem.NotificationType.Info);
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error toggling free camera: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to toggle free camera", NotificationSystem.NotificationType.Error);
            }
        }
        public static void DrawFreecamOverlay()
        {
            try
            {
                // Create a style for the freecam text
                GUIStyle freecamStyle = new GUIStyle();
                freecamStyle.normal.textColor = new Color(1f, 0.5f, 0f, 0.8f); // Orange with some transparency
                freecamStyle.fontSize = 22;
                freecamStyle.fontStyle = FontStyle.Bold;
                freecamStyle.alignment = TextAnchor.UpperCenter;
                freecamStyle.wordWrap = false;

                // Calculate position - centered at top of screen
                Rect textRect = new Rect(
                    Screen.width / 2 - 150,
                    20,
                    300,
                    30
                );

                // Draw text with a shadow effect for better visibility
                // First draw shadow
                GUI.color = new Color(0, 0, 0, 0.7f);
                GUI.Label(new Rect(textRect.x + 2, textRect.y + 2, textRect.width, textRect.height),
                    "FREECAM MODE (ESC to exit)", freecamStyle);

                // Then draw main text
                GUI.color = new Color(1f, 0.5f, 0f, 0.8f);
                GUI.Label(textRect, "FREECAM MODE (ESC to exit)", freecamStyle);

                // Reset color
                GUI.color = Color.white;

                // Controls help text with the same shadow effect
                GUIStyle helpStyle = new GUIStyle();
                helpStyle.normal.textColor = new Color(1f, 1f, 1f, 0.6f); // White with some transparency
                helpStyle.fontSize = 18;
                helpStyle.alignment = TextAnchor.UpperCenter;

                // Positioned further down
                Rect helpRect = new Rect(
                    Screen.width / 2 - 200,
                    66,
                    400,
                    60
                );

                // Draw shadow for control text
                GUI.color = new Color(0, 0, 0, 0.7f);
                GUI.Label(new Rect(helpRect.x + 2, helpRect.y + 2, helpRect.width, helpRect.height),
                    "WASD to move · Space/Ctrl to move up/down · Shift to move faster",
                    helpStyle);

                // Draw main control text
                GUI.color = new Color(1f, 1f, 1f, 0.6f);
                GUI.Label(helpRect,
                    "WASD to move · Space/Ctrl to move up/down · Shift to move faster",
                    helpStyle);

                // Reset color
                GUI.color = Color.white;
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error drawing freecam overlay: {ex.Message}");
            }
        }

        public static void SetTimeLength(string[] args)
        {
            try
            {
                float timeLength;
                bool success = float.TryParse(args[0], out timeLength);
                if (!success)
                    return;
                TimeManager.instance.TimeProgressionMultiplier = timeLength;

                Notifier.ShowNotification("Set Time Length", $"TimeProgressionMultiplier has been set to: {TimeManager.instance.TimeProgressionMultiplier}" ,NotificationSystem.NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error Set Time Length: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to Set Time Length", NotificationSystem.NotificationType.Error);
            }
        }
        public static void SetWorldTrashLimit(string[] args)
        {
            int trashLimit;
            bool success = int.TryParse(args[0], out trashLimit);
            if (!success)
                return;

            ModStateS.trashMaxLimit = trashLimit;
            Notifier.ShowNotification("Max Trash Limit", $"Max Trash Limit was set to: {ModStateS.trashMaxLimit}", NotificationSystem.NotificationType.Success);
        }
        public static void SetAllSpawnRemovalVolumes(string[] args)
        {
            if (args.Length < 3)
                return;

            if (!int.TryParse(args[0], out int trashLimit) ||
                !float.TryParse(args[1], out float TrashSpawnChance) ||
                !float.TryParse(args[2], out float RemovealChance))
                return;

            int spawnvol = 0;
            int removol = 0;

            foreach (var trashSpawnVol in UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.Trash.TrashSpawnVolume>())
            {
                trashSpawnVol.TrashLimit = trashLimit;
                trashSpawnVol.TrashSpawnChance = TrashSpawnChance;
                spawnvol++;
            }

            foreach (var trashRemovalVol in UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.Trash.TrashRemovalVolume>())
            {
                trashRemovalVol.RemovalChance = RemovealChance;
                removol++;
            }

            Notifier.ShowNotification("All Spawn Volumes", $"TrashLimit Set To: {trashLimit} | Spawn Chance Set To: {TrashSpawnChance} on {spawnvol} volumes.", NotificationSystem.NotificationType.Success);
            Notifier.ShowNotification("All Removal Volumes", $"Removal Chance Set To: {RemovealChance} on {removol} volumes.", NotificationSystem.NotificationType.Success);
        }

    }
}
