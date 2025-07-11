using Modern_Cheat_Menu.Library;
using static Il2CppMS.Internal.Xml.XPath.QueryBuilder;
using static Modern_Cheat_Menu.Core;

namespace Modern_Cheat_Menu.Commands
{
    public class PropertyCommands
    {
        public static void ForceOwnProperty(string[] args)
        {
            try
            {
                string property = "storageunit";

                if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
                    property = args[0].ToLowerInvariant();

                var cmd = new Il2CppScheduleOne.Console.SetPropertyOwned();
                cmd.Execute(CommandCore.ToCommandList(property));

                Notifier.ShowSuccess("ForceOwnProperty", $"Set own property: {property}");
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"ForceOwnProperty failed: {ex.Message}");
                Notifier.ShowError("Error", "Failed to set own property");
            }
        }

        public static void SetTrashCanSettings(string[] args)
        {
            if (args.Length < 1)
            {
                ModLogger.Error("amount required!");
                Notifier.ShowNotification("Error", "An amount is required", NotificationSystem.NotificationType.Error);
                return;
            }

            // Try to parse the first argument into an integer
            if (!int.TryParse(args[0], out int radius))
            {
                Notifier.ShowNotification("Error", "Invalid radius. Please enter a valid number", NotificationSystem.NotificationType.Error);
                return;
            }
            if (!int.TryParse(args[1], out int width))
            {
                Notifier.ShowNotification("Error", "Invalid width. Please enter a valid number", NotificationSystem.NotificationType.Error);
                return;
            }
            if (!int.TryParse(args[2], out int capcity))
            {
                Notifier.ShowNotification("Error", "Invalid capcity. Please enter a valid number", NotificationSystem.NotificationType.Error);
                return;
            }

            try
            {

                int modified = 0;
                // Get all trash container items in the scene
                var allTrashContainers = UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.ObjectScripts.TrashContainerItem>();

                foreach (var container in allTrashContainers)
                {
                    container.calculatedPickupRadius = radius;
                    container.PickupSquareWidth = width;

                    // Get the associated TrashContainer component on the same GameObject
                    var capacityComponent = container.GetComponent<Il2CppScheduleOne.Trash.TrashContainer>();
                    if (capacityComponent != null)
                    {
                        capacityComponent.TrashCapacity = capcity;
                    }

                    modified++;
                }

                Notifier.ShowNotification("Success", $"[TrashSettings]TrashContainers set: radius={radius}, width={width}, capacity={capcity}", NotificationSystem.NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                Notifier.ShowNotification("Error", $"TrashSettings Failed to set. ex: {ex}", NotificationSystem.NotificationType.Error);
            }
        }

        public static void SetDumpsterSettings(string[] args)
        {
            if (args.Length < 1)
            {
                ModLogger.Error("amount required!");
                Notifier.ShowNotification("Error", "An amount is required", NotificationSystem.NotificationType.Error);
                return;
            }

            try
            {

                int spawnvol = 0;
                int removol = 0;
                // Get all trash container items in the scene
                var allTrashSpawnVolume = UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.Trash.TrashSpawnVolume>();
                var allTrashRemovalVolume = UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.Trash.TrashRemovalVolume>();

                foreach (var trashSpawnVol in allTrashSpawnVolume)
                {
                    trashSpawnVol.TrashLimit = 150;
                    trashSpawnVol.TrashSpawnChance = 1f;
                    spawnvol++;
                }

                foreach (var trashRemovalVol in allTrashRemovalVolume)
                {
                    trashRemovalVol.RemovalChance = 0;
                    removol++;
                }

                Notifier.ShowNotification("Success", $"[Dumpsters] All Spawn Volumes={spawnvol}, All Remover Chance={removol}", NotificationSystem.NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                Notifier.ShowNotification("Error", $"TrashSettings Failed to set. ex: {ex}", NotificationSystem.NotificationType.Error);
            }
        }

        public static void AddEmployeeToProperty(string[] args)
        {
            if (args.Length < 2)
            {
                Notifier.ShowNotification("AddEmployee", "Usage: <property> <employeeCode>", NotificationSystem.NotificationType.Error);
                return;
            }

            string employeeCode = args[0]; // like "Botanist", "Packager", etc.
            string propertyCode = args[1].ToLowerInvariant(); // like "dockswarehouse"

            try
            {
                var cmd = new Il2CppScheduleOne.Console.AddEmployeeCommand();
                var list = new Il2CppSystem.Collections.Generic.List<string>();
                list.Add(propertyCode);
                list.Add(employeeCode);
                cmd.Execute(list);

                Notifier.ShowNotification("AddEmployee", $"Added {employeeCode} to {propertyCode}", NotificationSystem.NotificationType.Success);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Failed to add employee: {ex}");
                Notifier.ShowNotification("Error", "Failed to add employee", NotificationSystem.NotificationType.Error);
            }
        }
    }
}






