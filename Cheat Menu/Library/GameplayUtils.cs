using static Modern_Cheat_Menu.Core;
using static Modern_Cheat_Menu.Commands.WorldCommand;
using Il2CppScheduleOne.PlayerScripts.Health;
using System.Reflection;
using UnityEngine;
using Modern_Cheat_Menu.Commands;

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
                        ModLogger.Error("Player health or onDie event is null");
                }
                else
                    ModLogger.Error("Local player not found for death event subscription");
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Failed to subscribe to player death event: {ex.Message}");
            }
        }

        public void OnPlayerDeath()
        {
            if (ModStateS._freeCamEnabled)
                ToggleFreeCam(new string[] { "banana" });

            if (UIs._uiVisible)
                ModSetting.ToggleUI(false);

            UIs._needsStyleRecreation = true;
            UIs._needsStyleRecreation = true;
        }

        public void ApplyLobbyPatch()
        {
            try
            {
                var fishyTransports = Resources.FindObjectsOfTypeAll<Il2CppFishySteamworks.FishySteamworks>();
                if (fishyTransports != null && fishyTransports.Length > 0)
                {
                    var fishyTransport = fishyTransports[0];
                    if (fishyTransport != null)
                    {
                        ModLogger.Info($"Current maximum clients: {fishyTransport._maximumClients}"); // max b4
                        fishyTransport._maximumClients = 16; // Change to your desired value
                        ModLogger.Info($"Changed maximum clients to: {fishyTransport._maximumClients}"); // max after

                        if (fishyTransport._server != null)
                        {
                            fishyTransport._server._maximumClients = 16; // modify server socket
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
                    return player.GetComponent<PlayerHealth>();
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

                if (playerList == null || playerList.Count == 0)
                {
                    ModLogger.Error("PlayerList is null or empty!");
                    return null;
                }

                foreach (var player in playerList)
                    if (player?.IsLocalPlayer == true) // ?isLocal?
                        return player;

                foreach (var player in playerList)
                    if (player != null && player.name.Contains(SystemInfo.deviceName)) // ?Device match?
                        return player;

                foreach (var player in playerList)
                {
                    if (player == null)
                        continue;

                    var netBehavior = player.GetComponent<Il2CppFishNet.Object.NetworkBehaviour>();
                    if (netBehavior?.IsOwner == true) // Network Ownership
                        return player;

                    var netObject = player.GetComponent<Il2CppFishNet.Object.NetworkObject>();
                    if (netObject?.IsOwner == true) // Network Ownship
                        return player;

                    // Reflective ownership fallback
                    var properties = player.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var prop in properties)
                    {
                        if (prop.PropertyType == typeof(bool))
                        {
                            string name = prop.Name.ToLower();
                            if (name.Contains("islocal") || name.Contains("isowner") || name.Contains("ismine"))
                            {
                                try
                                {
                                    if ((bool)prop.GetValue(player))
                                        return player;
                                }
                                catch { /* Ignore property errors */ }
                            }
                        }
                    }
                }

                if (playerList.Count == 1)
                    return playerList[0]; // Assume local

                ModLogger.Error("Could not identify local player.");
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
                    // is local player ??
                    return player.IsLocalPlayer ||
                           player.name.Contains(SystemInfo.deviceName);
                }
            }
            catch { }
            return false;
        }

        public static class LocalPlayerCache
        {
            private static Il2CppScheduleOne.PlayerScripts.Player _cached;

            public static Il2CppScheduleOne.PlayerScripts.Player Instance
            {
                get
                {
                    if (_cached == null)
                        _cached = GameplayUtils.FindLocalPlayer();
                    return _cached;
                }
            }
        }

        // Mainly for debugging
        public static void DrawLineInGame(Vector3 start, Vector3 end, Color color, float duration = 0.1f)
        {
            var go = new GameObject("DebugLine");
            var lr = go.AddComponent<LineRenderer>();

            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = color;
            lr.startWidth = lr.endWidth = 0.02f;
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);

            UnityEngine.Object.Destroy(go, duration);
        }

        public static GameObject CreateDebugSphere(Vector3 pos, Color color, float duration = 1f)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = pos;
            sphere.transform.localScale = Vector3.one * 0.2f;
            var renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;

            UnityEngine.Object.Destroy(sphere, duration);
            return sphere;
        }
        
        public static void DrawBoxInGame(Vector3 center, Vector3 halfExtents, Quaternion rotation, Color color, float duration = 0.1f)
        {
            Vector3[] corners = new Vector3[8];
            Vector3 extents = halfExtents;

            // Calculate corners in local space
            corners[0] = center + rotation * new Vector3(-extents.x, -extents.y, -extents.z);
            corners[1] = center + rotation * new Vector3(extents.x, -extents.y, -extents.z);
            corners[2] = center + rotation * new Vector3(extents.x, -extents.y, extents.z);
            corners[3] = center + rotation * new Vector3(-extents.x, -extents.y, extents.z);

            corners[4] = center + rotation * new Vector3(-extents.x, extents.y, -extents.z);
            corners[5] = center + rotation * new Vector3(extents.x, extents.y, -extents.z);
            corners[6] = center + rotation * new Vector3(extents.x, extents.y, extents.z);
            corners[7] = center + rotation * new Vector3(-extents.x, extents.y, extents.z);

            // Bottom rectangle
            DrawLineInGame(corners[0], corners[1], color, duration);
            DrawLineInGame(corners[1], corners[2], color, duration);
            DrawLineInGame(corners[2], corners[3], color, duration);
            DrawLineInGame(corners[3], corners[0], color, duration);

            // Top rectangle
            DrawLineInGame(corners[4], corners[5], color, duration);
            DrawLineInGame(corners[5], corners[6], color, duration);
            DrawLineInGame(corners[6], corners[7], color, duration);
            DrawLineInGame(corners[7], corners[4], color, duration);

            // Vertical edges
            DrawLineInGame(corners[0], corners[4], color, duration);
            DrawLineInGame(corners[1], corners[5], color, duration);
            DrawLineInGame(corners[2], corners[6], color, duration);
            DrawLineInGame(corners[3], corners[7], color, duration);
        }
        public static T GetComponentInSelfOrParents<T>(Transform start) where T : Component
        {
            var current = start;
            while (current != null)
            {
                var comp = current.GetComponent<T>();
                if (comp != null)
                    return comp;

                current = current.parent;
            }
            return null;
        }
    }

    // Trash Limit Mod Helper
    public static class TrashLimitHelper
    {
        public static bool CanSpawnTrash()
        {
            if (ModStateS.trashEstCache > ModStateS.trashMaxLimit)
                return false;

            ModStateS.trashEstCache++;
            return true;
        }
        public static void SetIsSleeping(bool _isSleeping)
        {
            ModStateS.patchSleepTrashLimit = _isSleeping;
            ModLogger.Info($"[Sleep State] Update New State | is sleeping: {_isSleeping}");
        }
    }

}
