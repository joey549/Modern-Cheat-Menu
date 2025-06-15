using Il2CppScheduleOne.PlayerScripts.Health;
using static Modern_Cheat_Menu.Core;
using System.Reflection;
using UnityEngine;

namespace Modern_Cheat_Menu.Library
{
    public class GameplayUtils
    {
        // Enhanced socket finding method
        public Il2CppFishySteamworks.Server.ServerSocket FindBestServerSocket()
        {
            try
            {
                var transports = Resources.FindObjectsOfTypeAll<Il2CppFishySteamworks.FishySteamworks>();

                if (transports != null && transports.Length > 0)
                {
                    ModLogger.Info($"Found {transports.Length} FishySteamworks transports");
                    return transports[0]._server;
                }
                else
                {
                    ModLogger.Error("Could not find FishySteamworks transport");
                    return null;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error finding server socket: {ex.Message}");
                return null;
            }
        }

        public void SubscribeToPlayerDeathEvent()
        {
            try
            {
                var localPlayer = FindLocalPlayer();
                if (localPlayer != null)
                {
                    var playerHealth = GetPlayerHealth(localPlayer);
                    if (playerHealth != null && playerHealth.onDie != null)
                    {
                        // Create a Unity action that will be called when the player dies
                        playerHealth.onDie.AddListener(new Action(OnPlayerDeath));
                        ModLogger.Info("Successfully subscribed to player death event");
                    }
                    else
                    {
                        ModLogger.Error("Player health or onDie event is null");
                    }
                }
                else
                {
                    ModLogger.Error("Local player not found for death event subscription");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Failed to subscribe to player death event: {ex.Message}");
            }
        }

        public void OnPlayerDeath()
        {
            // Disable freecam on death.
            if (ModStateS._freeCamEnabled)
            {
                ModSetting.togglePlayerControllable(true);
                ModStateS._freeCamEnabled = false;
            }

            // Close the menu if it's open.
            if (UIs._uiVisible)
            {
                ModSetting.ToggleUI(false);
            }

            UIs._needsStyleRecreation = true;
            UIs._needsStyleRecreation = true;
        }

        // Add this method to your menu initialization
        public void ApplyLobbyPatch()
        {
            try
            {
                // Find the FishySteamworks transport
                var fishyTransports = Resources.FindObjectsOfTypeAll<Il2CppFishySteamworks.FishySteamworks>();
                if (fishyTransports != null && fishyTransports.Length > 0)
                {
                    var fishyTransport = fishyTransports[0];
                    if (fishyTransport != null)
                    {
                        // Directly change the maximum clients value
                        ModLogger.Info($"Current maximum clients: {fishyTransport._maximumClients}");
                        fishyTransport._maximumClients = 16; // Change to your desired value
                        ModLogger.Info($"Changed maximum clients to: {fishyTransport._maximumClients}");

                        // Also modify server socket if available
                        if (fishyTransport._server != null)
                        {
                            fishyTransport._server._maximumClients = 16;
                            ModLogger.Info("Also updated server socket maximum clients");
                        }

                        Notifier.ShowNotification("Lobby Size", "Maximum players increased to 16", NotificationSystem.NotificationType.Success);
                    }
                }
                else
                {
                    ModLogger.Error("Could not find FishySteamworks transport");
                    Notifier.ShowNotification("Lobby Size", "Failed to modify - transport not found", NotificationSystem.NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error applying lobby size patch: {ex.Message}");
                Notifier.ShowNotification("Lobby Size", "Failed to modify - " + ex.Message, NotificationSystem.NotificationType.Error);
            }
        }


        // Method to get a player's health component
        public static PlayerHealth GetPlayerHealth(Il2CppScheduleOne.PlayerScripts.Player player)
        {
            try
            {
                if (player != null)
                {
                    // Try to get the health component directly
                    return player.GetComponent<PlayerHealth>();
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error getting player health: {ex.Message}");
            }
            return null;
        }

        public static Il2CppScheduleOne.PlayerScripts.Player FindLocalPlayer()
        {
            try
            {
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
                if (playerList != null)
                {
                    // First check: Try to find player with IsLocalPlayer flag

                    foreach (var player in playerList)
                    {
                        if (player != null && player.IsLocalPlayer)
                        {
                            return player;
                        }
                    }

                    // Second check: Try to find player with name matching device name
                    foreach (var player in playerList)
                    {
                        if (player != null && player.name.Contains(SystemInfo.deviceName))
                        {
                            return player;
                        }
                    }

                    // Third check: Try to find player with IsOwner flag or similar ownership flag
                    foreach (var player in playerList)
                    {
                        if (player != null)
                        {
                            // Check for NetworkBehaviour and IsOwner
                            var netBehavior = player.GetComponent<Il2CppFishNet.Object.NetworkBehaviour>();
                            if (netBehavior != null && netBehavior.IsOwner)
                            {
                                return player;
                            }

                            // Check for NetworkObject and IsOwner
                            var netObject = player.GetComponent<Il2CppFishNet.Object.NetworkObject>();
                            if (netObject != null && netObject.IsOwner)
                            {
                                return player;
                            }

                            // Use reflection to check for any property that might indicate ownership
                            var properties = player.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            foreach (var prop in properties)
                            {
                                string propName = prop.Name.ToLower();
                                if ((propName.Contains("islocal") || propName.Contains("isowner") || propName.Contains("ismine")) &&
                                    prop.PropertyType == typeof(bool))
                                {
                                    try
                                    {
                                        bool value = (bool)prop.GetValue(player);
                                        if (value)
                                        {
                                            return player;
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                    }

                    // As a last resort, if we only have one player, assume it's the local player
                    if (playerList.Count == 1)
                    {
                        return playerList[0];
                    }

                    ModLogger.Error("Could not identify local player!");
                }
                else
                {
                    ModLogger.Error("PlayerList is null!");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error finding local player: {ex.Message}");
            }
            return null;
        }

        // Method to determine if a player is the local player
        public static bool IsLocalPlayer(Il2CppScheduleOne.PlayerScripts.Player player)
        {
            try
            {
                if (player != null)
                {
                    // Check if this player is the local player
                    return player.IsLocalPlayer ||
                           player.name.Contains(SystemInfo.deviceName);
                }
            }
            catch { }
            return false;
        }
    }
}
