using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.PlayerScripts.Health;
using Il2CppScheduleOne.NPCs;
using MelonLoader;
using static Modern_Cheat_Menu.Core;
using Modern_Cheat_Menu.Library;
using Modern_Cheat_Menu.Model;
using Modern_Cheat_Menu.Patches;
using System.Reflection;
using System.Collections;
using UnityEngine;
using Modern_Cheat_Menu.Features;

namespace Modern_Cheat_Menu.Commands
{
    public class PlayerCommand
    {

        public static void ServerExecuteDamagePlayer(Il2CppScheduleOne.PlayerScripts.Player targetPlayer, float damageAmount)
        {
            try
            {
                var playerHealth = GameplayUtils.GetPlayerHealth(targetPlayer);
                if (playerHealth == null)
                {
                    ModLogger.Error("PlayerHealth not found on player!");
                    return;
                }

                // For damage, we use RpcLogic___TakeDamage, but first need to call RpcWriter to send to server
                try
                {
                    // This is the key - we send to the server, not directly to the client
                    playerHealth.RpcWriter___Observers_TakeDamage_3505310624(damageAmount, true, true);
                }
                catch (Exception e)
                {
                    ModLogger.Error($"Failed using RpcWriter: {e.Message}");
                    try
                    {
                        playerHealth.TakeDamage(damageAmount, true, true);
                    }
                    catch (Exception e2)
                    {
                        ModLogger.Error($"All damage methods failed: {e2.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error in ServerExecuteDamagePlayer: {ex.Message}");
            }
        }

        public static void ServerExecuteKillPlayer(Il2CppScheduleOne.PlayerScripts.Player targetPlayer)
        {
            try
            {
                var playerHealth = GameplayUtils.GetPlayerHealth(targetPlayer);
                if (playerHealth == null) return;

                // Prefer Server SendDie for remote players
                playerHealth.RpcWriter___Server_SendDie_2166136261();
                playerHealth.RpcWriter___Observers_TakeDamage_3505310624(99999999999999f, true, true);

                ModLogger.Info($"Killed player: {targetPlayer.name}");
                Notifier.ShowNotification("Player", $"Killed {targetPlayer.name}", NotificationSystem.NotificationType.Success);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error killing player: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to kill player", NotificationSystem.NotificationType.Error);
            }
        }

        // Command Handlers
        public static void DamagePlayerCommand(string[] args)
        {
            if (args.Length < 2 ||
                !int.TryParse(args[0], out int playerIndex) || playerIndex < 1 ||
                !float.TryParse(args[1], out float damage))
            {
                ModLogger.Error("Invalid parameters! Please enter valid player index and damage amount.");
                Notifier.ShowNotification("Error", "Invalid parameters", NotificationSystem.NotificationType.Error);
                return;
            }

            try
            {
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
                playerIndex--; // Convert to 0-based index

                if (playerList != null && playerIndex >= 0 && playerIndex < playerList.Count)
                {
                    var player = playerList[playerIndex];
                    if (player == null)
                    {
                        ModLogger.Error("Player is null!");
                        Notifier.ShowNotification("Error", "Player is null", NotificationSystem.NotificationType.Error);
                        return;
                    }

                    // Check if it's the local player
                    if (GameplayUtils.IsLocalPlayer(player))
                    {
                        // For local player, we can use the direct method
                        var playerHealth = GameplayUtils.GetPlayerHealth(player);
                        if (playerHealth != null)
                        {
                            playerHealth.TakeDamage(damage, true, true);
                            Notifier.ShowNotification("Player", "Damaged local player", NotificationSystem.NotificationType.Success);
                        }
                        else
                        {
                            ModLogger.Error("Local player health component not found!");
                            Notifier.ShowNotification("Error", "Health component not found", NotificationSystem.NotificationType.Error);
                        }
                    }
                    else
                    {
                        // For other players, use the server method
                        ServerExecuteDamagePlayer(player, damage);
                        Notifier.ShowNotification("Player", $"Sent damage request for {player.name}", NotificationSystem.NotificationType.Success);
                    }
                }
                else
                {
                    ModLogger.Error("Player index out of range!");
                    Notifier.ShowNotification("Error", "Player index out of range", NotificationSystem.NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error damaging player: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to damage player", NotificationSystem.NotificationType.Error);
            }
        }

        public static void KillPlayerCommand(string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int playerIndex) || playerIndex < 1)
            {
                ModLogger.Error("Invalid player index! Please enter a valid number.");
                Notifier.ShowNotification("Error", "Invalid player index", NotificationSystem.NotificationType.Error);
                return;
            }

            try
            {
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
                playerIndex--; // Convert to 0-based index

                if (playerList != null && playerIndex >= 0 && playerIndex < playerList.Count)
                {
                    var player = playerList[playerIndex];
                    if (player == null)
                    {
                        ModLogger.Error("Player is null!");
                        Notifier.ShowNotification("Error", "Player is null", NotificationSystem.NotificationType.Error);
                        return;
                    }

                    //ServerExecuteKillPlayer(player);
                    ServerExecuteDamagePlayer(player, 99999999999999f);
                    Notifier.ShowNotification("Player", $"Sent kill request for {player.name}", NotificationSystem.NotificationType.Success);

                    var playerHealth = GameplayUtils.GetPlayerHealth(player);
                    if (playerHealth != null)
                    {
                        playerHealth.Die();
                        ModLogger.Info("Killed local player");
                        Notifier.ShowNotification("Player", "Killed local player", NotificationSystem.NotificationType.Success);
                    }
                }
                else
                {
                    ModLogger.Error("Player index out of range!");
                    Notifier.ShowNotification("Error", "Player index out of range", NotificationSystem.NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error killing player: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to kill player", NotificationSystem.NotificationType.Error);
            }
        }

        public static void KillAllPlayersCommand(string[] args)
        {
            try
            {
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
                if (playerList == null || playerList.Count == 0)
                {
                    ModLogger.Error("No players found!");
                    Notifier.ShowNotification("Players", "No players found", NotificationSystem.NotificationType.Error);
                    return;
                }

                int killedCount = 0;

                for (int i = 0; i < playerList.Count; i++)
                {
                    var player = playerList[i];
                    if (player == null) continue;

                    // Skip the local player
                    if (GameplayUtils.IsLocalPlayer(player))
                    {
                        ModLogger.Info($"Skipping local player: {player.name}");
                        continue;
                    }

                    // Kill the remote player
                    ServerExecuteDamagePlayer(player, 99999999999999f);
                    killedCount++;
                }
                Notifier.ShowNotification("Players", $"Kill requests sent for {killedCount} players", NotificationSystem.NotificationType.Success);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error killing all players: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to kill all players", NotificationSystem.NotificationType.Error);
            }
        }

        public static void ToggleNeverWanted(string[] args)
        {
            try
            {
                ModStateS._playerNeverWantedEnabled = !ModStateS._playerNeverWantedEnabled;

                if (ModStateS._playerNeverWantedEnabled)
                {
                    if (ModStateS._neverWantedCoroutine == null)
                    {
                        ModStateS._neverWantedCoroutine = MelonCoroutines.Start(NeverWantedRoutine());
                    }
                    Notifier.ShowNotification("Never Wanted", "Enabled", NotificationSystem.NotificationType.Success);
                }
                else
                {
                    if (ModStateS._neverWantedCoroutine != null)
                    {
                        MelonCoroutines.Stop(ModStateS._neverWantedCoroutine);
                        ModStateS._neverWantedCoroutine = null;
                    }

                    Notifier.ShowNotification("Never Wanted", "Disabled", NotificationSystem.NotificationType.Info);
                }
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error toggling never wanted: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to toggle never wanted", NotificationSystem.NotificationType.Error);
            }
        }

        private static IEnumerator NeverWantedRoutine()
        {
            while (true)
            {
                try
                {
                    // Clear Wanted Level
                    ClearWantedLevelEx(null);
                }
                catch (System.Exception ex)
                {
                    ModLogger.Error($"Error in godmode routine: {ex.Message}");
                }

                // Wait before next health update
                yield return new WaitForSeconds(0.2f);
            }
        }


        // Helper method to find the local player
        public static void PatchImpactNetworkMethods()
        {
            try
            {
                ModLogger.Info("Setting up godmode network method patches...");

                // Save the local player name for comparison
                var localPlayer = GameplayUtils.FindLocalPlayer();
                if (localPlayer != null)
                {
                    ModState._localPlayerName = localPlayer.name;
                    ModLogger.Info($"Local player identified as: {ModState._localPlayerName}");
                }
                else
                {
                    ModLogger.Error("Failed to identify local player for godmode!");
                    return;
                }

                // Use System.Type instead of Il2CppSystem.Type
                var playerHealthType = typeof(PlayerHealth);

                var blockMethod = typeof(Core).GetMethod("BlockNetworkDamageMethod",
                    BindingFlags.Static | BindingFlags.NonPublic);

                if (blockMethod == null)
                {
                    ModLogger.Error("BlockNetworkDamageMethod not found!");
                    return;
                }

                var prefix = new HarmonyLib.HarmonyMethod(blockMethod);

                // Patch TakeDamage methods
                ModSetting.PatchMethod(playerHealthType, "RpcWriter___Observers_TakeDamage_3505310624", prefix);
                ModSetting.PatchMethod(playerHealthType, "RpcLogic___TakeDamage_3505310624", prefix);
                ModSetting.PatchMethod(playerHealthType, "RpcReader___Observers_TakeDamage_3505310624", prefix);

                // Patch Die methods
                ModSetting.PatchMethod(playerHealthType, "RpcWriter___Observers_Die_2166136261", prefix);
                ModSetting.PatchMethod(playerHealthType, "RpcLogic___Die_2166136261", prefix);
                ModSetting.PatchMethod(playerHealthType, "RpcReader___Observers_Die_2166136261", prefix);

                ModLogger.Info("Successfully patched PlayerHealth network methods");
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error patching network damage methods: {ex}");
            }
        }

        // Modify the ToggleGodMode method
        public static void ToggleGodmode(string[] args)
        {
            try
            {
                // Toggle godmode state
                ModStateS._playerGodmodeEnabled = !ModStateS._playerGodmodeEnabled;
                ModState._staticPlayerGodmodeEnabled = ModStateS._playerGodmodeEnabled;

                if (ModStateS._playerGodmodeEnabled)
                {
                    // Update local player name for checking
                    var localPlayer = GameplayUtils.FindLocalPlayer();
                    if (localPlayer != null)
                    {
                        ModState._localPlayerName = localPlayer.name;
                    }
                    else
                    {
                        ModLogger.Error("Failed to identify local player for godmode!");
                    }

                    // Patch network methods to block damage for local player only
                    PatchImpactNetworkMethods();

                    // Start the comprehensive godmode coroutine
                    if (ModStateS._godModeCoroutine == null)
                    {
                        ModStateS._godModeCoroutine = MelonCoroutines.Start(GodModeRoutine());
                    }

                    Notifier.ShowNotification("Godmode", "Enabled network patches.", NotificationSystem.NotificationType.Success);
                }
                else
                {
                    // Stop the godmode coroutine if it's running
                    if (ModStateS._godModeCoroutine != null)
                    {
                        MelonCoroutines.Stop(ModStateS._godModeCoroutine);
                        ModStateS._godModeCoroutine = null;
                    }

                    // Clear local player name
                    ModState._localPlayerName = "";

                    // Attempt to unpatch methods
                    try
                    {
                        HarmonyLib.Harmony.UnpatchAll();
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Error($"Error unpatching methods: {ex.Message}");
                    }

                    Notifier.ShowNotification("Godmode", "Disabled network patches.", NotificationSystem.NotificationType.Info);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error toggling godmode: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to toggle godmode", NotificationSystem.NotificationType.Error);
            }
        }

        private static IEnumerator GodModeRoutine()
        {
            while (ModState._staticPlayerGodmodeEnabled)
            {
                try
                {
                    // Find the local player
                    var localPlayer = GameplayUtils.FindLocalPlayer();
                    if (localPlayer != null)
                    {
                        // Get the health component
                        var playerHealth = GameplayUtils.GetPlayerHealth(localPlayer);
                        if (playerHealth != null)
                        {
                            // Set health to maximum using native method
                            playerHealth.SetHealth(PlayerHealth.MAX_HEALTH);

                            // Revive player if not alive using native method
                            if (!playerHealth.IsAlive)
                            {
                                // Use the native revive method with current position and rotation
                                playerHealth.Revive(
                                    playerHealth.transform.position,
                                    playerHealth.transform.rotation
                                );
                            }

                            // Remove any lethal effects
                            playerHealth.SetAfflictedWithLethalEffect(false);

                            // Optional: Recover health
                            playerHealth.RecoverHealth(PlayerHealth.HEALTH_RECOVERY_PER_MINUTE);

                            // Prevent death events
                            if (playerHealth.onDie != null)
                                playerHealth.onDie.RemoveAllListeners();
                        }
                        else
                        {
                            ModLogger.Error("Local player health component not found!");
                        }
                    }
                    else
                    {
                        ModLogger.Error("Local player not found in godmode routine!");
                    }
                }
                catch (System.Exception ex)
                {
                    ModLogger.Error($"Error in godmode routine: {ex.Message}");
                }

                // Wait before next health update
                yield return new WaitForSeconds(0.5f);
            }
        }

        public static void SetPlayerMovementSpeed(string[] args)
        {
            if (!CommandCore.TryParseSingleIntArg(args, out int speed, "Movement Speed"))
                return;

            try
            {
                var cmd = new Il2CppScheduleOne.Console.SetMoveSpeedCommand();
                cmd.Execute(CommandCore.ToCommandList(speed));
                Notifier.ShowNotification("Movement Speed", $"Set speed to {speed}", NotificationSystem.NotificationType.Success);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Unable to set player movement speed: {ex.Message}");
            }
        }

        public static void SetPlayerStaminaReserve(string[] args)
        {
            if (!CommandCore.TryParseSingleIntArg(args, out int reserve, "Stamina Reserve"))
                return;

            try
            {
                var cmd = new Il2CppScheduleOne.Console.SetStaminaReserve();
                cmd.Execute(CommandCore.ToCommandList(reserve));
                Notifier.ShowNotification("Stamina Reserve", $"Successfully set stamina reserve to {reserve}", NotificationSystem.NotificationType.Success);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Unable to set player stamina reserve: {ex.Message}");
            }
        }
        public static void SetJumpForce(string[] args)
        {
            if (!CommandCore.TryParseSingleIntArg(args, out int force, "Set Jump Force"))
                return;

            try
            {
                var cmd = new Il2CppScheduleOne.Console.SetJumpMultiplier();
                cmd.Execute(CommandCore.ToCommandList(force));
                Notifier.ShowNotification("Set Jump Force", $"Successfully set stamina force to {force}", NotificationSystem.NotificationType.Success);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Unable to set player jump force: {ex.Message}");
            }
        }

        public static void SetLawIntensity(string[] args)
        {
            if (!CommandCore.TryParseSingleIntArg(args, out int intensity, "Law Intensity"))
                return;

            try
            {
                var cmd = new Il2CppScheduleOne.Console.SetLawIntensity();
                cmd.Execute(CommandCore.ToCommandList(intensity));
                Notifier.ShowNotification("Law Intensity", $"Successfully set to {intensity}", NotificationSystem.NotificationType.Success);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Unable to set law intensity: {ex.Message}");
            }
        }

        public static void ChangeXP(string[] args)
        {
            if (!CommandCore.TryParseSingleIntArg(args, out int amount, "XP"))
                return;

            try
            {
                var cmd = new Il2CppScheduleOne.Console.GiveXP();
                cmd.Execute(CommandCore.ToCommandList(amount));
                Notifier.ShowNotification("XP", $"Successfully Changed by {amount}", NotificationSystem.NotificationType.Success);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Unable to change XP: {ex.Message}");
            }
        }

        public static void ChangeCash(string[] args)
        {
            if (!CommandCore.TryParseSingleIntArg(args, out int amount, "Cash"))
                return;

            try
            {
                var cmd = new Il2CppScheduleOne.Console.ChangeCashCommand();
                cmd.Execute(CommandCore.ToCommandList(amount));
                Notifier.ShowNotification("Cash", $"Successfully Changed by {amount}", NotificationSystem.NotificationType.Success);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Failed to change cash amount: {ex.Message}");
            }
        }

        public static void ChangeBalance(string[] args)
        {
            if (!CommandCore.TryParseSingleIntArg(args, out int amount, "Online Balance"))
                return;

            try
            {
                var cmd = new Il2CppScheduleOne.Console.ChangeOnlineBalanceCommand();
                cmd.Execute(CommandCore.ToCommandList(amount));
                Notifier.ShowNotification("Cash", $"Successfully Changed by {amount}", NotificationSystem.NotificationType.Success);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Failed to change online balance: {ex.Message}");
            }
        }

        public static void ClearInventory(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.ClearInventoryCommand();
                command.Execute(null);
                ModLogger.Info("Inventory cleared.");
                Notifier.ShowNotification("Inventory", "Cleared all items", NotificationSystem.NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to clear inventory", NotificationSystem.NotificationType.Error);
            }
        }

        public static void RaiseWantedLevel(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.RaisedWanted();
                command.Execute(null);
                Notifier.ShowNotification("Wanted Level", "Increased", NotificationSystem.NotificationType.Info);
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error raising wanted level: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to raise wanted level", NotificationSystem.NotificationType.Error);
            }
        }

        public static void LowerWantedLevel(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.LowerWanted();
                command.Execute(null);
                Notifier.ShowNotification("Wanted Level", "Decreased", NotificationSystem.NotificationType.Info);
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error lowering wanted level: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to lower wanted level", NotificationSystem.NotificationType.Error);
            }
        }

        public static void ClearWantedLevel(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.ClearWanted();
                command.Execute(null);
                Notifier.ShowSuccess("Wanted Level", "Cleared");
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error clearing wanted level: {ex.Message}");
                Notifier.ShowError("Error", "Failed to clear wanted level");
            }
        }

        public static void ClearWantedLevelEx(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.ClearWanted();
                command.Execute(null);
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error clearing wanted level: {ex.Message}");
                Notifier.ShowError("Error", "Failed to clear wanted level");
            }
        }

        public static void UnlimitedTrashGrabber(string[] args)
        {
            ModLogger.Error($"Set TimeManager GetMethods: {typeof(Il2CppScheduleOne.GameTime.TimeManager).GetMethods()}");
            if (TrashGrabberPatch.Enabled)
                TrashGrabberPatch.Enabled = false;
            else
                TrashGrabberPatch.Enabled = true;

            Notifier.ShowSuccess("Trash Grabber Capacity", TrashGrabberPatch.Enabled ? "Enabled infinite capacity!" : "Restored original capacity.");
        }

        public static void SetDiscovered(string[] args)
        {
            try
            {
                string displayName = args.Length > 0 ? args[0] : null;
                if (string.IsNullOrEmpty(displayName))
                {
                    Notifier.ShowError("Error", "Please select an item");
                    return;
                }

                var cmd = new Il2CppScheduleOne.Console.SetDiscovered();
                cmd.Execute(CommandCore.ToCommandList(displayName));

                Notifier.ShowSuccess("Set Discovered", $"Discovered: {displayName})");
            }
            catch (Exception ex)
            {
                ModLogger.Error($"SetDiscovered failed: {ex.Message}");
                Notifier.ShowError("Error", "Failed to set discovered item.");
            }
        }

        public static void EnableKeyExplosions(string[] args)
        {
            if (ExplosionManager.Enabled)
                ExplosionManager.Enabled = false;
            else
                ExplosionManager.Enabled = true;

            Notifier.ShowSuccess("Explosion Key", ExplosionManager.Enabled ? "Enabled quick explosions!" : "Disabled quick explosions.");
        }

        public static void SetEmotion(string[] args)
        {
            try
            {
                
                
                string emotionName = args.Length > 0 ? args[0] : null;
                
                foreach (var kk in ModData._emotionCache)
                {
                    ModLogger.Error($"Set ModData._emotionCache: {kk}");
                }

                var cmd = new Il2CppScheduleOne.Console.SetEmotion();
                cmd.Execute(CommandCore.ToCommandList("Zombie"));

                //Notifier.ShowSuccess("Set Emotion", $"Emotion: ({emotion})");
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Set Emotion failed: {ex.Message}");
                Notifier.ShowError("Error", "Failed to set emotion item.");
            }
        }

        public static void SetTrashGrabberAutoSize(string[] args)
        {
            if (!float.TryParse(args[0], out float debugv))
                return;

            try
            {
                ModStateS.trashGrabberAutoRadius = debugv;
                Notifier.ShowNotification("debugv", $"debug set {debugv}", NotificationSystem.NotificationType.Success);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Unable to set debugv: {ex.Message}");
            }
        }
        public static void DrawTrashGrabberAutoBox(string[] args)
        {
            if (ModStateS.drawTrashGrabberPickup)
                ModStateS.drawTrashGrabberPickup = false;
            else
                ModStateS.drawTrashGrabberPickup = true;

            Notifier.ShowNotification("Trash Grabber Debug Box", $"Box Visual {ModStateS.drawTrashGrabberPickup}", NotificationSystem.NotificationType.Success);
        }

        
        #region Weapons Modifications
        public static Equippable_RangedWeapon GetEquippedWeapon()
        {
            try
            {
                // Use cached weapon if recent
                if (PlayerCache._cachedWeapon != null &&
                    Time.time - PlayerCache._lastWeaponCheckTime < PlayerCache.WEAPON_CACHE_INTERVAL)
                {
                    return PlayerCache._cachedWeapon;
                }

                // Reset cache time
                PlayerCache._lastWeaponCheckTime = Time.time;

                // Find player object
                var playerObject = PlayerCache.FindPlayerNetworkObject();
                if (playerObject == null)
                {
                    ModLogger.Error("Cannot find player object for weapon detection.");
                    return null;
                }

                // Attempt to find weapon through different methods
                Equippable_RangedWeapon foundWeapon = null;

                // Method 1: Direct component search on player object
                foundWeapon = playerObject.GetComponent<Equippable_RangedWeapon>();

                // Method 2: Search in player's children
                if (foundWeapon == null)
                {
                    foundWeapon = playerObject.GetComponentInChildren<Equippable_RangedWeapon>();
                }

                // Method 3: Reflection-based search in player components
                if (foundWeapon == null)
                {
                    var components = playerObject.GetComponents<Component>();
                    foreach (var component in components)
                    {
                        try
                        {
                            // Look for properties that might contain the weapon
                            var properties = component.GetType().GetProperties(
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);

                            foreach (var prop in properties)
                            {
                                if (prop.Name.Contains("Weapon") || prop.Name.Contains("Equipped") || prop.Name.Contains("CurrentItem"))
                                {
                                    var value = prop.GetValue(component);
                                    if (value is Equippable_RangedWeapon rangedWeapon)
                                    {
                                        foundWeapon = rangedWeapon;
                                        break;
                                    }
                                }
                            }

                            if (foundWeapon != null) break;
                        }
                        catch { }
                    }
                }

                // Fallback: Direct type search
                if (foundWeapon == null)
                {
                    var weapons = Resources.FindObjectsOfTypeAll<Equippable_RangedWeapon>();
                    foundWeapon = weapons.FirstOrDefault(w =>
                        w != null &&
                        w.gameObject != null &&
                        w.gameObject.name.Contains("(Clone)"));
                }

                // Cache the weapon
                if (foundWeapon != null)
                {
                    PlayerCache._cachedWeapon = foundWeapon;
                    return foundWeapon;
                }
                return null;
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error in weapon detection: {ex.Message}");
                return null;
            }
        }
        #endregion

        // Toggle unlimited ammo
        public static void ToggleUnlimitedAmmo(string[] args)
        {
            try
            {
                ModStateS._unlimitedAmmoEnabled = !ModStateS._unlimitedAmmoEnabled;

                if (ModStateS._unlimitedAmmoEnabled)
                {
                    if (ModStateS._unlimitedAmmoCoroutine == null)
                    {
                        ModStateS._unlimitedAmmoCoroutine = MelonCoroutines.Start(UnlimitedAmmoRoutine());
                    }

                    Notifier.ShowNotification("Unlimited Ammo", "Enabled", NotificationSystem.NotificationType.Success);
                }
                else
                {
                    if (ModStateS._unlimitedAmmoCoroutine != null)
                    {
                        MelonCoroutines.Stop(ModStateS._unlimitedAmmoCoroutine);
                        ModStateS._unlimitedAmmoCoroutine = null;
                    }

                    Notifier.ShowNotification("Unlimited Ammo", "Disabled", NotificationSystem.NotificationType.Info);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error toggling unlimited ammo: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to toggle unlimited ammo", NotificationSystem.NotificationType.Error);
            }
        }

        // Unlimited ammo coroutine
        public static IEnumerator UnlimitedAmmoRoutine()
        {
            while (ModStateS._unlimitedAmmoEnabled)
            {
                try
                {
                    // Get the weapon more efficiently
                    var weapon = GetEquippedWeapon();

                    // Only process if weapon is actually equipped
                    if (weapon != null)
                    {
                        // Ensure the weapon is usable
                        if (weapon.weaponItem != null)
                        {
                            // Directly set magazine to full
                            if (weapon.weaponItem.Value < weapon.MagazineSize)
                            {
                                weapon.weaponItem.Value = weapon.MagazineSize;
                            }
                        }

                        // Specific handling for different weapon types
                        if (weapon is Equippable_Revolver revolver)
                        {
                            revolver.SetDisplayedBullets(revolver.MagazineSize);
                        }

                        // Prevent unnecessary reloading
                        if (weapon.IsReloading)
                        {
                            // Quick way to stop reload
                            weapon.TimeSinceFire = float.MaxValue;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    ModLogger.Error($"Error in unlimited ammo routine: {ex.Message}");
                }

                // Wait slightly longer to reduce performance impact
                yield return new WaitForSeconds(0.3f);
            }
        }

        // Harmony prefix method must be static
        public static bool BlockNetworkDamageMethod()
        {
            return !ModState._staticPlayerGodmodeEnabled;
        }
    }
}
