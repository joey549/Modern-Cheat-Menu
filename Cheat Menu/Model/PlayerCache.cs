using Il2CppFishNet.Object;
using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.PlayerScripts.Health;
using Modern_Cheat_Menu.Library;
using UnityEngine;

namespace Modern_Cheat_Menu.Model
{

    public class OnlinePlayerInfo
    {
        public Il2CppScheduleOne.PlayerScripts.Player Player { get; set; }
        public string Name { get; set; }
        public string SteamID { get; set; }
        public string ServerBindAddress { get; set; }
        public string ClientAddress { get; set; }
        public PlayerHealth Health { get; set; }
        public bool IsLocal { get; set; }
        public bool ExplodeLoop { get; set; }
    }
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
            _onlinePlayers.Clear();

            var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
            if (playerList == null || playerList.Count == 0)
                return;

            var steamIdMap = BuildSteamIdMap();

            foreach (var player in playerList)
            {
                if (player == null) continue;

                var playerName = player.name;
                string steamId = ExtractSteamIdFromName(playerName);
                bool isLocal = GameplayUtils.IsLocalPlayer(player);
                string ipAddress = isLocal ? "Local Player" : steamId;
                var health = GameplayUtils.GetPlayerHealth(player);

                string networkInfo = "Unknown";
                var netObj = player.GetComponent<Il2CppFishNet.Object.NetworkObject>();

                if (netObj != null)
                {
                    int ownerId = (int)netObj.OwnerId;
                    networkInfo = $"Owner ID: {ownerId}, Spawned: {netObj.IsSpawned}, Is Owner: {netObj.IsOwner}";

                    if (!isLocal && steamIdMap.TryGetValue(ownerId, out string mappedId))
                        ipAddress = $"Steam ID: {mappedId}";
                }

                _onlinePlayers.Add(new OnlinePlayerInfo
                {
                    Player = player,
                    Name = playerName.Split('(')[0].Trim(),
                    SteamID = steamId,
                    ClientAddress = ipAddress,
                    Health = health,
                    IsLocal = isLocal,
                    ExplodeLoop = false
                });
            }
        }

        private static Dictionary<int, string> BuildSteamIdMap()
        {
            var map = new Dictionary<int, string>();

            try
            {
                var server = Resources.FindObjectsOfTypeAll<Il2CppFishySteamworks.FishySteamworks>()
                                      .FirstOrDefault()?._server;

                if (server?._steamIds?.First == null) return map;

                var enumerator = server._steamIds.First.GetType().GetMethod("GetEnumerator")?.Invoke(server._steamIds.First, null);
                var moveNext = enumerator?.GetType().GetMethod("MoveNext");
                var currentProp = enumerator?.GetType().GetProperty("Current");

                while (enumerator != null && (bool)moveNext.Invoke(enumerator, null))
                {
                    var kvp = currentProp.GetValue(enumerator);
                    var key = kvp?.GetType().GetProperty("Key")?.GetValue(kvp)?.ToString();
                    var value = kvp?.GetType().GetProperty("Value")?.GetValue(kvp);

                    if (int.TryParse(value?.ToString(), out int connId) && key != null)
                        map[connId] = key;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error building Steam ID map: {ex.Message}");
            }

            return map;
        }


        private static string ExtractSteamIdFromName(string name)
        {
            int start = name.IndexOf('(');
            int end = name.IndexOf(')');
            return (start != -1 && end != -1 && end > start)
                ? name.Substring(start + 1, end - start - 1)
                : "Unknown";
        }
    }
}
