using MelonLoader;
using static Modern_Cheat_Menu.Core;
using Modern_Cheat_Menu.Library;
using System.Reflection;
using UnityEngine;

namespace Modern_Cheat_Menu.Settings
{
    public class ModSettings
    {

        // HarmonyLib initialization.
        public HarmonyLib.Harmony _harmony;

        public Il2CppFishySteamworks.Server.ServerSocket _discoveredServerSocket;

        // At the top of your class, add these fields
        public MelonPreferences_Category _keybindCategory;
        public MelonPreferences_Entry<string> _menuToggleKeyEntry;
        public MelonPreferences_Entry<string> _explosionKeyEntry;
        public static bool _isCapturingKey = false;
        public static MelonPreferences_Entry<string> _currentKeyCaptureEntry;

        // Static fields to be used across the class
        public static KeyCode _currentMenuToggleKey = KeyCode.F10;
        public static KeyCode _currentExplosionAtCrosshairKey = KeyCode.LeftAlt;

        public KeyCode CurrentMenuToggleKey => _currentMenuToggleKey;
        public KeyCode CurrentExplosionAtCrosshairKey => _currentExplosionAtCrosshairKey;


        #region Settings System

        // Add these fields to the existing fields section
        private MelonPreferences_Category _settingsCategory;
        private MelonPreferences_Entry<float> _uiScaleEntry;
        private MelonPreferences_Entry<float> _uiOpacityEntry;
        private MelonPreferences_Entry<bool> _enableAnimationsEntry;
        private MelonPreferences_Entry<bool> _enableGlowEntry;
        private MelonPreferences_Entry<bool> _enableBlurEntry;
        private MelonPreferences_Entry<bool> _darkThemeEntry;

        public void InitializeSettingsSystem()
        {
            try
            {
                // Create or get the settings category
                _settingsCategory = MelonPreferences.CreateCategory("CheatMenu_Settings");

                // UI Settings
                _uiScaleEntry = _settingsCategory.CreateEntry("UIScale", 1.0f, "UI Scale", "Scale factor for the cheat menu UI");
                _uiOpacityEntry = _settingsCategory.CreateEntry("UIOpacity", 0.95f, "UI Opacity", "Opacity level for the cheat menu UI");
                _enableAnimationsEntry = _settingsCategory.CreateEntry("EnableAnimations", true, "Enable Animations", "Toggle animations in the cheat menu");
                _enableGlowEntry = _settingsCategory.CreateEntry("EnableGlow", true, "Enable Glow Effects", "Toggle glow effects in the cheat menu");
                _enableBlurEntry = _settingsCategory.CreateEntry("EnableBlur", true, "Enable Background Blur", "Toggle background blur in the cheat menu");
                _darkThemeEntry = _settingsCategory.CreateEntry("DarkTheme", true, "Dark Theme", "Use dark theme for the cheat menu");

                // Load all settings
                LoadSettings();

                ModLogger.Info("Settings system initialized successfully.");
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error initializing settings system: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to initialize settings", NotificationSystem.NotificationType.Error);
            }
        }

        public void LoadSettings()
        {
            try
            {
                // Load UI settings
                UIs._uiScale = _uiScaleEntry.Value;
                ModStateS._uiOpacity = _uiOpacityEntry.Value;
                ModStateS._enableAnimations = _enableAnimationsEntry.Value;
                ModStateS._enableGlow = _enableGlowEntry.Value;
                ModStateS._enableBlur = _enableBlurEntry.Value;
                ModStateS._darkTheme = _darkThemeEntry.Value;

                // Load keybinds (already implemented in UpdateKeybinds method)
                UpdateKeybinds();

            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error loading settings: {ex.Message}");
            }
        }

        public void SaveSettings()
        {
            try
            {
                // Save UI settings
                _uiScaleEntry.Value = UIs._uiScale;
                _uiOpacityEntry.Value = ModStateS._uiOpacity;
                _enableAnimationsEntry.Value = ModStateS._enableAnimations;
                _enableGlowEntry.Value = ModStateS._enableGlow;
                _enableBlurEntry.Value = ModStateS._enableBlur;
                _darkThemeEntry.Value = ModStateS._darkTheme;

                // Save all categories
                _settingsCategory.SaveToFile();
                _keybindCategory.SaveToFile();

                Notifier.ShowNotification("Settings", "Settings saved successfully", NotificationSystem.NotificationType.Success);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error saving settings: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to save settings", NotificationSystem.NotificationType.Error);
            }
        }

