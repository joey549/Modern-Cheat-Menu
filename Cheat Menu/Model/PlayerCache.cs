using Il2CppFishNet.Object;
using Il2CppScheduleOne.Equipping;
using Modern_Cheat_Menu.Library;
using UnityEngine;

namespace Modern_Cheat_Menu.Model
{
    public static class PlayerCache
    {
        public static List<OnlinePlayerInfo> _onlinePlayers = new List<OnlinePlayerInfo>();
        public static float _lastPlayerRefreshTime = 0f;
        public const float PLAYER_REFRESH_INTERVAL = 7f;
        public static NetworkObject _cachedPlayerObject = null;
        public static Equippable_RangedWeapon _cachedWeapon = null;
        public static float _lastWeaponCheckTime = 0f;
        public const float WEAPON_CACHE_INTERVAL = 1f;
        public static string _packageType = "baggie";

        public static NetworkObject FindPlayerNetworkObject()
        {
            // Use cached player object if recent
            if (_cachedPlayerObject != null &&
                Time.time - _lastWeaponCheckTime < WEAPON_CACHE_INTERVAL)
            {
                return _cachedPlayerObject;
            }

            // Reset cache time
            _lastWeaponCheckTime = Time.time;

            // Direct search for player NetworkObject
            var playerObjects = Resources.FindObjectsOfTypeAll<NetworkObject>()
                .Where(obj =>
                    obj != null &&
                    obj.gameObject != null &&
                    (obj.gameObject.name.Contains("Player") ||
                     obj.name.Contains("Player") ||
                     obj.name.Contains(SystemInfo.deviceName)))
                .ToList();

            if (playerObjects.Count > 0)
            {
                _cachedPlayerObject = playerObjects[0];
                return _cachedPlayerObject;
            }

            ModLogger.Error("No player NetworkObject found!");
            return null;
        }

        public static void RefreshOnlinePlayers()
        {
            try
            {
                PlayerCache._onlinePlayers.Clear();
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;

                if (playerList == null || playerList.Count == 0)
                    return;

                //LoggerInstance.Msg($"Total players in list: {playerList.Count}");

                // Get FishySteamworks transport instance
                var fishyTransport = Resources.FindObjectsOfTypeAll<Il2CppFishySteamworks.FishySteamworks>().FirstOrDefault();
                var serverSocket = fishyTransport?._server;

                // Create a mapping of connection IDs to Steam IDs
                Dictionary<int, string> connIdToSteamId = new Dictionary<int, string>();

                // Extract Steam ID mappings from the server socket
                if (serverSocket != null && serverSocket._steamIds != null)
                {
                    try
                    {
                        var steamIds = serverSocket._steamIds.First;
                        if (steamIds != null)
                        {
                            var getEnumerator = steamIds.GetType().GetMethod("GetEnumerator");
                            if (getEnumerator != null)
                            {
                                var enumerator = getEnumerator.Invoke(steamIds, null);
                                if (enumerator != null)
                                {
                                    var moveNext = enumerator.GetType().GetMethod("MoveNext");
                                    var current = enumerator.GetType().GetProperty("Current");

                                    if (moveNext != null && current != null)
                                    {
                                        while ((bool)moveNext.Invoke(enumerator, null))
                                        {
                                            var kvp = current.GetValue(enumerator);
                                            if (kvp != null)
                                            {
                                                var key = kvp.GetType().GetProperty("Key")?.GetValue(kvp);
                                                var value = kvp.GetType().GetProperty("Value")?.GetValue(kvp);

                                                if (key != null && value != null)
                                                {
                                                    string steamId = key.ToString();
                                                    int connId = Convert.ToInt32(value);
                                                    connIdToSteamId[connId] = steamId;
                                                    //LoggerInstance.Msg($"Mapped Connection ID {connId} to Steam ID {steamId}");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Error($"Error mapping connections to Steam IDs: {ex.Message}");
                    }
                }

                foreach (var player in playerList)
                {
                    if (player == null) continue;

                    bool isLocal = GameplayUtils.IsLocalPlayer(player);
                    var health = GameplayUtils.GetPlayerHealth(player);

                    // Extract Steam ID from name
                    string steamId = "Unknown";
                    string playerName = player.name;
                    int steamIdStart = playerName.IndexOf('(');
                    int steamIdEnd = playerName.IndexOf(')');

                    if (steamIdStart != -1 && steamIdEnd != -1)
                    {
                        steamId = playerName.Substring(steamIdStart + 1, steamIdEnd - steamIdStart - 1);
                    }

                    // Get network object for additional information
                    var netObj = player.GetComponent<Il2CppFishNet.Object.NetworkObject>();

                    string networkInfo = "Unknown";
                    string ipAddress = "Unknown";

                    if (netObj != null)
                    {
                        networkInfo = $"Owner ID: {netObj.OwnerId}, Spawned: {netObj.IsSpawned}, Is Owner: {netObj.IsOwner}";

                        // For local player, set address to "Local Player"
                        if (isLocal)
                        {
                            ipAddress = "Local Player";
                        }
                        else
                        {
                            // For remote players, set the address to the Steam ID from the mapping
                            int ownerId = (int)netObj.OwnerId;
                            if (connIdToSteamId.ContainsKey(ownerId))
                            {
                                ipAddress = $"Steam ID: {connIdToSteamId[ownerId]}";
                            }
                            else
                            {
                                // If no mapping, use the Steam ID from the player name
                                ipAddress = $"Steam ID: {steamId}";
                            }
                        }
                    }

                    var playerInfo = new OnlinePlayerInfo
                    {
                        Player = player,
                        Name = playerName.Split('(')[0].Trim(),
                        SteamID = steamId,
                        ClientAddress = ipAddress,
                        Health = health,
                        IsLocal = isLocal,
                        ExplodeLoop = false
                    };

                    PlayerCache._onlinePlayers.Add(playerInfo);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error refreshing online players: {ex.Message}");
            }
        }

    }
}
