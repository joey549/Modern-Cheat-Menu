using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;
using static Modern_Cheat_Menu.Core;
using Modern_Cheat_Menu.Library;
using System.Collections;

namespace Modern_Cheat_Menu.Commands
{
    public class SpawnCommand
    {
        public static void SpawnVehicle(string[] args)
        {
            try
            {
                string vehicle = "cheetah"; // Default to shitbox if no valid argument is provided

                if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
                {
                    // Take the first argument as the vehicle type
                    vehicle = args[0].ToLowerInvariant(); // Convert to lowercase
                }

                // Create command parameter list
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(vehicle);

                // Execute the command
                var cmd = new Il2CppScheduleOne.Console.SpawnVehicleCommand();
                cmd.Execute(commandList);

                Notifier.ShowNotification("Vehicle", $"Spawned {vehicle}", NotificationSystem.NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error spawning vehicle: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to spawn vehicle", NotificationSystem.NotificationType.Error);
            }
        }

        public static IEnumerator SpawnItemViaConsoleCoroutine(string itemId, int quantity, Il2CppScheduleOne.ItemFramework.EQuality quality)
        {
            bool success = false;
            Exception caughtException = null;

            // Initial add item command
            var addList = new Il2CppSystem.Collections.Generic.List<string>();
            addList.Add(itemId);
            addList.Add(quantity.ToString());

            try
            {
                new Il2CppScheduleOne.Console.AddItemToInventoryCommand().Execute(addList);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            yield return null; // Wait one frame

            if (caughtException != null)
            {
                ModLogger.Error($"Add item failed: {caughtException}");
                yield break;
            }

            // Final cleanup
            try
            {
                CursorManager.Instance.SetCursorAppearance(CursorManager.ECursorType.Default);
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Failed to spawn the item: {ex.Message}");
            }
        }

        // Updated spawn caller
        public static void SpawnItemViaConsole(string itemId, int quantity, Il2CppScheduleOne.ItemFramework.EQuality quality)
        {
            MelonCoroutines.Start(SpawnItemViaConsoleCoroutine(itemId, quantity, quality));
        }

        // For UI buttons (called from buttons in the UI)
        public static void PackageProductCommand(string packageType)
        {
            try
            {
                if (string.IsNullOrEmpty(ModStateS._selectedItemId))
                {
                    Notifier.ShowNotification("Package Product", "No item selected", NotificationSystem.NotificationType.Warning);
                    return;
                }

                // Validate package type
                if (packageType != "baggie" && packageType != "jar")
                {
                    packageType = "baggie"; // Default to baggie
                }

                // Create a parameter list for the command
                var args = new Il2CppSystem.Collections.Generic.List<string>();
                args.Add(packageType);

                // Execute the PackageProduct command
                var cmd = new Il2CppScheduleOne.Console.PackageProduct();
                cmd.Execute(args);

                Notifier.ShowNotification("Package Product", $"Item packaged in {packageType}", NotificationSystem.NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error packaging product: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to package product", NotificationSystem.NotificationType.Error);
            }
        }

        public static void SetItemQuality(Il2CppScheduleOne.ItemFramework.EQuality quality)
        {
            try
            {
                // Create parameter list with just the quality value
                var qualityList = new Il2CppSystem.Collections.Generic.List<string>();
                qualityList.Add(((int)quality).ToString());

                // Execute with the parameter list
                var cmd = new Il2CppScheduleOne.Console.SetQuality();
                cmd.Execute(qualityList);

                Notifier.ShowNotification("Item Quality", $"Set to {quality} quality", NotificationSystem.NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error setting item quality: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to set item quality", NotificationSystem.NotificationType.Error);
            }
        }
    }
}