        // Method to start key capture for a specific keybind
        public static void StartCaptureKeybind(MelonPreferences_Entry<string> keybindEntry)
        {
            _isCapturingKey = true;
            _currentKeyCaptureEntry = keybindEntry;
            Notifier.ShowNotification("Keybind", "Press any key to set binding...", NotificationSystem.NotificationType.Info);
        }

        public void togglePlayerControllable(bool controllable)
        {
            try
            {
                // Toggle cursor state.
                if (controllable == false && UIs._uiVisible == true)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
                else
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }

                // Toggle camera look controls
                if (Il2CppScheduleOne.PlayerScripts.PlayerCamera.instance != null)
                {
                    Il2CppScheduleOne.PlayerScripts.PlayerCamera.instance.SetCanLook(controllable);
                }

                // Toggle player movement
                if (Il2CppScheduleOne.PlayerScripts.PlayerMovement.Instance != null)
                {
                    Il2CppScheduleOne.PlayerScripts.PlayerMovement.Instance.canMove = controllable;
                }

                // Toggle input system
                if (Il2CppScheduleOne.GameInput.Instance != null &&
                    Il2CppScheduleOne.GameInput.Instance.PlayerInput != null)
                {
                    Il2CppScheduleOne.GameInput.Instance.PlayerInput.m_InputActive = controllable;
                }

                // Toggle inventory system
                //if(PlayerSingleton<PlayerInventory>.Instance != null)
                //{
                //    PlayerSingleton<PlayerInventory>.Instance.SetInventoryEnabled(controllable);
                //}
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error toggling player controls: {ex.Message}");
            }
        }

        public void ToggleUI(bool visible)
        {
            ModLogger.Info($"Toggling UI visibility: {visible}");
            UIs._uiVisible = visible;

            // Reset animation timers
            if (visible)
            {
                UIs._fadeInProgress = 0f;
                UIs._menuAnimationTime = 0f;

                // Ensure initial positioning starts from off-screen
                if (UIs._windowRect.x <= -UIs._windowRect.width)
                {
                    UIs._windowRect.x = -UIs._windowRect.width;
                }
                // Toggle player controls
                togglePlayerControllable(false);
            }
            else
            {
                togglePlayerControllable(true);
            }
        }

        public void InitializeKeybindConfig()
        {
            try
            {
                // Create a category for keybinds
                _keybindCategory = MelonPreferences.CreateCategory("CheatMenu_Keybinds");

                // Create entries with default values
                _menuToggleKeyEntry = _keybindCategory.CreateEntry(
                    "MenuToggleKey",
                    KeyCode.F10.ToString(),
                    "Menu Toggle Key",
                    "Keybind to open/close the cheat menu"
                );

                _explosionKeyEntry = _keybindCategory.CreateEntry(
                    "ExplosionKey",
                    KeyCode.LeftAlt.ToString(),
                    "Explosion at Cursor Key",
                    "Keybind to create explosion at cursor"
                );

                // Load the saved keybinds
                UpdateKeybinds();
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error initializing keybind config: {ex.Message}");
            }
        }

        private void UpdateKeybinds()
        {
            try
            {
                // Parse and update the toggle menu key
                if (Enum.TryParse(_menuToggleKeyEntry.Value, out KeyCode menuToggleKey))
                {
                    _currentMenuToggleKey = menuToggleKey;
                    ModLogger.Info($"Menu toggle key set to: {menuToggleKey}");
                }

                // Parse and update the explosion key
                if (Enum.TryParse(_explosionKeyEntry.Value, out KeyCode explosionKey))
                {
                    _currentExplosionAtCrosshairKey = explosionKey;
                    ModLogger.Info($"Explosion key set to: {explosionKey}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error updating keybinds: {ex.Message}");
            }
        }

        public void SaveKeybind(MelonPreferences_Entry<string> entry, KeyCode newKey)
        {
            try
            {
                entry.Value = newKey.ToString();
                _keybindCategory.SaveToFile();
                UpdateKeybinds();
                Notifier.ShowNotification("Keybind", $"Key set to {newKey}", NotificationSystem.NotificationType.Success);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error saving keybind: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to save keybind", NotificationSystem.NotificationType.Error);
            }
        }
        #endregion

        public void PatchMethod(Type targetType, string methodName, HarmonyLib.HarmonyMethod prefix)
        {
            try
            {
                // Get method with explicit flags for Il2Cpp methods
                var method = targetType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (method == null)
                {
                    ModLogger.Error($"Method {methodName} not found!");
                    return;
                }
                ModSetting._harmony.Patch(method, prefix: prefix);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error patching {methodName}: {ex.Message}");
            }
        }
    }
}
