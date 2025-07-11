using Il2CppScheduleOne.Combat;
using MelonLoader;
using static Modern_Cheat_Menu.Core;
using Modern_Cheat_Menu.Library;
using Modern_Cheat_Menu.Model;
using System.Reflection;
using System.Collections;
using UnityEngine;

namespace Modern_Cheat_Menu.Features
{
    public class ExplosionManager
    {
        public static bool Enabled = false;

        #region Explosion exploiting

        // Explosion loop functionality
        public static Dictionary<string, object> _explodeLoopCoroutines = new Dictionary<string, object>();

        public static void StartExplodeLoop(OnlinePlayerInfo playerInfo)
        {
            string playerKey = playerInfo.Player.GetInstanceID().ToString();
            if (_explodeLoopCoroutines.ContainsKey(playerKey))
                MelonCoroutines.Stop(_explodeLoopCoroutines[playerKey]);

            _explodeLoopCoroutines[playerKey] = MelonCoroutines.Start(ExplodeLoopRoutine(playerInfo));
        }

        public static void StopExplodeLoop(OnlinePlayerInfo playerInfo)
        {
            try
            {
                string playerKey = playerInfo.Player.GetInstanceID().ToString();

                // Stop the coroutine if it exists
                if (_explodeLoopCoroutines.ContainsKey(playerKey))
                    MelonCoroutines.Stop(_explodeLoopCoroutines[playerKey]);
                    _explodeLoopCoroutines.Remove(playerKey);

                // Reset explode loop state
                playerInfo.ExplodeLoop = false;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error stopping explosion loop: {ex.Message}");
            }
        }

        private static IEnumerator ExplodeLoopRoutine(OnlinePlayerInfo playerInfo)
        {
            string playerKey = playerInfo.Player.GetInstanceID().ToString();

            while (playerInfo.ExplodeLoop && playerInfo.Player != null)
            {
                try
                {
                    Vector3 explosionPosition = playerInfo.Player.transform.position; // explosion at player
                    CreateServerSideExplosion(explosionPosition, 99999999999999f, 2f);
                }
                catch (Exception ex)
                {
                    ModLogger.Error($"Error in explosion loop: {ex.Message}");
                }

                yield return new WaitForSeconds(0.09f);
            }
            _explodeLoopCoroutines.Remove(playerKey);
        }

        public static void CreateExplosion(string[] args)
        {
            try
            {
                // Parse optional parameters (damage and radius)
                float damage = 99999999999999f;
                float radius = 2f;
                string target = "custom";
                bool serverSide = true;

                // Parse arguments with more flexibility
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i].ToLowerInvariant();

                    // Existing target parsing
                    if (arg == "all" || arg == "random" || arg == "nukeall")
                    {
                        target = arg;
                        continue;
                    }

                    // Try parsing as damage or radius
                    if (float.TryParse(arg, out float numericValue))
                    {
                        // First numeric value is damage, second is radius
                        if (damage == 50f)
                            damage = numericValue;
                        else if (radius == 5f)
                            radius = numericValue;
                    }
                }

                // Find all players
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;

                if (playerList == null || playerList.Count == 0)
                {
                    Notifier.ShowNotification("Explosion", "No players found!", NotificationSystem.NotificationType.Error);
                    return;
                }

                // Explosion positions
                List<Vector3> explosionPositions = new List<Vector3>();

                switch (target)
                {
                    case "nukeall":
                        damage = 99999999999999f;
                        goto case "all";

                    case "all":
                        foreach (var player in playerList)
                        {
                            if (player != null && player.transform != null)
                            {
                                explosionPositions.Add(player.transform.position);
                            }
                        }
                        break;

                    case "random":
                        var randomPlayer = playerList[UnityEngine.Random.Range(0, playerList.Count)];
                        if (randomPlayer != null && randomPlayer.transform != null)
                        {
                            explosionPositions.Add(randomPlayer.transform.position);
                        }
                        break;

                    default: // custom or default
                             // Try to do a raycast from camera to find target position
                        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
                        RaycastHit hit;

                        Vector3 explosionPosition;
                        if (Physics.Raycast(ray, out hit, 100f))
                        {
                            // Use hit position
                            explosionPosition = hit.point;
                        }
                        else
                        {
                            // Use position a few meters in front of camera
                            explosionPosition = Camera.main.transform.position + Camera.main.transform.forward * 5f;
                        }
                        explosionPositions.Add(explosionPosition);
                        break;
                }

                // Create explosions at each target position
                foreach (Vector3 explosionPos in explosionPositions)
                {
                    CreateServerSideExplosion(explosionPos, damage, radius);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error creating explosion: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to create explosion", NotificationSystem.NotificationType.Error);
            }
        }

        public static void CreateServerSideExplosion(Vector3 position, float damage = 50f, float radius = 5f)
        {
            try
            {
                // Create explosion data
                ExplosionData explosionData = new ExplosionData(radius, damage, radius * 2.0f);

                // Get the CombatManager instance
                var combatManager = CombatManager.Instance;
                if (combatManager == null)
                {
                    ModLogger.Error("CombatManager instance is NULL!");
                    return;
                }

                // Generate a unique explosion ID
                int explosionId = UnityEngine.Random.Range(0, 10000);

                // Try multiple methods to ensure explosion visibility and damage
                try
                {
                    // Method 1: Direct CreateExplosion
                    combatManager.CreateExplosion(position, explosionData, explosionId);
                }
                catch (Exception createEx)
                {
                    ModLogger.Error($"CreateExplosion failed: {createEx.Message}");
                }

                try
                {
                    // Method 2: Explicit Explosion method
                    combatManager.Explosion(position, explosionData, explosionId);
                }
                catch (Exception explodeEx)
                {
                    ModLogger.Error($"Explosion method failed: {explodeEx.Message}");
                }

                try
                {
                    // Method 3: Observers RPC method
                    var observersMethod = combatManager.GetType().GetMethod("RpcWriter___Observers_Explosion_2907189355", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (observersMethod != null)
                    {
                        observersMethod.Invoke(combatManager, new object[] { position, explosionData, explosionId });
                    }
                }
                catch (Exception observersEx)
                {
                    ModLogger.Error($"Observers RPC method failed: {observersEx.Message}");
                }

                // Additional diagnostic checks
                try
                {
                    // Check for explosion prefab
                    var explosionPrefab = combatManager.ExplosionPrefab;
                    if (explosionPrefab != null)
                    {
                        // Instantiate explosion prefab manually
                        var instantiatedExplosion = UnityEngine.Object.Instantiate(explosionPrefab.gameObject, position, Quaternion.identity);
                        instantiatedExplosion.transform.position = position;
                    }
                    else
                    {
                        ModLogger.Error("No explosion prefab found!");
                    }
                }
                catch (Exception prefabEx)
                {
                    ModLogger.Error($"Explosion prefab error: {prefabEx.Message}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Server-side explosion error: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to create server-side explosion", NotificationSystem.NotificationType.Error);
            }
        }
    }
    #endregion
}
