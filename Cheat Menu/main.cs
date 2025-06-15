﻿using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;
using MelonLoader;
using MelonLoader.Utils;
using MelonLoader.NativeUtils;
using UnityEngine;
using Il2CppScheduleOne;
using Il2CppScheduleOne.ItemFramework;
using static Il2CppScheduleOne.Console;
using static Il2CppScheduleOne.GameInput;
using Il2CppScheduleOne.PlayerScripts;
using static Il2CppSystem.Net.ServicePointManager;
using Il2CppScheduleOne.Persistence;
using System.Text.RegularExpressions;
using UnityEngine.InputSystem;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Combat;
using Il2CppScheduleOne.Equipping;
using Il2CppFishNet.Object;
using Il2CppScheduleOne.AvatarFramework.Equipping;
using UnityEngine.EventSystems;
using UnityEngine.UIElements.Internal;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.PlayerScripts.Health;
using System.Runtime.InteropServices;
using Il2CppInterop.Common;
using Il2CppNewtonsoft.Json;
using Il2CppNewtonsoft.Json.Linq;
using Il2CppNewtonsoft.Json.Converters;
using HarmonyLib;
using Il2CppFishNet.Transporting;
using Il2CppFishySteamworks;
using Il2CppSteamworks;
using Unity.Collections;
using UnityEngine.Playables;
using UnityEngine.UI;
using Il2CppFluffyUnderware.Curvy.Generator;
using Il2CppScheduleOne.DevUtilities;
using Il2CppFishNet.Component;
using Il2CppFishNet.Managing;
/*
 * ---------- Commands To Implement ----------
 * setowned, setqueststate, setquestentrystate, setemotion, setunlocked, setrelationship, addemployee
 * setdiscovered
 * ---------- Function Ideas ----------
 * Make every ATM spit out cash of any quantity and dollar amount. (Call it make it rain)
 * Make everyone/person puke
 * Tase everyone/person
 * Arrest everyone/person
 * Give everyone/person wanted level
 * Spam throw cars at people/everyone
 * Control vehicles on the map, maybe remote control someone's.
 * Trash Tornado around player/everyone?
 * Freeze their inputs/character controls
 */

[assembly: MelonInfo(typeof(Modern_Cheat_Menu.Core), Modern_Cheat_Menu.ModInfo.Name, Modern_Cheat_Menu.ModInfo.Version, Modern_Cheat_Menu.ModInfo.Author, null)]
[assembly: MelonGame(Modern_Cheat_Menu.ModInfo.GameDevelopers, Modern_Cheat_Menu.ModInfo.NameOfGame)]
[assembly: HarmonyDontPatchAll]

namespace Modern_Cheat_Menu
{
    public class CustomTextField
    {
        private string _value;
        private GUIStyle _style;
        private bool _isFocused;
        private int _id;
        private static int _nextId = 1000;
        private float _lastInputTime;
        private const float INPUT_COOLDOWN = 0.1f;

        // Static field to track the currently focused text field
        private static CustomTextField _currentlyFocusedField = null;

        public string Value
        {
            get => _value;
            set => _value = value ?? "";
        }

        public CustomTextField(string initialValue = "", GUIStyle style = null)
        {
            _value = initialValue ?? "";
            _style = style ?? GUI.skin.textField;
            _id = _nextId++;
        }

        public string Draw(Rect position)
        {
            return Draw(position, _value, _style);
        }

        public string Draw(Rect position, string text, GUIStyle style)
        {
            Event current = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Keyboard);

            // Handle focus
            switch (current.type)
            {
                case EventType.MouseDown:
                    if (position.Contains(current.mousePosition))
                    {
                        // Unfocus any previously focused field
                        if (_currentlyFocusedField != null && _currentlyFocusedField != this)
                        {
                            _currentlyFocusedField._isFocused = false;
                        }

                        // Focus this field
                        GUIUtility.keyboardControl = controlID;
                        _isFocused = true;
                        _currentlyFocusedField = this;
                        current.Use();
                    }
                    else if (_isFocused)
                    {
                        // Clicked outside, unfocus this field
                        _isFocused = false;
                        if (_currentlyFocusedField == this)
                        {
                            _currentlyFocusedField = null;
                        }
                    }
                    break;

                case EventType.KeyDown:
                    if (_isFocused && GUIUtility.keyboardControl == controlID)
                    {
                        switch (current.keyCode)
                        {
                            case KeyCode.Backspace:
                                if (_value.Length > 0)
                                {
                                    _value = _value.Substring(0, _value.Length - 1);
                                    current.Use();
                                }
                                break;

                            case KeyCode.Return:
                            case KeyCode.KeypadEnter:
                            case KeyCode.Escape:
                                _isFocused = false;
                                GUIUtility.keyboardControl = 0;
                                _currentlyFocusedField = null;
                                current.Use();
                                break;
                        }
                    }
                    break;

                case EventType.Layout:
                    if (_isFocused && _currentlyFocusedField == this)
                    {
                        HandleTextInput(current);
                    }
                    break;
            }

            // Draw the field background
            GUI.Box(position, "", style);

            // Draw the text with cursor
            string displayText = _value;
            if (_isFocused && (Time.time % 1f) < 0.5f)
            {
                displayText += "|"; // Blinking cursor
            }

            GUI.Label(position, displayText, style);

            return _value;
        }

        private void HandleTextInput(Event current)
        {
            // Prevent rapid duplicate input
            if (current.character != '\0' &&
                !char.IsControl(current.character) &&
                Time.time - _lastInputTime > INPUT_COOLDOWN)
            {
                _value += current.character;
                _lastInputTime = Time.time;
                current.Use();
            }
        }

        public string DrawLayout(GUILayoutOption[] options = null)
        {
            Rect rect = GUILayoutUtility.GetRect(40, 20, options ?? new GUILayoutOption[0]);
            return Draw(rect);
        }

        public static implicit operator string(CustomTextField textField)
        {
            return textField.Value;
        }
    }

    public class Core : MelonMod
    {
        // HarmonyLib initialization.
        private HarmonyLib.Harmony _harmony;

        // Add dictionary for text fields
        private Dictionary<string, CustomTextField> _textFields = new Dictionary<string, CustomTextField>();
        // Categories and commands
        private List<CommandCategory> _categories = new List<CommandCategory>();
        private Dictionary<string, string> _itemDictionary = new Dictionary<string, string>();
        private Dictionary<string, List<string>> _vehicleCache = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> _itemCache = new Dictionary<string, List<string>>();

        private Dictionary<string, bool> _qualitySupportCache = new Dictionary<string, bool>();
        private Dictionary<string, List<string>> _itemQualityCache = new Dictionary<string, List<string>>();

        private Il2CppFishySteamworks.Server.ServerSocket _discoveredServerSocket;

        // At the top of your class, add these fields
        private MelonPreferences_Category _keybindCategory;
        private MelonPreferences_Entry<string> _menuToggleKeyEntry;
        private MelonPreferences_Entry<string> _explosionKeyEntry;
        private static bool _isCapturingKey = false;
        private MelonPreferences_Entry<string> _currentKeyCaptureEntry;

        // Static fields to be used across the class
        private static KeyCode _currentMenuToggleKey = KeyCode.F10;
        private static KeyCode _currentExplosionAtCrosshairKey = KeyCode.LeftAlt;

        private KeyCode CurrentMenuToggleKey => _currentMenuToggleKey;
        private KeyCode CurrentExplosionAtCrosshairKey => _currentExplosionAtCrosshairKey;

        private Vector2 _playerScrollPosition = Vector2.zero;

        private Vector2 _settingsScrollPosition = Vector2.zero;

        // Player network interaction category
        public class NetworkPlayerCategory
        {
            public string Name { get; set; }
            public List<Command> Commands { get; set; } = new List<Command>();
        }

        // UI settings
        private bool _uiVisible = false;
        private Rect _windowRect = new Rect(20, 20, 900, 650);
        private Vector2 _scrollPosition = Vector2.zero;
        private int _selectedCategoryIndex = 0;
        private float _fadeInProgress = 0f;
        private bool _isInitialized = false;
        private float _uiScale = 1.0f;
        private bool _showSettings = false;
        private bool _isDragging = false;
        private Vector2 _dragOffset;
        private bool _stylesInitialized = false;
        private bool _needsTextureRecreation = false;
        private bool _needsStyleRecreation = false;
        private MelonPreferences_Entry<float> _menuPosXEntry;
        private MelonPreferences_Entry<float> _menuPosYEntry;

        // Animation timers
        private float _menuAnimationTime = 0f;
        private Dictionary<string, float> _commandAnimations = new Dictionary<string, float>();
        private Dictionary<string, float> _buttonHoverAnimations = new Dictionary<string, float>();
        private Dictionary<string, float> _toggleAnimations = new Dictionary<string, float>();
        private Dictionary<string, Vector2> _itemGridAnimations = new Dictionary<string, Vector2>();

        // Player booleans & shit
        private static bool _staticPlayerGodmodeEnabled = false;
        private bool _playerGodmodeEnabled = false;
        private object _godModeCoroutine = null;
        private bool _playerNeverWantedEnabled = false;
        private object _neverWantedCoroutine = null;
        private static string _localPlayerName = ""; // Store the local player name for checking

        // New weapon cheat settings
        private bool _unlimitedAmmoEnabled = false;
        private object _unlimitedAmmoCoroutine = null;
        private object _perfectAccuracyCoroutine = null;
        private bool _aimbotEnabled = false;
        private object _aimbotCoroutine = null;
        private float _aimbotRange = 50f; // Maximum range to detect enemies
        private bool _autoFireEnabled = false;
        private float _autoFireDelay = 0.5f; // Delay between auto shots
        private bool _perfectAccuracyEnabled = false;
        private bool _noRecoilEnabled = false;
        private object _noRecoilCoroutine = null;
        private bool _oneHitKillEnabled = false;
        private bool _npcsPacifiedEnabled = false;
        private object _pacifyNPCsCoroutine = null;
        private bool _forceCrosshairAlwaysVisible = false;

        // Free camera settings
        private bool _freeCamEnabled = false;

        // IMGUI Styling
        private GUISkin _customSkin;
        private Texture2D _backgroundTexture;
        private Texture2D _panelTexture;
        private Texture2D _buttonNormalTexture;
        private Texture2D _buttonHoverTexture;
        private Texture2D _buttonActiveTexture;
        private Texture2D _toggleOnTexture;
        private Texture2D _toggleOffTexture;
        private Texture2D _sliderThumbTexture;
        private Texture2D _sliderTrackTexture;
        private Texture2D _inputFieldTexture;
        private Texture2D _headerTexture;
        private Texture2D _categoryTabTexture;
        private Texture2D _categoryTabActiveTexture;
        private Texture2D _checkmarkTexture;
        private Texture2D _settingsIconTexture;
        private Texture2D _closeIconTexture;
        private Texture2D _glowTexture;
        private GUIStyle _labelStyle;
        private Texture2D _warningTexture;

        // Styles
        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _categoryButtonStyle;
        private GUIStyle _categoryButtonActiveStyle;
        private GUIStyle _commandLabelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _iconButtonStyle;
        private GUIStyle _toggleButtonStyle;
        private GUIStyle _toggleButtonActiveStyle;
        private GUIStyle _sliderStyle;
        private GUIStyle _sliderThumbStyle;
        private GUIStyle _inputFieldStyle;
        private GUIStyle _searchBoxStyle;
        private GUIStyle _tooltipStyle;
        private GUIStyle _itemButtonStyle;
        private GUIStyle _itemSelectedStyle;
        private GUIStyle _closeButtonStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _panelStyle;
        private GUIStyle _separatorStyle;

        // Colors
        private Color _backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        private Color _panelColor = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        private Color _accentColor = new Color(0.15f, 0.55f, 0.95f, 1f); // Blue accent
        private Color _secondaryAccentColor = new Color(0.15f, 0.85f, 0.55f); // Green accent
        private Color _warningColor = new Color(0.95f, 0.55f, 0.15f); // Orange warning
        private Color _dangerColor = new Color(0.95f, 0.25f, 0.25f); // Red danger
        private Color _textColor = new Color(0.9f, 0.9f, 0.9f);
        private Color _dimTextColor = new Color(0.7f, 0.7f, 0.75f);
        private Color _headerColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);

        // Item manager state
        private string _itemSearchText = "";
        private Vector2 _itemScrollPosition = Vector2.zero;
        private int _itemsPerRow = 5;
        private int _selectedItemIndex = -1;
        private int _selectedQualityIndex = 4; // Default to Heavenly (4)
        private string _selectedItemId = "";
        private string _quantityInput = "1";
        private string _slotInput = "1";
        private float _timeScaleValue = 1.0f;
        private float _timeHours = 12.0f;
        private float _timeMinutes = 0.0f;
        private bool _showTooltip = false;
        private string _currentTooltip = "";
        private Vector2 _tooltipPosition;
        private float _tooltipTimer = 0f;
        private int _tooltipItemId = -1;

        // Settings
        private bool _enableBlur = true;
        private bool _enableAnimations = true;
        private bool _enableGlow = true;
        private bool _darkTheme = true;
        private float _uiOpacity = 0.95f;

        // Notification system
        private Queue<Notification> _notifications = new Queue<Notification>();
        private List<Notification> _activeNotifications = new List<Notification>();
        private float _notificationDisplayTime = 3f;
        private float _notificationFadeTime = 0.5f;

        // Player Window
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

        private List<OnlinePlayerInfo> _onlinePlayers = new List<OnlinePlayerInfo>();
        private float _lastPlayerRefreshTime = 0f;
        private const float PLAYER_REFRESH_INTERVAL = 7f; // Refresh every 7 seconds
        private NetworkObject _cachedPlayerObject = null;
        private Equippable_RangedWeapon _cachedWeapon = null;
        private float _lastWeaponCheckTime = 0f;
        private const float WEAPON_CACHE_INTERVAL = 1f; // Check weapon cache every second
        private string _packageType = "baggie"; 

        private NetworkObject FindPlayerNetworkObject()
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

            LoggerInstance.Error("No player NetworkObject found!");
            return null;
        }

        public enum EQuality
        {
            Trash = 0,
            Poor = 1,
            Standard = 2,
            Premium = 3,
            Heavenly = 4
        }

        // Helper class for command parameters
        public class CommandParameter
        {
            public string Name { get; set; }
            public string Placeholder { get; set; }
            public ParameterType Type { get; set; }
            public string ItemCacheKey { get; set; }
            public string Value { get; set; }
        }

        // Parameter type enum
        public enum ParameterType
        {
            Input,
            Dropdown
        }

        // Command class
        public class Command
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public System.Action<string[]> Handler { get; set; }
            public List<CommandParameter> Parameters { get; set; } = new List<CommandParameter>();
        }

        // Command category class
        public class CommandCategory
        {
            public string Name { get; set; }
            public List<Command> Commands { get; set; } = new List<Command>();
        }


        public override void OnInitializeMelon()
        {
            try
            {
                // Initialize Harmony
                _harmony = new HarmonyLib.Harmony($"{Modern_Cheat_Menu.ModInfo.ComName}");

                // Other initialization code...
                _harmony.PatchAll(typeof(Core).Assembly);

                // Initialize Keybind config
                InitializeKeybindConfig();

                // Initialize Settings system - add this
                InitializeSettingsSystem();

                // Initialize HWID Spoofer
                InitializeHwidPatch();

                // Register commands
                RegisterCommands();

                // Draw Online Players Map
                InitializePlayerMap();

                LoggerInstance.Msg($"{Modern_Cheat_Menu.ModInfo.Name} successfully initialized.");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to initialize mod: {ex.Message}");
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                LoggerInstance.Msg("Main scene loaded, initializing cheat menu.");

                // Find the server socket
                _discoveredServerSocket = FindBestServerSocket();

                // Rest of your existing setup...
                MelonCoroutines.Start(SetupUI());
            }
        }

        private IEnumerator SetupUI()
        {
            yield return new WaitForSeconds(1f);

            try
            {
                // Create textures (still safe to do here)
                CreateTextures();

                // Create button textures
                CreateButtonTextures();

                // Cache game items
                CacheGameItems();

                // Subscribe to player death event - add this line
                SubscribeToPlayerDeathEvent();

                _isInitialized = true;

                // Show notification
                ShowNotification($"{ModInfo.Name} Loaded", $"Press {CurrentMenuToggleKey} to toggle menu visibility", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"UI SETUP FAILED: {ex}");
                _isInitialized = false;
                ShowNotification("Initialization Failed", ex.Message, NotificationType.Error);
            }
        }

        #region Textures and Styles
        private void CreateTextures()
        {
            try
            {
                // Background texture
                _backgroundTexture = new Texture2D(1, 1);
                _backgroundTexture.SetPixel(0, 0, _backgroundColor);
                _backgroundTexture.Apply();

                // Panel texture
                _panelTexture = new Texture2D(1, 1);
                _panelTexture.SetPixel(0, 0, _panelColor);
                _panelTexture.Apply();

                // Button textures
                _buttonNormalTexture = new Texture2D(1, 1);
                _buttonNormalTexture.SetPixel(0, 0, new Color(0.18f, 0.18f, 0.24f, 0.8f));
                _buttonNormalTexture.Apply();

                _buttonHoverTexture = new Texture2D(1, 1);
                _buttonHoverTexture.SetPixel(0, 0, new Color(0.25f, 0.25f, 0.35f, 0.9f));
                _buttonHoverTexture.Apply();

                _buttonActiveTexture = new Texture2D(1, 1);
                _buttonActiveTexture.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.4f, 1f));
                _buttonActiveTexture.Apply();

                // Tab textures
                _categoryTabTexture = _buttonNormalTexture;
                _categoryTabActiveTexture = _buttonActiveTexture;
                _categoryTabActiveTexture = _buttonActiveTexture;

                _settingsIconTexture = new Texture2D(16, 16);
                Color[] pixels = new Color[16 * 16];

                // Fill with transparent pixels
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color(0, 0, 0, 0);

                // Draw a simple gear icon
                Color iconColor = new Color(0.9f, 0.9f, 0.9f, 1f);

                // Outer circle
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        float dx = x - 8;
                        float dy = y - 8;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);

                        if (dist > 5 && dist < 7)
                            pixels[y * 16 + x] = iconColor;

                        // Add simple teeth
                        if ((x == 1 || x == 14) && y >= 6 && y <= 9)
                            pixels[y * 16 + x] = iconColor;
                        if ((y == 1 || y == 14) && x >= 6 && x <= 9)
                            pixels[y * 16 + x] = iconColor;
                        if ((x == 3 || x == 12) && (y == 3 || y == 12))
                            pixels[y * 16 + x] = iconColor;
                    }
                }

                // Inner circle
                for (int y = 6; y <= 9; y++)
                {
                    for (int x = 6; x <= 9; x++)
                    {
                        float dx = x - 8;
                        float dy = y - 8;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);

                        if (dist < 2)
                            pixels[y * 16 + x] = iconColor;
                    }
                }

                _settingsIconTexture.SetPixels(pixels);
                _settingsIconTexture.Apply();

            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error creating textures: {ex.Message}");
            }
        }

        private void InitializeStyles()
        {
            try
            {
                _customSkin = ScriptableObject.CreateInstance<GUISkin>();
                _customSkin.box = new GUIStyle(GUI.skin.box);
                _customSkin.button = new GUIStyle(GUI.skin.button);
                _customSkin.label = new GUIStyle(GUI.skin.label);
                _customSkin.textField = new GUIStyle(GUI.skin.textField);
                _customSkin.toggle = new GUIStyle(GUI.skin.toggle);
                _customSkin.window = new GUIStyle(GUI.skin.window);
                _customSkin.horizontalSlider = new GUIStyle(GUI.skin.horizontalSlider);
                _customSkin.horizontalSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb);

                // Window style
                _windowStyle = new GUIStyle();
                _windowStyle.normal.background = _backgroundTexture;
                _windowStyle.border = new RectOffset(10, 10, 10, 10);
                _windowStyle.padding = new RectOffset(0, 0, 0, 0);
                _windowStyle.margin = new RectOffset(0, 0, 0, 0);

                // Panel style
                _panelStyle = new GUIStyle(GUI.skin.box);
                _panelStyle.normal.background = _panelTexture;
                _panelStyle.border = new RectOffset(8, 8, 8, 8);
                _panelStyle.margin = new RectOffset(10, 10, 10, 10);
                _panelStyle.padding = new RectOffset(10, 10, 10, 10);

                // Title style
                _titleStyle = new GUIStyle(GUI.skin.label);
                _titleStyle.fontSize = 20;
                _titleStyle.fontStyle = FontStyle.Bold;
                _titleStyle.normal.textColor = _textColor;
                _titleStyle.alignment = TextAnchor.MiddleCenter;
                _titleStyle.margin = new RectOffset(0, 0, 10, 15);

                // Header style
                _headerStyle = new GUIStyle(GUI.skin.box);
                _headerStyle.normal.background = _backgroundTexture;
                _headerStyle.border = new RectOffset(2, 2, 2, 2);
                _headerStyle.margin = new RectOffset(0, 0, 0, 10);
                _headerStyle.padding = new RectOffset(10, 10, 8, 8);
                _headerStyle.fontSize = 14;
                _headerStyle.fontStyle = FontStyle.Bold;
                _headerStyle.normal.textColor = _textColor;

                // Button style
                _buttonStyle = new GUIStyle(GUI.skin.button);
                _buttonStyle.normal.background = _buttonNormalTexture;
                _buttonStyle.hover.background = _buttonHoverTexture;
                _buttonStyle.active.background = _buttonActiveTexture;
                _buttonStyle.focused.background = _buttonNormalTexture;
                _buttonStyle.normal.textColor = _textColor;
                _buttonStyle.hover.textColor = Color.white;
                _buttonStyle.fontSize = 12;
                _buttonStyle.alignment = TextAnchor.MiddleCenter;
                _buttonStyle.margin = new RectOffset(5, 5, 2, 2);
                _buttonStyle.padding = new RectOffset(10, 10, 6, 6);
                _buttonStyle.border = new RectOffset(6, 6, 6, 6);

                // Icon button style
                _iconButtonStyle = new GUIStyle(_buttonStyle);
                _iconButtonStyle.padding = new RectOffset(6, 6, 6, 6);

                // Search box style
                _searchBoxStyle = new GUIStyle(GUI.skin.textField);
                _searchBoxStyle.fontSize = 14;
                _searchBoxStyle.margin = new RectOffset(0, 0, 5, 10);

                // Label style
                _labelStyle = new GUIStyle(GUI.skin.label);
                _labelStyle.fontSize = 12;
                _labelStyle.normal.textColor = _textColor;
                _labelStyle.alignment = TextAnchor.MiddleLeft;
                _labelStyle.padding = new RectOffset(5, 5, 2, 2);

                // Category styles
                _categoryButtonStyle = new GUIStyle(_buttonStyle);
                _categoryButtonActiveStyle = new GUIStyle(_buttonStyle);
                _categoryButtonActiveStyle.normal.background = _buttonActiveTexture;

                // Item button style
                _itemButtonStyle = new GUIStyle(_buttonStyle);
                _itemSelectedStyle = new GUIStyle(_itemButtonStyle);
                _itemSelectedStyle.normal.background = _buttonActiveTexture;

                _stylesInitialized = true;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error initializing styles: {ex}");
            }
        }

        #endregion

        #region Settings System

        // Add these fields to the existing fields section
        private MelonPreferences_Category _settingsCategory;
        private MelonPreferences_Entry<float> _uiScaleEntry;
        private MelonPreferences_Entry<float> _uiOpacityEntry;
        private MelonPreferences_Entry<bool> _enableAnimationsEntry;
        private MelonPreferences_Entry<bool> _enableGlowEntry;
        private MelonPreferences_Entry<bool> _enableBlurEntry;
        private MelonPreferences_Entry<bool> _darkThemeEntry;
        private Texture2D _settingsButtonTexture;
        private Texture2D _closeButtonTexture;

        private void CreateButtonTextures()
        {
            // Create settings button texture (gear icon) - SMALLER (18x18)
            _settingsButtonTexture = new Texture2D(18, 18, TextureFormat.RGBA32, false);
            Color[] settingsPixels = new Color[18 * 18];
            for (int i = 0; i < settingsPixels.Length; i++)
                settingsPixels[i] = Color.clear;
            // Draw gear with light gray color instead of white
            Color iconColor = new Color(0.8f, 0.8f, 0.8f, 1.0f); // Light gray
            int center = 9; // Center of 18x18 texture
            int outerRadius = 7; // Smaller radius
            int innerRadius = 3; // Smaller inner radius
            for (int y = 0; y < 18; y++)
            {
                for (int x = 0; x < 18; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > innerRadius && dist < outerRadius)
                        settingsPixels[y * 18 + x] = iconColor;
                }
            }
            // Create spokes
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                float sx = Mathf.Sin(angle);
                float cx = Mathf.Cos(angle);
                for (int r = 6; r < 9; r++) // Adjusted for smaller size
                {
                    int x = (int)(center + cx * r);
                    int y = (int)(center + sx * r);
                    if (x >= 0 && x < 18 && y >= 0 && y < 18)
                        settingsPixels[y * 18 + x] = iconColor;
                }
            }
            _settingsButtonTexture.SetPixels(settingsPixels);
            _settingsButtonTexture.Apply();
            // Create close button texture (X icon) - SMALLER (18x18)
            _closeButtonTexture = new Texture2D(15, 15, TextureFormat.RGBA32, false);
            Color[] closePixels = new Color[15 * 15];
            for (int i = 0; i < closePixels.Length; i++)
                closePixels[i] = Color.clear;
            // Draw X with the same light gray color
            for (int i = 0; i < 15; i++)
            {
                int x1 = i;
                int y1 = i;
                int x2 = 14 - i;
                int y2 = i;
                // Draw diagonal lines with some thickness (but less for smaller size)
                for (int t = -1; t <= 1; t++) // Reduced thickness from -2,2 to -1,1
                {
                    int xt1 = x1 + t;
                    int yt1 = y1;
                    int xt2 = x2 + t;
                    int yt2 = y2;
                    if (xt1 >= 0 && xt1 < 15)
                        closePixels[yt1 * 15 + xt1] = iconColor;
                    if (xt2 >= 0 && xt2 < 15)
                        closePixels[yt2 * 15 + xt2] = iconColor;
                }
            }
            _closeButtonTexture.SetPixels(closePixels);
            _closeButtonTexture.Apply();
        }

        private void InitializeSettingsSystem()
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
                _menuPosXEntry = _settingsCategory.CreateEntry("MenuPosX", 20f, is_hidden: true);
                _menuPosYEntry = _settingsCategory.CreateEntry("MenuPosY", 20f, is_hidden: true);

                // Load all settings
                LoadSettings();

                LoggerInstance.Msg("Settings system initialized successfully.");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error initializing settings system: {ex.Message}");
                ShowNotification("Error", "Failed to initialize settings", NotificationType.Error);
            }
        }

        private void LoadSettings()
        {
            try
            {
                // Load UI settings
                _uiScale = _uiScaleEntry.Value;
                _uiOpacity = _uiOpacityEntry.Value;
                _enableAnimations = _enableAnimationsEntry.Value;
                _enableGlow = _enableGlowEntry.Value;
                _enableBlur = _enableBlurEntry.Value;
                _darkTheme = _darkThemeEntry.Value;

                // Load window position - ADD THIS
                if (_menuPosXEntry != null && _menuPosYEntry != null)
                {
                    LoggerInstance.Msg($"Loading saved window position: X={_menuPosXEntry.Value}, Y={_menuPosYEntry.Value}");
                    _windowRect.x = _menuPosXEntry.Value;
                    _windowRect.y = _menuPosYEntry.Value;
                }
                else
                {
                    LoggerInstance.Error("Menu position entries are null! Cannot load position.");
                }

                // Apply screen bounds checking
                if (_windowRect.x < 0 || _windowRect.x > Screen.width - 100 || _windowRect.y < 0 || _windowRect.y > Screen.height - 100)
                {
                    // Log detailed debug information
                    LoggerInstance.Error($"Menu position out of bounds! Debug info:");
                    LoggerInstance.Error($"Screen resolution: {Screen.width}x{Screen.height}, DPI: {Screen.dpi}");
                    LoggerInstance.Error($"Saved position: X={_windowRect.x}, Y={_windowRect.y}, Width={_windowRect.width}, Height={_windowRect.height}");

                    // Reset to default (center of screen)
                    _windowRect.x = (Screen.width - _windowRect.width) / 2;
                    _windowRect.y = (Screen.height - _windowRect.height) / 2;

                    LoggerInstance.Msg($"Repositioned menu to center: X={_windowRect.x}, Y={_windowRect.y}");
                }
                UpdateKeybinds();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error loading settings: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Save UI settings
                _uiScaleEntry.Value = _uiScale;
                _uiOpacityEntry.Value = _uiOpacity;
                _enableAnimationsEntry.Value = _enableAnimations;
                _enableGlowEntry.Value = _enableGlow;
                _enableBlurEntry.Value = _enableBlur;
                _darkThemeEntry.Value = _darkTheme;

                // Save all categories
                _settingsCategory.SaveToFile();
                _keybindCategory.SaveToFile();

                if (_menuPosXEntry != null && _menuPosYEntry != null)
                {
                    _menuPosXEntry.Value = _windowRect.x;
                    _menuPosYEntry.Value = _windowRect.y;
                }

                ShowNotification("Settings", "Settings saved successfully", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error saving settings: {ex.Message}");
                ShowNotification("Error", "Failed to save settings", NotificationType.Error);
            }
        }

        // Method to start key capture for a specific keybind
        private void StartCaptureKeybind(MelonPreferences_Entry<string> keybindEntry)
        {
            _isCapturingKey = true;
            _currentKeyCaptureEntry = keybindEntry;
            ShowNotification("Keybind", "Press any key to set binding...", NotificationType.Info);
        }

        #endregion

        public override void OnUpdate()
        {
            if (!_isInitialized)
                return;

            // Toggle menu visibility
            if (Input.GetKeyDown(CurrentMenuToggleKey) && !_freeCamEnabled)
            {
                ToggleUI(!_uiVisible);
            }

            // Handle ESC key for exiting freecam
            if (_freeCamEnabled && Input.GetKeyDown(KeyCode.Escape))
            {
                // Disable freecam and restore normal controls
                _freeCamEnabled = false;
                togglePlayerControllable(true);
                ShowNotification("Free Camera", "Disabled", NotificationType.Info);
            }

            // Disable explosion key if we're in freecam mode as well as when the menu is open
            if (!((_freeCamEnabled) || (_uiVisible)))
            {
                if (Input.GetKeyDown(CurrentExplosionAtCrosshairKey))
                {
                    Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
                    RaycastHit hit;
                    Vector3 explosionPosition;
                    if (Physics.Raycast(ray, out hit, 100f))
                    {
                        explosionPosition = hit.point;
                    }
                    else
                    {
                        explosionPosition = Camera.main.transform.position + Camera.main.transform.forward * 5f;
                    }
                    CreateServerSideExplosion(explosionPosition, 99999999999999f, 2f);
                }
            }

            // Update animations for menu
            if (_uiVisible)
            {
                // Menu animation
                _menuAnimationTime += Time.deltaTime * (_enableAnimations ? 1.0f : 10.0f);
                if (_menuAnimationTime > 1.0f)
                    _menuAnimationTime = 1.0f;

                // Update fade in animation
                _fadeInProgress += Time.deltaTime * 5f; // Adjust speed as needed
                if (_fadeInProgress > 1.0f)
                    _fadeInProgress = 1.0f;

                // Update tooltip timer
                if (_showTooltip)
                {
                    _tooltipTimer += Time.deltaTime;
                    if (_tooltipTimer > 0.5f) // Show tooltip after 0.5 sec hover
                    {
                        _showTooltip = true;
                    }
                }

                // Update button hover animations
                List<string> keysToRemove = new List<string>();
                foreach (var key in _buttonHoverAnimations.Keys)
                {
                    float value = _buttonHoverAnimations[key];
                    if (value > 0)
                    {
                        value -= Time.deltaTime * 4f;
                        if (value <= 0)
                        {
                            value = 0;
                            keysToRemove.Add(key);
                        }
                        _buttonHoverAnimations[key] = value;
                    }
                }

                // Clean up completed animations
                foreach (var key in keysToRemove)
                {
                    _buttonHoverAnimations.Remove(key);
                }

                // Update toggle animations
                keysToRemove.Clear();
                foreach (var key in _toggleAnimations.Keys)
                {
                    bool isOn = false;

                    // Determine if toggle is on based on key
                    if (key == "Godmode")
                        isOn = _playerGodmodeEnabled;
                    else if (key == "NeverWanted")
                        isOn = _playerNeverWantedEnabled;
                    else if (key == "FreeCamera")
                        isOn = _freeCamEnabled;

                    float targetValue = isOn ? 1.0f : 0.0f;
                    float currentValue = _toggleAnimations[key];

                    if (currentValue != targetValue)
                    {
                        if (isOn)
                            currentValue += Time.deltaTime * 4f;
                        else
                            currentValue -= Time.deltaTime * 4f;

                        currentValue = Mathf.Clamp01(currentValue);
                        _toggleAnimations[key] = currentValue;

                        if (currentValue == targetValue)
                            keysToRemove.Add(key);
                    }
                }

                // Clean up completed animations
                foreach (var key in keysToRemove)
                {
                    _toggleAnimations.Remove(key);
                }

                // Update notification animations
                UpdateNotifications();
            }
            else
            {
                // Reset menu animation when hidden
                _menuAnimationTime = 0f;
                _fadeInProgress = 0f;

                // Update notification animations even when menu is hidden
                UpdateNotifications();
            }

            // Key capture logic for settings
            if (_isCapturingKey && Input.anyKeyDown)
            {
                foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(key))
                    {
                        // Prevent capturing Escape or other special keys
                        if (key != KeyCode.Escape)
                        {
                            SaveKeybind(_currentKeyCaptureEntry, key);
                        }

                        _isCapturingKey = false;
                        _currentKeyCaptureEntry = null;
                        break;
                    }
                }
            }
        }

        private void togglePlayerControllable(bool controllable)
        {
            try
            {
                // Toggle cursor state.
                if (controllable == false && _uiVisible == true)
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
                LoggerInstance.Error($"Error toggling player controls: {ex.Message}");
            }
        }

        private void ToggleUI(bool visible)
        {
            LoggerInstance.Msg($"Toggling UI visibility: {visible}");
            _uiVisible = visible;

            // Reset animation timers
            if (visible)
            {
                _fadeInProgress = 0f;
                _menuAnimationTime = 0f;

                // Ensure initial positioning starts from off-screen
                if (_windowRect.x <= -_windowRect.width)
                {
                    _windowRect.x = -_windowRect.width;
                }
                // Toggle player controls
                togglePlayerControllable(false);
            }
            else
            { 
                togglePlayerControllable(true);
            }
        }

        private void InitializeKeybindConfig()
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
                LoggerInstance.Error($"Error initializing keybind config: {ex.Message}");
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
                    LoggerInstance.Msg($"Menu toggle key set to: {menuToggleKey}");
                }

                // Parse and update the explosion key
                if (Enum.TryParse(_explosionKeyEntry.Value, out KeyCode explosionKey))
                {
                    _currentExplosionAtCrosshairKey = explosionKey;
                    LoggerInstance.Msg($"Explosion key set to: {explosionKey}");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error updating keybinds: {ex.Message}");
            }
        }

        private void SaveKeybind(MelonPreferences_Entry<string> entry, KeyCode newKey)
        {
            try
            {
                entry.Value = newKey.ToString();
                _keybindCategory.SaveToFile();
                UpdateKeybinds();
                ShowNotification("Keybind", $"Key set to {newKey}", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error saving keybind: {ex.Message}");
                ShowNotification("Error", "Failed to save keybind", NotificationType.Error);
            }
        }
        
        #region OnGUI and UI Drawing
        public override void OnGUI()
        {
            if (!_isInitialized)
                return;

            // Check if textures/styles need recreation
            if (_needsTextureRecreation)
            {
                CreateTextures();
                CreateButtonTextures();
                _needsTextureRecreation = false;
            }

            if (_needsStyleRecreation)
            {
                InitializeStyles();
                _stylesInitialized = true;
                _needsStyleRecreation = false;
            }

            // Draw notifications even when menu is hidden
            if (_activeNotifications.Count > 0)
            {
                DrawNotifications();
            }

            // Draw "Freecam Enabled" overlay when in freecam mode
            if (_freeCamEnabled && !_uiVisible)
            {
                DrawFreecamOverlay();
            }

            // Don't process UI when not visible
            if (!_uiVisible)
                return;

            if (!_stylesInitialized)
            {
                InitializeStyles();
            }

            // Apply custom GUI skin
            GUI.skin = _customSkin;

            // Draw menu with fade in and scale animation
            Color originalColor = GUI.color;
            GUI.color = new Color(1, 1, 1, _fadeInProgress);

            // Apply UI scale
            Matrix4x4 originalMatrix = GUI.matrix;
            if (_uiScale != 1.0f)
            {
                Vector2 center = new Vector2(Screen.width / 2, Screen.height / 2);
                GUI.matrix = Matrix4x4.TRS(
                    center,
                    Quaternion.identity,
                    new Vector3(_uiScale, _uiScale, 1)
                ) * Matrix4x4.TRS(
                    -center,
                    Quaternion.identity,
                    Vector3.one
                );
            }

            // Animation for menu appearance
            float menuAnim = Mathf.SmoothStep(0, 1, _menuAnimationTime);

            // Ensure initial positioning is smooth
            if (_windowRect.x <= -_windowRect.width)
            {
                _windowRect.x = Mathf.Lerp(-_windowRect.width, 20, menuAnim);
            }

            // Draw the main window without a title (we'll add our own)
            _windowRect = GUI.Window(
                0,
                _windowRect,
                DelegateSupport.ConvertDelegate<GUI.WindowFunction>(DrawWindow),
                "",
                _windowStyle
            );

            // Draw tooltip if needed
            if (_showTooltip && _tooltipTimer > 0.5f)
            {
                Vector2 mousePos = Event.current.mousePosition;
                float tooltipWidth = 250;
                float tooltipHeight = GUI.skin.box.CalcHeight(new GUIContent(_currentTooltip), tooltipWidth);

                // Adjust position to keep on screen
                float tooltipX = mousePos.x + 20;
                if (tooltipX + tooltipWidth > Screen.width)
                    tooltipX = Screen.width - tooltipWidth - 10;

                float tooltipY = mousePos.y + 20;
                if (tooltipY + tooltipHeight > Screen.height)
                    tooltipY = mousePos.y - tooltipHeight - 10;

                Rect tooltipRect = new Rect(tooltipX, tooltipY, tooltipWidth, tooltipHeight);
                GUI.Box(tooltipRect, _currentTooltip, _tooltipStyle ?? GUI.skin.box);
            }

            // Restore original settings
            GUI.matrix = originalMatrix;
            GUI.color = originalColor;

            // Handle escape key to close menu
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                if (_showSettings)
                {
                    _showSettings = false;
                }
                else
                {
                    ToggleUI(false);
                }
                Event.current.Use();
            }
        }

        private void DrawHeaderWithTexturedButtons()
        {
            // Header background
            Rect headerRect = new Rect(0, 0, _windowRect.width, 40);
            GUI.Box(headerRect, "", _headerStyle ?? GUI.skin.box);

            // Title
            Rect titleRect = new Rect(headerRect.x + 10, headerRect.y, headerRect.width - 80, headerRect.height);
            GUI.Label(titleRect, ModInfo.Name, _titleStyle ?? GUI.skin.label);

            // Settings button - using custom texture
            Rect settingsRect = new Rect(headerRect.width - 70, headerRect.y + 5, 30, 30);
            if (_settingsButtonTexture != null && GUI.Button(settingsRect, _settingsButtonTexture, GUIStyle.none))
            {
                _showSettings = true;
            }

            // Close button - using custom texture
            Rect closeRect = new Rect(headerRect.width - 35, headerRect.y + 5, 30, 30);
            if (_closeButtonTexture != null && GUI.Button(closeRect, _closeButtonTexture, GUIStyle.none))
            {
                ToggleUI(false);
            }
        }

        private void DrawWindow(int windowId)
        {
            try
            {
                // Draw our custom header with textured buttons
                DrawHeaderWithTexturedButtons();

                // Main window vertical group - start below header
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                GUILayout.Space(40); // Space for header

                // --- Category Tabs ---
                GUILayout.BeginHorizontal();
                try
                {
                    for (int i = 0; i < _categories.Count; i++)
                    {
                        var style = i == _selectedCategoryIndex ?
                            _categoryButtonActiveStyle : _categoryButtonStyle;

                        if (GUILayout.Button(_categories[i].Name, style, GUILayout.ExpandWidth(true)))
                        {
                            _selectedCategoryIndex = i;
                            _scrollPosition = Vector2.zero;
                        }
                    }
                }
                finally
                {
                    GUILayout.EndHorizontal();
                }

                // --- Content Area ---
                GUILayout.BeginVertical(_panelStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                try
                {
                    if (_showSettings)
                    {
                        DrawSettingsPanel();
                    }
                    else
                    {
                        // Draw based on selected category
                        DrawSelectedCategory();
                    }
                }
                finally
                {
                    GUILayout.EndVertical();
                }

                // Handle window dragging
                HandleWindowDragging();

                GUILayout.EndVertical(); // End main window vertical group
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Window draw error: {ex}");
                GUILayout.EndVertical();
            }
        }

        private void DrawSelectedCategory()
        {
            if (_selectedCategoryIndex < 0 || _selectedCategoryIndex >= _categories.Count)
                return;

            var category = _categories[_selectedCategoryIndex];

            // Different handling based on category name
            switch (category.Name)
            {
                case "Item Manager":
                    DrawItemManager();
                    break;
                case "Online":
                    DrawOnlinePlayers();
                    break;
                case "Teleport Manager":
                    DrawTeleportManager();
                    break;
                default:
                    DrawCommandCategory(category);
                    break;
            }
        }

        private bool _showPlayerMap = false;
        private Texture2D _playerMapTexture;
        private bool _playerMapInitialized = false;
        private float _playerMapZoom = 1.0f;
        private Vector2 _playerMapPanOffset = Vector2.zero;
        private bool _isDraggingPlayerMap = false;
        private Vector2 _playerMapDragStart;
        private Vector2 _playerMapDragStartOffset;
        private Dictionary<string, Color> _playerColors = new Dictionary<string, Color>();

        private void DrawOnlinePlayers()
        {
            try
            {
                float windowWidth = _windowRect.width - 40f;
                float windowHeight = _windowRect.height - 150f;

                // Header with refresh button and map toggle
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Refresh Players", _buttonStyle, GUILayout.Width(150), GUILayout.Height(30)))
                {
                    RefreshOnlinePlayers();
                    _lastPlayerRefreshTime = Time.time;
                    ShowNotification("Online", "Player list refreshed", NotificationType.Info);
                }

                // Map toggle button
                bool showMap = GUILayout.Toggle(_showPlayerMap, "Show Map", _toggleButtonStyle ?? GUI.skin.toggle, GUILayout.Width(100));
                if (showMap != _showPlayerMap)
                {
                    _showPlayerMap = showMap;
                    if (_showPlayerMap)
                        RefreshPlayerMapPositions();
                }

                // Total players count display
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Total Players: {_onlinePlayers.Count}", _labelStyle, GUILayout.Width(150));
                GUILayout.EndHorizontal();

                GUILayout.Space(5); // Reduced spacing

                // Player Grid Layout
                // Check for refresh time
                if (Time.time - _lastPlayerRefreshTime > PLAYER_REFRESH_INTERVAL)
                {
                    RefreshOnlinePlayers();
                    _lastPlayerRefreshTime = Time.time;
                }

                // Grid layout settings
                float playerCardWidth = 210f;
                float playerCardHeight = 140f; // Smaller height for each player card
                int playersPerRow = Mathf.FloorToInt((windowWidth - 10) / (playerCardWidth + 10));
                playersPerRow = Mathf.Max(playersPerRow, 1); // Ensure at least 1 player per row

                // Calculate maximum height for player list area based on whether map is shown
                float playerListMaxHeight = _showPlayerMap ?
                    windowHeight * 0.4f : // Smaller when map is shown
                    windowHeight - 40f;   // Larger when map is hidden

                // Calculate how many rows we need
                int totalRows = Mathf.CeilToInt((float)_onlinePlayers.Count / playersPerRow);
                float contentHeight = totalRows * (playerCardHeight + 10);

                // Create player list scroll view
                _playerScrollPosition = GUILayout.BeginScrollView(
                    _playerScrollPosition,
                    GUILayout.Height(Mathf.Min(contentHeight + 10, playerListMaxHeight))
                );

                if (_onlinePlayers.Count == 0)
                {
                    GUILayout.Label("No players found. Try refreshing the list.", _labelStyle);
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    int playerIndex = 0;

                    foreach (var playerInfo in _onlinePlayers)
                    {
                        if (playerInfo == null || playerInfo.Player == null)
                            continue;

                        // Start new row if needed
                        if (playerIndex > 0 && playerIndex % playersPerRow == 0)
                        {
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                        }

                        // Player card
                        GUILayout.BeginVertical(GUILayout.Width(playerCardWidth), GUILayout.Height(playerCardHeight));

                        // Player card background
                        Rect cardRect = GUILayoutUtility.GetRect(playerCardWidth - 10, playerCardHeight - 10);
                        GUI.Box(cardRect, "", _panelStyle);

                        // Player name header with status indicator
                        Rect headerRect = new Rect(cardRect.x + 5, cardRect.y + 5, cardRect.width - 10, 25);
                        GUI.color = playerInfo.IsLocal ? new Color(0.2f, 0.7f, 1f) : Color.white;

                        GUIStyle nameStyle = new GUIStyle(_commandLabelStyle ?? _labelStyle);
                        nameStyle.fontStyle = FontStyle.Bold;
                        nameStyle.fontSize = 12;
                        nameStyle.alignment = TextAnchor.MiddleCenter;

                        string localTag = playerInfo.IsLocal ? " (YOU)" : "";
                        GUI.Label(headerRect, $"{playerInfo.Name}{localTag}", nameStyle);
                        GUI.color = Color.white;

                        // Status indicator
                        bool isAlive = playerInfo.Health != null && playerInfo.Health.IsAlive;
                        Color statusColor = isAlive ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.2f, 0.2f);
                        string statusText = isAlive ? "ALIVE" : "DEAD";

                        Rect statusRect = new Rect(cardRect.x + 5, cardRect.y + 30, cardRect.width - 10, 20);
                        GUIStyle statusStyle = new GUIStyle(_labelStyle);
                        statusStyle.normal.textColor = statusColor;
                        statusStyle.fontStyle = FontStyle.Bold;
                        statusStyle.alignment = TextAnchor.MiddleCenter;
                        GUI.Label(statusRect, statusText, statusStyle);

                        // Health bar if available
                        if (playerInfo.Health != null)
                        {
                            Rect healthLabelRect = new Rect(cardRect.x + 5, cardRect.y + 50, 50, 20);
                            GUI.Label(healthLabelRect, "Health:", _labelStyle);

                            // Slimmer health bar container (reduced from 10px to 6px height)
                            Rect healthBarRect = new Rect(cardRect.x + 60, cardRect.y + 57, cardRect.width - 70, 6); // Height reduced by 40%

                            // 1. Draw subtle 1px border
                            GUI.color = new Color(0.1f, 0.1f, 0.1f, 1f); // Dark gray border
                            GUI.DrawTexture(new Rect(
                                healthBarRect.x - 1,
                                healthBarRect.y - 1,
                                healthBarRect.width + 2,
                                healthBarRect.height + 2
                            ), Texture2D.whiteTexture);

                            // 2. Draw background
                            GUI.color = new Color(0.25f, 0.25f, 0.25f, 1f); // Medium gray background
                            GUI.DrawTexture(healthBarRect, Texture2D.whiteTexture);

                            // 3. Draw slim health fill (4px height = 66% of container)
                            float healthPercent = playerInfo.Health.CurrentHealth / (float)PlayerHealth.MAX_HEALTH;
                            Rect fillRect = new Rect(
                                healthBarRect.x,
                                healthBarRect.y + 1, // Center vertically
                                healthBarRect.width * healthPercent,
                                healthBarRect.height - 2 // Leaves 1px margin top/bottom
                            );

                            Color healthColor = Color.Lerp(Color.red, Color.green, healthPercent);
                            GUI.color = healthColor;
                            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
                            GUI.color = Color.white;
                        }

                        // Player ID 
                        GUIStyle smallTextStyle = new GUIStyle(_labelStyle);
                        smallTextStyle.fontSize = 12;
                        smallTextStyle.alignment = TextAnchor.MiddleCenter;

                        string steamId = playerInfo.SteamID;

                        Rect idRect = new Rect(cardRect.x + 5, cardRect.y + 75, cardRect.width - 10, 15);
                        GUI.Label(idRect, $"Steam ID: {steamId}", smallTextStyle);

                        // Action buttons - only if not local player
                        if (!playerInfo.IsLocal)
                        {
                            float buttonWidth = (cardRect.width - 20) / 2;
                            float buttonY = cardRect.y + 90;

                            // First row of buttons
                            if (GUI.Button(new Rect(cardRect.x + 5, buttonY, buttonWidth, 20), "Kill", _buttonStyle))
                            {
                                ServerExecuteKillPlayer(playerInfo.Player);
                                ShowNotification("Player", $"Killed {playerInfo.Name}", NotificationType.Success);
                            }

                            if (GUI.Button(new Rect(cardRect.x + 10 + buttonWidth, buttonY, buttonWidth, 20), "Explode", _buttonStyle))
                            {
                                CreateServerSideExplosion(playerInfo.Player.transform.position, 100f, 5f);
                                ShowNotification("Player", $"Exploded {playerInfo.Name}", NotificationType.Success);
                            }

                            // Second row - Teleport and Explosion Loop
                            float button2Y = buttonY + 25;
                            if (GUI.Button(new Rect(cardRect.x + 5, button2Y, buttonWidth, 20), "Teleport To", _buttonStyle))
                            {
                                TeleportPlayer(playerInfo.Player.transform.position);
                                ShowNotification("Player", $"Teleported to {playerInfo.Name}", NotificationType.Success);
                            }

                            bool newLoopState = GUI.Toggle(
                                new Rect(cardRect.x + 10 + buttonWidth, button2Y, buttonWidth, 20),
                                playerInfo.ExplodeLoop,
                                "Loop",
                                _toggleButtonStyle ?? GUI.skin.toggle
                            );

                            if (newLoopState != playerInfo.ExplodeLoop)
                            {
                                playerInfo.ExplodeLoop = newLoopState;
                                if (newLoopState)
                                {
                                    StartExplodeLoop(playerInfo);
                                    ShowNotification("Player", $"Started explosion loop on {playerInfo.Name}", NotificationType.Warning);
                                }
                            }
                        }

                        GUILayout.EndVertical();
                        GUILayout.Space(10);
                        playerIndex++;
                    }

                    // Fill empty slots in the last row for better alignment
                    for (int i = 0; i < playersPerRow - (_onlinePlayers.Count % playersPerRow); i++)
                    {
                        if (_onlinePlayers.Count % playersPerRow == 0) break;
                        GUILayout.BeginVertical(GUILayout.Width(playerCardWidth), GUILayout.Height(playerCardHeight));
                        GUILayout.EndVertical();
                        GUILayout.Space(10);
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();

                // Draw player map as a separate element if enabled
                if (_showPlayerMap)
                {
                    GUILayout.Space(5); // Minimal spacing between player list and map

                    // Add a titled header for the map section
                    GUILayout.BeginVertical();
                    GUIStyle mapTitleStyle = new GUIStyle(_titleStyle ?? _labelStyle);
                    mapTitleStyle.alignment = TextAnchor.MiddleCenter;
                    GUILayout.Label("Player Positions", mapTitleStyle);

                    // Fixed height for map
                    float mapHeight = 200f;
                    Rect mapContainerRect = GUILayoutUtility.GetRect(_windowRect.width - 40, mapHeight);
                    GUI.Box(mapContainerRect, "", _panelStyle);

                    // Draw the map with proper integration
                    DrawPlayerPositionsMap(mapContainerRect);

                    GUILayout.EndVertical();
                }

                GUILayout.Space(5); // Reduced spacing

                // Global actions row at the bottom - always visible
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Kill All", _buttonStyle, GUILayout.Height(30)))
                {
                    KillAllPlayersCommand(null);
                }

                if (GUILayout.Button("Explode All", _buttonStyle, GUILayout.Height(30)))
                {
                    CreateExplosion(new string[] { "all", "99999999999999", "2" });
                }

                if (GUILayout.Button("Teleport All To Me", _buttonStyle, GUILayout.Height(30)))
                {
                    TeleportAllPlayersToMe();
                }

                if (GUILayout.Button("Increase Lobby Size", _buttonStyle, GUILayout.Height(30)))
                {
                    ApplyLobbyPatch();
                }

                GUILayout.EndHorizontal();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in DrawOnlinePlayers: {ex.Message}");
            }
        }
        private void DrawPlayerPositionsMap(Rect mapRect)
        {
            try
            {
                // Inner map container with padding
                Rect innerMapRect = new Rect(
                    mapRect.x + 5,
                    mapRect.y + 5,
                    mapRect.width - 10,
                    mapRect.height - 10
                );

                // Draw background
                GUI.color = new Color(0.1f, 0.1f, 0.15f, 1f);
                GUI.DrawTexture(innerMapRect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                // Initialize map if needed
                if (_playerMapTexture == null || !_playerMapInitialized)
                {
                    InitializePlayerMap();
                }

                if (_playerMapTexture != null)
                {
                    // Calculate display rectangle with proper aspect ratio
                    float texRatio = (float)_playerMapTexture.width / _playerMapTexture.height;
                    float rectRatio = innerMapRect.width / innerMapRect.height;

                    Rect baseDisplayRect;
                    if (texRatio > rectRatio)
                    {
                        // Fit to width
                        float height = innerMapRect.width / texRatio;
                        baseDisplayRect = new Rect(
                            innerMapRect.x,
                            innerMapRect.y + (innerMapRect.height - height) / 2,
                            innerMapRect.width,
                            height
                        );
                    }
                    else
                    {
                        // Fit to height
                        float width = innerMapRect.height * texRatio;
                        baseDisplayRect = new Rect(
                            innerMapRect.x + (innerMapRect.width - width) / 2,
                            innerMapRect.y,
                            width,
                            innerMapRect.height
                        );
                    }

                    // Apply zoom and pan using your existing method
                    Rect displayRect = ApplyPlayerMapZoomAndPan(baseDisplayRect, innerMapRect);

                    // Create a strict clipping area to prevent drawing outside the map container
                    GUI.BeginClip(innerMapRect);

                    // Adjust coordinates for clipping
                    Rect adjustedDisplayRect = new Rect(
                        displayRect.x - innerMapRect.x,
                        displayRect.y - innerMapRect.y,
                        displayRect.width,
                        displayRect.height
                    );

                    // Draw map texture
                    GUI.DrawTexture(adjustedDisplayRect, _playerMapTexture, ScaleMode.StretchToFill);

                    // Draw player markers - adjusted for clipping
                    foreach (var playerInfo in _onlinePlayers)
                    {
                        if (playerInfo == null || playerInfo.Player == null)
                            continue;

                        // Get player position and convert to map position
                        Vector3 worldPos = playerInfo.Player.transform.position;
                        Vector2 normalizedPos = WorldToMapPosition(worldPos);

                        // Convert normalized position to display coordinates
                        Vector2 markerPos = new Vector2(
                            adjustedDisplayRect.x + normalizedPos.x * adjustedDisplayRect.width,
                            adjustedDisplayRect.y + normalizedPos.y * adjustedDisplayRect.height
                        );

                        // Draw marker
                        DrawPlayerMapMarker(markerPos, playerInfo.IsLocal ? Color.white : Color.red, playerInfo.IsLocal, playerInfo.Name);
                    }

                    // End clip area
                    GUI.EndClip();

                    // *** FIXED: Draw legend and controls outside clipped area with proper visibility ***
                    DrawPlayerMapLegend(innerMapRect);
                    DrawPlayerMapZoomControls(innerMapRect);

                    // Handle map interaction
                    HandlePlayerMapInteraction(innerMapRect, displayRect, baseDisplayRect);
                }
                else
                {
                    // No map texture available
                    GUIStyle msgStyle = new GUIStyle(GUI.skin.label);
                    msgStyle.alignment = TextAnchor.MiddleCenter;
                    msgStyle.fontSize = 16;
                    msgStyle.normal.textColor = Color.white;

                    GUI.Label(innerMapRect, "Map not available", msgStyle);

                    if (GUI.Button(
                        new Rect(innerMapRect.x + innerMapRect.width / 2 - 60,
                                innerMapRect.y + innerMapRect.height / 2 + 30,
                                120, 30),
                        "Initialize Map", _buttonStyle))
                    {
                        InitializePlayerMap();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error drawing player map: {ex.Message}");
            }
        }


        private void HandlePlayerMapInteraction(Rect mapContainer, Rect displayRect, Rect baseRect)
        {
            // Check if mouse is over the map
            if (mapContainer.Contains(Event.current.mousePosition))
            {
                // Calculate relative mouse position
                Vector2 normalizedPos = new Vector2(
                    Mathf.Clamp01((Event.current.mousePosition.x - displayRect.x) / displayRect.width),
                    Mathf.Clamp01((Event.current.mousePosition.y - displayRect.y) / displayRect.height)
                );

                // Handle middle-click drag (panning)
                if (Event.current.type == EventType.MouseDown && Event.current.button == 2)
                {
                    _isDraggingPlayerMap = true;
                    _playerMapDragStart = Event.current.mousePosition;
                    _playerMapDragStartOffset = _playerMapPanOffset;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseUp && Event.current.button == 2)
                {
                    _isDraggingPlayerMap = false;
                    Event.current.Use();
                }

                // Handle drag movement
                if (_isDraggingPlayerMap && Event.current.type == EventType.MouseDrag)
                {
                    Vector2 delta = Event.current.mousePosition - _playerMapDragStart;
                    _playerMapPanOffset = _playerMapDragStartOffset + delta / _playerMapZoom;
                    Event.current.Use();
                }

                // Handle scroll wheel zooming
                if (Event.current.type == EventType.ScrollWheel)
                {
                    // Only allow zooming in if already at or below default zoom
                    if (Event.current.delta.y > 0 && _playerMapZoom <= 1.0f)
                    {
                        // Don't allow zooming out more
                    }
                    else
                    {
                        float zoomDelta = -Event.current.delta.y * 0.05f;
                        float newZoom = Mathf.Clamp(_playerMapZoom + zoomDelta, 1.0f, 3.0f);

                        // Adjust pan to zoom toward mouse position
                        Vector2 mousePos = Event.current.mousePosition;
                        Vector2 mapCenter = new Vector2(
                            baseRect.x + baseRect.width / 2,
                            baseRect.y + baseRect.height / 2
                        );

                        _playerMapZoom = newZoom;
                    }

                    Event.current.Use();
                }
            }
            else
            {
                // Cancel dragging if mouse leaves map
                if (_isDraggingPlayerMap && Event.current.type == EventType.MouseUp)
                {
                    _isDraggingPlayerMap = false;
                }
            }
        }

        private Rect ApplyPlayerMapZoomAndPan(Rect baseRect, Rect containerRect)
        {
            // Calculate the zoom-adjusted size
            float zoomedWidth = baseRect.width * _playerMapZoom;
            float zoomedHeight = baseRect.height * _playerMapZoom;

            // Calculate center position
            float centerX = baseRect.x + baseRect.width / 2;
            float centerY = baseRect.y + baseRect.height / 2;

            // Apply pan offset with improved constraints
            float maxPanX = Math.Max(0, (zoomedWidth - containerRect.width) / 2);
            float maxPanY = Math.Max(0, (zoomedHeight - containerRect.height) / 2);

            // Clamp pan offset to prevent going outside container bounds
            _playerMapPanOffset.x = Mathf.Clamp(_playerMapPanOffset.x, -maxPanX, maxPanX);
            _playerMapPanOffset.y = Mathf.Clamp(_playerMapPanOffset.y, -maxPanY, maxPanY);

            centerX += _playerMapPanOffset.x;
            centerY += _playerMapPanOffset.y;

            // Calculate the zoomed rect with the adjusted center
            Rect zoomedRect = new Rect(
                centerX - zoomedWidth / 2,
                centerY - zoomedHeight / 2,
                zoomedWidth,
                zoomedHeight
            );

            // Improved container bounds checking
            if (zoomedWidth <= containerRect.width)
            {
                // Center horizontally if smaller than container
                zoomedRect.x = containerRect.x + (containerRect.width - zoomedWidth) / 2;
            }
            else
            {
                // Constrain to container edges
                if (zoomedRect.x > containerRect.x)
                    zoomedRect.x = containerRect.x;
                if (zoomedRect.x + zoomedWidth < containerRect.x + containerRect.width)
                    zoomedRect.x = containerRect.x + containerRect.width - zoomedWidth;
            }

            if (zoomedHeight <= containerRect.height)
            {
                // Center vertically if smaller than container
                zoomedRect.y = containerRect.y + (containerRect.height - zoomedHeight) / 2;
            }
            else
            {
                // Constrain to container edges
                if (zoomedRect.y > containerRect.y)
                    zoomedRect.y = containerRect.y;
                if (zoomedRect.y + zoomedHeight < containerRect.y + containerRect.height)
                    zoomedRect.y = containerRect.y + containerRect.height - zoomedHeight;
            }

            return zoomedRect;
        }

        private void DrawPlayerMapMarker(Vector2 position, Color color, bool isLocal, string playerName)
        {
            try
            {
                // Marker size (larger for local player)
                float markerSize = isLocal ? 10f : 8f;

                // Create the marker rect
                Rect markerRect = new Rect(
                    position.x - markerSize / 2,
                    position.y - markerSize / 2,
                    markerSize,
                    markerSize
                );

                // Draw shadow for better visibility
                GUI.color = new Color(0, 0, 0, 0.5f);
                GUI.DrawTexture(new Rect(markerRect.x + 1, markerRect.y + 1, markerRect.width, markerRect.height),
                                Texture2D.whiteTexture);

                // Draw marker in player color
                GUI.color = color;
                GUI.DrawTexture(markerRect, Texture2D.whiteTexture);

                // For local player, add white border
                if (isLocal)
                {
                    GUI.color = Color.white;
                    // Top
                    GUI.DrawTexture(new Rect(markerRect.x - 1, markerRect.y - 1, markerRect.width + 2, 1), Texture2D.whiteTexture);
                    // Bottom
                    GUI.DrawTexture(new Rect(markerRect.x - 1, markerRect.y + markerRect.height, markerRect.width + 2, 1), Texture2D.whiteTexture);
                    // Left
                    GUI.DrawTexture(new Rect(markerRect.x - 1, markerRect.y - 1, 1, markerRect.height + 2), Texture2D.whiteTexture);
                    // Right
                    GUI.DrawTexture(new Rect(markerRect.x + markerRect.width, markerRect.y - 1, 1, markerRect.height + 2), Texture2D.whiteTexture);
                }

                // Reset color
                GUI.color = Color.white;

                // Show tooltip on hover
                if (markerRect.Contains(Event.current.mousePosition))
                {
                    // Tooltip positioning
                    float tooltipWidth = 120;
                    float tooltipHeight = 25;

                    Rect tooltipRect = new Rect(
                        position.x + markerSize,
                        position.y - tooltipHeight / 2,
                        tooltipWidth,
                        tooltipHeight
                    );

                    // Background
                    GUI.color = new Color(0, 0, 0.15f, 0.9f);
                    GUI.DrawTexture(tooltipRect, Texture2D.whiteTexture);

                    // Border
                    GUI.color = color;
                    // Top
                    GUI.DrawTexture(new Rect(tooltipRect.x, tooltipRect.y, tooltipRect.width, 1), Texture2D.whiteTexture);
                    // Bottom
                    GUI.DrawTexture(new Rect(tooltipRect.x, tooltipRect.y + tooltipRect.height - 1, tooltipRect.width, 1), Texture2D.whiteTexture);
                    // Left
                    GUI.DrawTexture(new Rect(tooltipRect.x, tooltipRect.y, 1, tooltipRect.height), Texture2D.whiteTexture);
                    // Right
                    GUI.DrawTexture(new Rect(tooltipRect.x + tooltipRect.width - 1, tooltipRect.y, 1, tooltipRect.height), Texture2D.whiteTexture);

                    GUI.color = Color.white;

                    // Player name text
                    GUIStyle tooltipStyle = new GUIStyle(_labelStyle);
                    tooltipStyle.normal.textColor = Color.white;
                    tooltipStyle.alignment = TextAnchor.MiddleCenter;
                    tooltipStyle.fontSize = 12;

                    GUI.Label(tooltipRect, playerName, tooltipStyle);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error drawing player marker: {ex.Message}");
            }
        }

        // Initialize player map (using the existing map if available)
        private void InitializePlayerMap()
        {
            try
            {
                // Try to use existing map texture if available
                if (_mapTexture != null)
                {
                    _playerMapTexture = _mapTexture;
                    _playerMapInitialized = true;
                    return;
                }

                // Otherwise try to capture from the game
                var mapApp = Resources.FindObjectsOfTypeAll<Il2CppScheduleOne.UI.Phone.Map.MapApp>()
                    .FirstOrDefault();

                if (mapApp != null && mapApp.MainMapSprite != null && mapApp.MainMapSprite.texture != null)
                {
                    _playerMapTexture = CreateReadableTexture(mapApp.MainMapSprite.texture);
                    _playerMapInitialized = true;
                }
                else
                {
                    // Create fallback texture if map cannot be obtained
                    _playerMapTexture = new Texture2D(1, 1);
                    _playerMapTexture.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.3f));
                    _playerMapTexture.Apply();
                    _playerMapInitialized = true;
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error initializing player map: {ex.Message}");
            }
        }

        // Helper method to refresh player map positions
        private void RefreshPlayerMapPositions()
        {
            // Just refresh player list
            RefreshOnlinePlayers();

            // Ensure map is initialized
            if (!_playerMapInitialized)
            {
                InitializePlayerMap();
            }

            // Assign unique colors to new players
            foreach (var player in _onlinePlayers)
            {
                if (player != null && !_playerColors.ContainsKey(player.SteamID))
                {
                    _playerColors[player.SteamID] = GetUniqueColor(player.SteamID);
                }
            }
        }

        // Convert world position to map position
        private Vector2 WorldToMapPosition(Vector3 worldPos)
        {
            try
            {
                // Using the same conversion method from your teleport code
                Vector2 _mapDimensions = new Vector2(2048f, 2048f);
                float _scaleFactor = 5.006356f;

                // Calculate normalized map coordinates (0-1 range)
                float normalizedX = (worldPos.x * _scaleFactor / _mapDimensions.x) + 0.5f;
                float normalizedZ = 0.5f - (worldPos.z * _scaleFactor / _mapDimensions.y);

                // Clamp to valid range
                normalizedX = Mathf.Clamp01(normalizedX);
                normalizedZ = Mathf.Clamp01(normalizedZ);

                return new Vector2(normalizedX, normalizedZ);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error converting world to map position: {ex.Message}");
                return new Vector2(0.5f, 0.5f); // Default to center of map
            }
        }

        // Draw map legend

        private void DrawPlayerMapLegend(Rect mapRect)
        {
            try
            {
                // Create a black background panel for the legend
                Rect legendRect = new Rect(
                    mapRect.x + 10,
                    mapRect.y + 10,
                    130,
                    70
                );

                // Solid black background
                GUI.color = new Color(0, 0, 0, 1f);
                GUI.DrawTexture(legendRect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                // Add "Legend" header
                GUIStyle headerStyle = new GUIStyle();
                headerStyle.normal.textColor = Color.white;
                headerStyle.alignment = TextAnchor.MiddleCenter;
                headerStyle.fontStyle = FontStyle.Bold;
                headerStyle.fontSize = 14;

                GUI.Label(
                    new Rect(legendRect.x, legendRect.y + 5, legendRect.width, 20),
                    "Legend",
                    headerStyle
                );

                // Label style
                GUIStyle labelStyle = new GUIStyle();
                labelStyle.normal.textColor = Color.white;
                labelStyle.alignment = TextAnchor.MiddleLeft;
                labelStyle.fontSize = 12;

                // You marker
                Rect youLabelRect = new Rect(legendRect.x + 40, legendRect.y + 30, 60, 20);
                GUI.Label(youLabelRect, "You", labelStyle);

                Rect youMarkerRect = new Rect(legendRect.x + 15, legendRect.y + 30, 15, 15);
                GUI.color = Color.white;
                GUI.DrawTexture(youMarkerRect, Texture2D.whiteTexture);

                // Others marker
                Rect othersLabelRect = new Rect(legendRect.x + 40, legendRect.y + 50, 60, 20);
                GUI.Label(othersLabelRect, "Others", labelStyle);

                Rect othersMarkerRect = new Rect(legendRect.x + 15, legendRect.y + 50, 15, 15);
                GUI.color = Color.red;
                GUI.DrawTexture(othersMarkerRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error drawing map legend: {ex.Message}");
            }
        }


        // Improved zoom controls
        private void DrawPlayerMapZoomControls(Rect mapRect)
        {
            try
            {
                // Controls container - solid black background
                Rect controlsRect = new Rect(
                    mapRect.x + mapRect.width - 110,
                    mapRect.y + 10,
                    100,
                    90
                );

                // Solid black background
                GUI.color = new Color(0, 0, 0, 1f);
                GUI.DrawTexture(controlsRect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                // Zoom indicator
                GUIStyle zoomStyle = new GUIStyle();
                zoomStyle.normal.textColor = Color.white;
                zoomStyle.alignment = TextAnchor.MiddleCenter;
                zoomStyle.fontSize = 16;
                zoomStyle.fontStyle = FontStyle.Bold;

                GUI.Label(
                    new Rect(controlsRect.x, controlsRect.y + 5, controlsRect.width, 25),
                    $"Zoom: {_playerMapZoom:F1}x",
                    zoomStyle
                );

                // Reset button
                if (GUI.Button(
                    new Rect(controlsRect.x + 10, controlsRect.y + 55, 80, 25),
                    "Reset",
                    _buttonStyle))
                {
                    _playerMapZoom = 1.0f;
                    _mapPanOffset = Vector2.zero;
                }

                // Zoom buttons
                if (GUI.Button(
                    new Rect(controlsRect.x + 55, controlsRect.y + 30, 35, 25),
                    "+",
                    _buttonStyle))
                {
                    _playerMapZoom = Mathf.Clamp(_playerMapZoom + 0.2f, 1.0f, 3.0f);
                }

                GUI.enabled = _playerMapZoom > 1.0f;
                if (GUI.Button(
                    new Rect(controlsRect.x + 10, controlsRect.y + 30, 35, 25),
                    "-",
                    _buttonStyle))
                {
                    _playerMapZoom = Mathf.Clamp(_playerMapZoom - 0.2f, 1.0f, 3.0f);
                }
                GUI.enabled = true;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error drawing zoom controls: {ex.Message}");
            }
        }


        // Generate a unique color based on player ID
        private Color GetUniqueColor(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return Color.gray;

            // If the player is local, use green
            var localPlayer = FindLocalPlayer();
            if (localPlayer != null && localPlayer.name.Contains(identifier))
                return new Color(0.2f, 0.8f, 0.2f); // Green

            // Hash the identifier to create a consistent color
            int hash = 0;
            foreach (char c in identifier)
            {
                hash = (hash * 31) + c;
            }

            // Use the hash to create a HSV color with consistent saturation and value
            float hue = (hash % 360) / 360f;
            return Color.HSVToRGB(hue, 0.8f, 0.8f);
        }

        // Teleport all players to the local player's position
        private void TeleportAllPlayersToMe()
        {
            try
            {
                var localPlayer = FindLocalPlayer();
                if (localPlayer == null)
                {
                    ShowNotification("Teleport", "Local player not found", NotificationType.Error);
                    return;
                }

                Vector3 myPosition = localPlayer.transform.position;
                int teleportCount = 0;

                foreach (var playerInfo in _onlinePlayers)
                {
                    if (playerInfo == null || playerInfo.Player == null || playerInfo.IsLocal)
                        continue;

                    try
                    {

                        // Method 1: Try using SetTransform component
                        var setTransform = playerInfo.Player.GetComponent<Il2CppScheduleOne.DevUtilities.SetTransform>();
                        if (setTransform == null)
                        {
                            // Add the component if it doesn't exist
                            setTransform = playerInfo.Player.gameObject.AddComponent<Il2CppScheduleOne.DevUtilities.SetTransform>();
                        }

                        if (setTransform != null)
                        {
                            // Slight random offset to avoid players stacking on top of each other
                            Vector3 offset = new Vector3(
                                UnityEngine.Random.Range(-1.5f, 1.5f),
                                0,
                                UnityEngine.Random.Range(-1.5f, 1.5f)
                            );

                            // Configure the SetTransform component
                            setTransform.SetOnUpdate = true;
                            setTransform.SetPosition = true;
                            setTransform.LocalPosition = myPosition + offset;
                            playerInfo.Player.Update();

                            // Call Set to apply the transform immediately
                            setTransform.Set();

                            LoggerInstance.Msg($"Applied SetTransform to {playerInfo.Player.name}");
                            teleportCount++;
                        }
                        else
                        {
                            // Fallback method: Try the PlayerMovement.Teleport method
                            var playerMovement = playerInfo.Player.GetComponent<Il2CppScheduleOne.PlayerScripts.PlayerMovement>();
                            if (playerMovement != null)
                            {
                                Vector3 offset = new Vector3(
                                    UnityEngine.Random.Range(-1.5f, 1.5f),
                                    0,
                                    UnityEngine.Random.Range(-1.5f, 1.5f)
                                );
                                playerMovement.Teleport(myPosition + offset);
                                LoggerInstance.Msg($"Used PlayerMovement.Teleport for {playerInfo.Player.name}");
                                teleportCount++;
                            }
                            else
                            {
                                // Last resort: direct transform position modification
                                Vector3 offset = new Vector3(
                                    UnityEngine.Random.Range(-1.5f, 1.5f),
                                    0,
                                    UnityEngine.Random.Range(-1.5f, 1.5f)
                                );
                                playerInfo.Player.transform.position = myPosition + offset;
                                LoggerInstance.Msg($"Used direct transform position for {playerInfo.Player.name}");
                                teleportCount++;
                            }
                        }
                    }
                    catch (Exception playerEx)
                    {
                        LoggerInstance.Error($"Failed to teleport {playerInfo.Player.name}: {playerEx.Message}");
                    }
                }

                if (teleportCount > 0)
                {
                    ShowNotification("Teleport", $"Teleported {teleportCount} players to your position", NotificationType.Success);
                }
                else
                {
                    ShowNotification("Teleport", "No other players to teleport", NotificationType.Info);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in TeleportAllPlayersToMe: {ex.Message}");
                ShowNotification("Error", "Failed to teleport players", NotificationType.Error);
            }
        }

        private void SaveWindowPosition()
        {
            try
            {
                if (_menuPosXEntry == null || _menuPosYEntry == null)
                {
                    LoggerInstance.Error("Menu position entries are null! Cannot save position.");
                    return;
                }

                // Set the values
                _menuPosXEntry.Value = _windowRect.x;
                _menuPosYEntry.Value = _windowRect.y;

                // Save to file
                _settingsCategory.SaveToFile(false); // Pass false to prevent triggering the OnSaved event

                LoggerInstance.Msg($"Window position saved: X={_windowRect.x}, Y={_windowRect.y}");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to save window position: {ex.Message}");
                LoggerInstance.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        private void HandleWindowDragging()
        {
            try
            {
                // Make the drag area just the header section
                Rect dragRect = new Rect(0, 0, _windowRect.width, 40);

                // Current event
                Event current = Event.current;

                // Start dragging
                if (current.type == EventType.MouseDown &&
                    current.button == 0 &&
                    dragRect.Contains(current.mousePosition))
                {
                    _isDragging = true;
                    _dragOffset = current.mousePosition - new Vector2(_windowRect.x, _windowRect.y);
                    current.Use();
                }
                // End dragging
                else if (_isDragging && current.type == EventType.MouseUp)
                {
                    _isDragging = false;

                    // Save position
                    SaveWindowPosition();

                    current.Use();
                }
                // Handle dragging - simplified approach
                else if (_isDragging)
                {
                    // Update position regardless of event type while dragging is active
                    // This provides smoother dragging by updating on every frame
                    _windowRect.x = current.mousePosition.x - _dragOffset.x;
                    _windowRect.y = current.mousePosition.y - _dragOffset.y;

                    // Keep window on screen
                    _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
                    _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);

                    // Force repaint
                    GUI.changed = true;

                    // Only consume the event for mouse movement
                    if (current.type == EventType.MouseDrag)
                    {
                        current.Use();
                    }

                    // Check if mouse button is released outside normal events
                    if (!Input.GetMouseButton(0))
                    {
                        _isDragging = false;

                        // Save position when drag ends
                        _menuPosXEntry.Value = _windowRect.x;
                        _menuPosYEntry.Value = _windowRect.y;
                        _settingsCategory.SaveToFile();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Window dragging error: {ex.Message}");
                _isDragging = false;
            }
        }

        // Enhanced socket finding method
        private Il2CppFishySteamworks.Server.ServerSocket FindBestServerSocket()
        {
            try
            {
                var transports = Resources.FindObjectsOfTypeAll<Il2CppFishySteamworks.FishySteamworks>();

                if (transports != null && transports.Length > 0)
                {
                    LoggerInstance.Msg($"Found {transports.Length} FishySteamworks transports");
                    return transports[0]._server;
                }
                else
                {
                    LoggerInstance.Error("Could not find FishySteamworks transport");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error finding server socket: {ex.Message}");
                return null;
            }
        }

        private void RefreshOnlinePlayers()
        {
            try
            {
                _onlinePlayers.Clear();
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
                        LoggerInstance.Error($"Error mapping connections to Steam IDs: {ex.Message}");
                    }
                }

                foreach (var player in playerList)
                {
                    if (player == null) continue;

                    bool isLocal = IsLocalPlayer(player);
                    var health = GetPlayerHealth(player);

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

                    _onlinePlayers.Add(playerInfo);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error refreshing online players: {ex.Message}");
            }
        }

        private void SubscribeToPlayerDeathEvent()
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
                        LoggerInstance.Msg("Successfully subscribed to player death event");
                    }
                    else
                    {
                        LoggerInstance.Error("Player health or onDie event is null");
                    }
                }
                else
                {
                    LoggerInstance.Error("Local player not found for death event subscription");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to subscribe to player death event: {ex.Message}");
            }
        }

        private void OnPlayerDeath()
        {
            // Disable freecam on death.
            if (_freeCamEnabled)
            {
                togglePlayerControllable(true);
                _freeCamEnabled = false;
            }

            // Close the menu if it's open.
            if(_uiVisible)
            {
                ToggleUI(false);
            }

            _needsStyleRecreation = true;
            _needsStyleRecreation = true;
        }

        // Add this method to your menu initialization
        private void ApplyLobbyPatch()
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
                        LoggerInstance.Msg($"Current maximum clients: {fishyTransport._maximumClients}");
                        fishyTransport._maximumClients = 16; // Change to your desired value
                        LoggerInstance.Msg($"Changed maximum clients to: {fishyTransport._maximumClients}");

                        // Also modify server socket if available
                        if (fishyTransport._server != null)
                        {
                            fishyTransport._server._maximumClients = 16;
                            LoggerInstance.Msg("Also updated server socket maximum clients");
                        }

                        ShowNotification("Lobby Size", "Maximum players increased to 16", NotificationType.Success);
                    }
                }
                else
                {
                    LoggerInstance.Error("Could not find FishySteamworks transport");
                    ShowNotification("Lobby Size", "Failed to modify - transport not found", NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error applying lobby size patch: {ex.Message}");
                ShowNotification("Lobby Size", "Failed to modify - " + ex.Message, NotificationType.Error);
            }
        }

        private void DrawCommandCategory(CommandCategory category)
        {
            try
            {
                // Special handling for Player Exploits category
                if (category.Name == "Player Exploits")
                {
                    DrawPlayerExploitsUI(category);
                    return;
                }

                // Original implementation for other categories
                float windowWidth = _windowRect.width - 40f;

                // Manual scroll view setup
                _scrollPosition = GUI.BeginScrollView(
                    new Rect(20, 100, windowWidth, _windowRect.height - 150),
                    _scrollPosition,
                    new Rect(0, 0, windowWidth - 20, category.Commands.Count * 100f)
                );

                float yOffset = 0f;
                foreach (var command in category.Commands)
                {
                    // Command container rectangle
                    Rect commandRect = new Rect(0, yOffset, windowWidth - 40f, 90f);
                    GUI.Box(commandRect, "", _panelStyle);

                    // Command Name
                    GUI.Label(
                        new Rect(commandRect.x + 10f, commandRect.y + 5f, 200f, 25f),
                        command.Name,
                        _commandLabelStyle ?? _labelStyle
                    );

                    // Parameters handling
                    float paramX = commandRect.x + 220f;
                    if (command.Parameters.Count > 0)
                    {
                        foreach (var param in command.Parameters)
                        {
                            Rect paramRect = new Rect(paramX, commandRect.y + 5f, 120f, 25f);

                            if (param.Type == ParameterType.Input)
                            {
                                // Unique key for each parameter
                                string paramKey = $"param_{command.Name}_{param.Name}";

                                // Custom text field
                                if (!_textFields.TryGetValue(paramKey, out var textField))
                                {
                                    textField = new CustomTextField(param.Value ?? "", _inputFieldStyle ?? GUI.skin.textField);
                                    _textFields[paramKey] = textField;
                                }

                                param.Value = textField.Draw(paramRect);
                            }
                            else if (param.Type == ParameterType.Dropdown)
                            {
                                // Dropdown-like button
                                if (GUI.Button(paramRect, param.Value ?? "Select", _buttonStyle))
                                {
                                    ShowDropdownMenu(param);
                                }
                            }

                            paramX += 130f;
                        }
                    }

                    // Execute Button
                    Rect executeRect = new Rect(paramX, commandRect.y + 5f, 120f, 25f);
                    if (GUI.Button(executeRect, "Execute", _buttonStyle))
                    {
                        ExecuteCommand(command);
                    }

                    // Optional description
                    if (!string.IsNullOrEmpty(command.Description))
                    {
                        GUI.Label(
                            new Rect(commandRect.x + 10f, commandRect.y + 35f, windowWidth - 60f, 50f),
                            command.Description,
                            _tooltipStyle ?? GUI.skin.label
                        );
                    }

                    // Increment Y offset for next command
                    yOffset += 100f;
                }

                GUI.EndScrollView();
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error in DrawCommandCategory: {ex}");
            }
        }

        private void DrawItemManager()
        {
            try
            {
                float windowWidth = _windowRect.width - 40f;
                float windowHeight = _windowRect.height - 150f;

                // Vertical offset to move everything down
                float verticalOffset = 90f;

                // Add a header panel for the options
                Rect headerRect = new Rect(20f, 20f + verticalOffset, windowWidth - 40f, 50f);
                GUI.Box(headerRect, "", _panelStyle);

                // Center and space out the buttons in the header
                float buttonWidth = 120f;
                float buttonHeight = 30f;
                float buttonSpacing = 20f;
                float startX = headerRect.x + (headerRect.width - (buttonWidth * 3 + buttonSpacing * 2)) / 2;

                // Set Quality Button (first in the row)
                Rect setQualityRect = new Rect(
                    startX,
                    headerRect.y + (headerRect.height - buttonHeight) / 2,
                    buttonWidth,
                    buttonHeight
                );

                if (!_itemCache.TryGetValue("qualities", out var qualities)) return;
                if (GUI.Button(setQualityRect, "Set Quality", _buttonStyle))
                {
                    // Convert selected quality index to enum value
                    var quality = (Il2CppScheduleOne.ItemFramework.EQuality)_selectedQualityIndex;
                    SetItemQuality(quality);
                }

                // Package Item Button (second in the row)
                Rect packageItemRect = new Rect(
                    setQualityRect.x + buttonWidth + buttonSpacing,
                    setQualityRect.y,
                    buttonWidth,
                    buttonHeight
                );

                if (GUI.Button(packageItemRect, "Package Item", _buttonStyle))
                {
                    PackageProductCommand(_packageType);   
                }

                // Package Type Button (third in the row)
                Rect packageTypeRect = new Rect(
                    packageItemRect.x + buttonWidth + buttonSpacing,
                    packageItemRect.y,
                    buttonWidth,
                    buttonHeight
                );

                // Toggle between baggie/jar on click
                string packageTypeText = _packageType == "baggie" ? "Type: Baggie" : "Type: Jar";
                if (GUI.Button(packageTypeRect, packageTypeText, _buttonStyle))
                {
                    _packageType = _packageType == "baggie" ? "jar" : "baggie";
                    ShowNotification("Package Type", $"Set to {_packageType}", NotificationType.Info);
                }

                // Search Bar - moved down for better spacing
                float searchBarY = headerRect.y + headerRect.height + 20f;
                Rect searchRect = new Rect(20f, searchBarY, windowWidth - 40f, 30f);
                GUI.Label(new Rect(searchRect.x, searchRect.y - 25f, 100f, 25f), "Search:", _labelStyle);

                // Use custom text field for search
                if (!_textFields.TryGetValue("itemSearch", out var searchField))
                {
                    searchField = new CustomTextField(_itemSearchText, _searchBoxStyle ?? GUI.skin.textField);
                    _textFields["itemSearch"] = searchField;
                }
                _itemSearchText = searchField.Draw(searchRect);

                // Item Grid Scroll View - adjusted position to account for new header
                float scrollViewY = searchRect.y + searchRect.height + 20f;
                Rect scrollViewRect = new Rect(
                    20f,
                    scrollViewY,
                    windowWidth - 40f,
                    windowHeight - (scrollViewY - verticalOffset) - 120f // Adjust height to fit
                );

                // Calculate dynamic content height based on filtered items
                if (!_itemCache.TryGetValue("items", out var allItems)) return;

                var filteredItems = allItems
                    .Where(item => string.IsNullOrEmpty(_itemSearchText) ||
                                   item.IndexOf(_itemSearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                const float itemSize = 100f;
                const float spacing = 25;
                int columns = Mathf.FloorToInt((scrollViewRect.width - spacing) / (itemSize + spacing));
                columns = Mathf.Max(columns, 5); // Force 5 columns

                // Calculate total rows and content height
                int totalRows = Mathf.CeilToInt((float)filteredItems.Count / columns);
                float contentHeight = totalRows * (itemSize + spacing) + spacing;

                // Create content rect with calculated height
                Rect contentRect = new Rect(0f, 0f, scrollViewRect.width - 20f, contentHeight);

                // Begin scroll view with dynamic content height
                _itemScrollPosition = GUI.BeginScrollView(
                    scrollViewRect,
                    _itemScrollPosition,
                    contentRect,
                    false,  // Horizontal scrolling
                    true    // Vertical scrolling
                );

                float yPos = 0f;
                for (int i = 0; i < filteredItems.Count; i += columns)
                {
                    for (int j = 0; j < columns; j++)
                    {
                        int index = i + j;
                        if (index >= filteredItems.Count) break;

                        Rect itemRect = new Rect(
                            j * (itemSize + spacing),
                            yPos,
                            itemSize,
                            itemSize
                        );

                        string itemName = filteredItems[index];
                        DrawItemButton(itemName, itemRect);
                    }

                    yPos += itemSize + spacing;
                }

                GUI.EndScrollView();

                // Item Details Panel
                if (!string.IsNullOrEmpty(_selectedItemId))
                {
                    Rect detailsRect = new Rect(
                        20f,
                        windowHeight - 100f + verticalOffset,
                        windowWidth - 40f,
                        90f
                    );

                    GUI.Box(detailsRect, "", _panelStyle);

                    // Selected Item Label
                    GUI.Label(
                        new Rect(detailsRect.x + 10f, detailsRect.y + 10f, 200f, 25f),
                        "Selected Item: " + GetDisplayNameFromId(_selectedItemId),
                        _labelStyle
                    );

                    // Quantity Input
                    GUI.Label(
                        new Rect(detailsRect.x + 10f, detailsRect.y + 40f, 100f, 25f),
                        "Quantity:",
                        _labelStyle
                    );

                    Rect quantityRect = new Rect(detailsRect.x + 120f, detailsRect.y + 40f, 100f, 25f);
                    if (!_textFields.TryGetValue("quantityInput", out var quantityField))
                    {
                        quantityField = new CustomTextField(_quantityInput, _inputFieldStyle ?? GUI.skin.textField);
                        _textFields["quantityInput"] = quantityField;
                    }
                    _quantityInput = quantityField.Draw(quantityRect);

                    int quantity = 1;
                    if (!int.TryParse(_quantityInput, out quantity))
                    {
                        // Invalid input - reset to default
                        quantity = 1;
                        _quantityInput = "1";
                    }

                    // Ensure minimum quantity of 1
                    quantity = Math.Max(quantity, 1);

                    // Slot Input
                    GUI.Label(
                        new Rect(detailsRect.x + 250f, detailsRect.y + 40f, 100f, 25f),
                        "Slot:",
                        _labelStyle
                    );

                    Rect slotRect = new Rect(detailsRect.x + 350f, detailsRect.y + 40f, 100f, 25f);
                    if (!_textFields.TryGetValue("slotInput", out var slotField))
                    {
                        slotField = new CustomTextField(_slotInput, _inputFieldStyle ?? GUI.skin.textField);
                        _textFields["slotInput"] = slotField;
                    }
                    _slotInput = slotField.Draw(slotRect);

                    // Quality Selection
                    if (qualities != null)
                    {
                        float qualityX = detailsRect.x + 10f;
                        for (int i = 0; i < qualities.Count; i++)
                        {
                            Rect qualityRect = new Rect(qualityX, detailsRect.y + 70f, 80f, 25f);

                            var style = i == _selectedQualityIndex ? _itemSelectedStyle : _itemButtonStyle;
                            if (GUI.Button(qualityRect, qualities[i], style ?? _buttonStyle))
                            {
                                _selectedQualityIndex = i;
                            }

                            qualityX += 90f;
                        }
                    }

                    // Spawn Button - Now we have plenty of space for it
                    Rect spawnRect = new Rect(
                        detailsRect.x + detailsRect.width - 130f,
                        detailsRect.y + detailsRect.height - 40f,
                        120f,
                        30f
                    );

                    if (GUI.Button(spawnRect, "Spawn Item", _buttonStyle))
                    {
                        // Get the quantity
                        if (!int.TryParse(_quantityInput, out quantity) || quantity < 1)
                        {
                            quantity = 1;
                        }

                        // Convert selected quality index to enum value
                        var quality = (Il2CppScheduleOne.ItemFramework.EQuality)_selectedQualityIndex;

                        // Use the console command approach
                        SpawnItemViaConsole(_selectedItemId, quantity, quality);
                    }
                }
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error in DrawItemManager: {ex}");
            }
        }
        
        private string GetDisplayNameFromId(string itemId)
        {
            // Find the display name that corresponds to the item ID
            foreach (var item in _itemDictionary)
            {
                if (item.Value == itemId)
                {
                    return item.Key;
                }
            }
            return itemId; // Fallback to ID if no display name found
        }

        private void DrawItemButton(string itemName, Rect buttonRect)
        {
            if (string.IsNullOrEmpty(itemName) || !_itemDictionary.ContainsKey(itemName))
                return;

            // Get the item ID for this display name
            string itemId = _itemDictionary[itemName];

            bool isSelected = _selectedItemId == itemId;
            // Ensure consistent item size with some padding
            var style = isSelected ? _itemSelectedStyle : _itemButtonStyle;
            style = style ?? _buttonStyle;

            // Adjust style to ensure text is centered and doesn't overflow
            style.alignment = TextAnchor.MiddleCenter;
            style.wordWrap = true;

            if (GUI.Button(buttonRect, itemName, style))
            {
                // Set the selected item ID when clicked
                _selectedItemId = itemId;

                // Reset quantity and slot to defaults
                _quantityInput = "1";
                _slotInput = "1";

                // Reset quality to default (Heavenly)
                _selectedQualityIndex = 4; // Assuming Heavenly is the 5th option
            }

            if (buttonRect.Contains(Event.current.mousePosition))
            {
                ShowItemTooltip(itemName, buttonRect);
            }
        }

        private void ShowItemTooltip(string itemName, Rect hoverRect)
        {
            string itemId = _itemDictionary[itemName];
            _currentTooltip = $"Item: {itemName}\nID: {itemId}";
            _tooltipPosition = new Vector2(hoverRect.xMax + 10, hoverRect.y);
            _showTooltip = true;
            _tooltipTimer = 0f;
        }

        private void DrawSettingsPanel()
        {
            try
            {
                // Header with title and close button
                GUILayout.BeginHorizontal(_headerStyle);
                GUILayout.Label("Settings", _titleStyle, GUILayout.ExpandWidth(true));

                // Close button
                if (GUILayout.Button("X", _iconButtonStyle, GUILayout.Width(30), GUILayout.Height(30)))
                {
                    _showSettings = false;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                // Create scrollview for settings
                _settingsScrollPosition = GUILayout.BeginScrollView(_settingsScrollPosition);

                // Settings content - now organized in sections
                // Visual Settings Section
                GUILayout.BeginVertical(_panelStyle);
                GUILayout.Label("Visual Settings", _subHeaderStyle ?? _labelStyle);

                // UI Scale slider
                GUILayout.BeginHorizontal();
                GUILayout.Label("UI Scale:", GUILayout.Width(120));
                float newScale = GUILayout.HorizontalSlider(_uiScale, 0.7f, 1.5f, _sliderStyle ?? GUI.skin.horizontalSlider,
                                                       _sliderThumbStyle ?? GUI.skin.horizontalSliderThumb, GUILayout.Width(200));
                if (newScale != _uiScale)
                {
                    _uiScale = newScale;
                }
                GUILayout.Label($"{_uiScale:F2}x", GUILayout.Width(50));
                GUILayout.EndHorizontal();

                // UI Opacity slider
                GUILayout.BeginHorizontal();
                GUILayout.Label("UI Opacity:", GUILayout.Width(120));
                float newOpacity = GUILayout.HorizontalSlider(_uiOpacity, 0.5f, 1.0f, _sliderStyle ?? GUI.skin.horizontalSlider,
                                                         _sliderThumbStyle ?? GUI.skin.horizontalSliderThumb, GUILayout.Width(200));
                if (newOpacity != _uiOpacity)
                {
                    _uiOpacity = newOpacity;
                }
                GUILayout.Label($"{(int)(_uiOpacity * 100)}%", GUILayout.Width(50));
                GUILayout.EndHorizontal();

                GUILayout.Space(15);




                // Toggle settings
                GUILayout.BeginHorizontal();
                bool newAnimations = GUILayout.Toggle(_enableAnimations, "Enable Animations", GUILayout.Width(200));
                if (newAnimations != _enableAnimations)
                {
                    _enableAnimations = newAnimations;
                    if (!_enableAnimations)
                    {
                        // Reset all animations
                        _commandAnimations.Clear();
                        _buttonHoverAnimations.Clear();
                        _itemGridAnimations.Clear();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                _enableGlow = GUILayout.Toggle(_enableGlow, "Enable Glow Effects", GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                _enableBlur = GUILayout.Toggle(_enableBlur, "Enable Background Blur", GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                _darkTheme = GUILayout.Toggle(_darkTheme, "Dark Theme", GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

                GUILayout.Space(15);

                // Keybind Settings Section
                GUILayout.BeginVertical(_panelStyle);
                GUILayout.Label("Keyboard Shortcuts", _subHeaderStyle ?? _labelStyle);

                // Menu Toggle Key
                GUILayout.BeginHorizontal();
                GUILayout.Label("Menu Toggle Key:", GUILayout.Width(150));
                string menuKeyText = _isCapturingKey && _currentKeyCaptureEntry == _menuToggleKeyEntry ?
                    "Press any key..." : _menuToggleKeyEntry.Value;
                GUILayout.Label(menuKeyText, GUILayout.Width(100));

                if (GUILayout.Button("Change", _buttonStyle, GUILayout.Width(80)))
                {
                    StartCaptureKeybind(_menuToggleKeyEntry);
                }
                GUILayout.EndHorizontal();

                // Explosion Key
                GUILayout.BeginHorizontal();
                GUILayout.Label("Explosion Key:", GUILayout.Width(150));
                string explosionKeyText = _isCapturingKey && _currentKeyCaptureEntry == _explosionKeyEntry ?
                    "Press any key..." : _explosionKeyEntry.Value;
                GUILayout.Label(explosionKeyText, GUILayout.Width(100));

                if (GUILayout.Button("Change", _buttonStyle, GUILayout.Width(80)))
                {
                    StartCaptureKeybind(_explosionKeyEntry);
                }
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

                GUILayout.Space(15);

                // About section
                GUILayout.BeginVertical(_panelStyle);
                GUILayout.Label("About", _subHeaderStyle ?? _labelStyle);
                GUILayout.Label($"{ModInfo.Name} - {ModInfo.Version}");
                GUILayout.Label($"by {ModInfo.Author}");

                // HWID Information
                GUILayout.Space(10);
                GUILayout.Label("HWID Spoofer", _subHeaderStyle ?? _labelStyle);
                GUILayout.Label($"Current HWID: {_generatedHwid}");

                if (GUILayout.Button("Generate New HWID", _buttonStyle, GUILayout.Width(150)))
                {
                    GenerateNewHWID(null);
                }
                GUILayout.EndVertical();

                GUILayout.Space(15);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Generate Debug Info", _buttonStyle, GUILayout.Width(150)))
                {
                    GenerateDebugInfoCommand(null);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(15);
                
                // Action buttons
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Save Settings", _buttonStyle, GUILayout.Width(150)))
                {
                    SaveSettings();
                }

                if (GUILayout.Button("Reset Settings", _buttonStyle, GUILayout.Width(150)))
                {
                    // Reset to defaults
                    _uiScale = 1.0f;
                    _uiOpacity = 0.95f;
                    _enableAnimations = true;
                    _enableGlow = true;
                    _enableBlur = true;
                    _darkTheme = true;

                    ShowNotification("Settings Reset", "All settings have been reset to defaults", NotificationType.Info);
                }
                GUILayout.EndHorizontal();

                GUILayout.EndScrollView();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error drawing settings panel: {ex.Message}");
            }
        }

        private void ShowDropdownMenu(CommandParameter param)
        {
            // Explicit logging

            if (param == null)
            {
                Debug.LogError("CommandParameter is NULL!");
                return;
            }

            if (!_itemCache.ContainsKey(param.ItemCacheKey))
            {
                Debug.LogError($"NO ITEM CACHE FOR KEY: {param.ItemCacheKey}");
                return;
            }

            var items = _itemCache[param.ItemCacheKey];

            // Default to first item if none selected
            if (string.IsNullOrEmpty(param.Value) && items.Count > 0)
                param.Value = items[0];

            // Get current index
            int currentIndex = items.IndexOf(param.Value);

            // Cycle to the next value, wrapping around
            int nextIndex = (currentIndex + 1) % items.Count;
            param.Value = items[nextIndex];

        }

        private void ExecuteCommand(Command command)
        {
            try
            {
                if (command.Handler != null)
                {
                    string[] args = command.Parameters
                        .Select(p => p.Value?.Trim() ?? "")
                        .Where(a => !string.IsNullOrEmpty(a))
                        .ToArray();

                    command.Handler.Invoke(args);
                    ShowNotification("Command Executed", $"{command.Name} completed", NotificationType.Success);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Command failed: {ex}");
                ShowNotification("Command Error", ex.Message, NotificationType.Error);
            }
        }

        #region Notification System

        public enum NotificationType
        {
            Info,
            Success,
            Warning,
            Error
        }

        public struct Notification
        {
            public string Title;
            public string Message;
            public NotificationType Type;
            public float Time;
            public float Alpha;
            public float PositionY;
        }

        private void ShowNotification(string title, string message, NotificationType type)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                Time = Time.realtimeSinceStartup,
                Alpha = 0f
            };

            _notifications.Enqueue(notification);

            // Limit queue size
            if (_notifications.Count > 5)
            {
                _notifications.Dequeue();
            }
        }

        private void UpdateNotifications()
        {
            // Process queue
            if (_notifications.Count > 0 && _activeNotifications.Count < 3)
            {
                _activeNotifications.Add(_notifications.Dequeue());
            }

            // Update active notifications
            for (int i = _activeNotifications.Count - 1; i >= 0; i--)
            {
                var notification = _activeNotifications[i];
                float elapsed = Time.realtimeSinceStartup - notification.Time;

                // Fade in
                if (elapsed < 0.3f)
                {
                    notification.Alpha = Mathf.Lerp(0, 1, elapsed / 0.3f);
                }
                // Stay visible
                else if (elapsed < _notificationDisplayTime)
                {
                    notification.Alpha = 1f;
                }
                // Fade out
                else if (elapsed < _notificationDisplayTime + _notificationFadeTime)
                {
                    notification.Alpha = Mathf.Lerp(1, 0, (elapsed - _notificationDisplayTime) / _notificationFadeTime);
                }
                // Remove
                else
                {
                    _activeNotifications.RemoveAt(i);
                    continue;
                }

                // Update position
                float targetY = Screen.height - 100 - (i * 70);
                if (notification.PositionY == 0)
                {
                    // Initial position
                    notification.PositionY = targetY;
                }
                else
                {
                    // Smooth movement
                    notification.PositionY = Mathf.Lerp(notification.PositionY, targetY, Time.deltaTime * 5f);
                }

                _activeNotifications[i] = notification;
            }
        }

        private void DrawNotifications()
        {
            if (_activeNotifications.Count == 0)
                return;

            GUIStyle notifStyle = _statusStyle ?? GUI.skin.box;
            if (notifStyle == null)
                return;

            foreach (var notification in _activeNotifications)
            {
                Rect notifRect = new Rect(
                    Screen.width - 320f,
                    notification.PositionY,
                    300f,
                    60f
                );

                // Background
                GUI.color = new Color(1, 1, 1, notification.Alpha);
                GUI.Box(notifRect, "");

                // Content
                GUILayout.BeginArea(notifRect);

                // Header
                GUILayout.BeginHorizontal();
                GUILayout.Label(notification.Title, GUILayout.Width(280));
                GUILayout.EndHorizontal();

                // Message
                GUILayout.Label(notification.Message);

                GUILayout.EndArea();

                GUI.color = Color.white;
            }
        }

        #endregion

        #region HWID Spoofer
        private static string _generatedHwid;

        private void ViewCurrentHWID(string[] args)
        {
            try
            {
                ShowNotification("HWID", $"Current HWID: {_generatedHwid}", NotificationType.Info);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error viewing HWID: {ex.Message}");
                ShowNotification("Error", "Failed to view HWID", NotificationType.Error);
            }
        }

        private void GenerateNewHWID(string[] args)
        {
            try
            {
                // Use the existing HWID generation logic from InitializeHwidPatch
                var random = new System.Random(Environment.TickCount);
                var bytes = new byte[SystemInfo.deviceUniqueIdentifier.Length / 2];
                random.NextBytes(bytes);
                var newId = string.Join("", bytes.Select(it => it.ToString("x2")));

                // Update the preferences entry
                var hwidEntry = MelonPreferences.CreateEntry("CheatMenu", "HWID", "", is_hidden: true);
                hwidEntry.Value = newId;

                // Update the static _generatedHwid
                _generatedHwid = newId;

                ShowNotification("HWID", $"Generated new HWID", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error generating new HWID: {ex.Message}");
                ShowNotification("Error", "Failed to generate new HWID", NotificationType.Error);
            }
        }

        private void InitializeHwidPatch()
        {
            try
            {
                LoggerInstance.Msg("Initializing HWID Patch...");

                // Always generate a new HWID on each game load
                var random = new System.Random(Environment.TickCount);
                var bytes = new byte[SystemInfo.deviceUniqueIdentifier.Length / 2];
                random.NextBytes(bytes);
                var newId = string.Join("", bytes.Select(it => it.ToString("x2")));

                // Save the new HWID to MelonPreferences
                var hwidEntry = MelonPreferences.CreateEntry("CheatMenu", "HWID", newId, is_hidden: true);

                // Store the generated HWID
                _generatedHwid = newId;

                // Create a harmony patch for the deviceUniqueIdentifier property
                var originalMethod = typeof(SystemInfo).GetProperty("deviceUniqueIdentifier").GetMethod;
                var patchMethod = typeof(Core).GetMethod("GetDeviceIdPatch",
                    BindingFlags.Static | BindingFlags.NonPublic);

                _harmony.Patch(originalMethod, new HarmonyLib.HarmonyMethod(patchMethod));

                LoggerInstance.Msg("HWID Patch integrated successfully");
                LoggerInstance.Msg($"New HWID: {newId}");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to initialize HWID patch: {ex.Message}");
            }
        }

        // Harmony patch for SystemInfo.deviceUniqueIdentifier
        private static bool GetDeviceIdPatch(ref string __result)
        {
            __result = _generatedHwid;
            return false; // Skip the original method
        }
        #endregion

        #region Command Implementations

        // Method to get a player's health component
        private PlayerHealth GetPlayerHealth(Il2CppScheduleOne.PlayerScripts.Player player)
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
                LoggerInstance.Error($"Error getting player health: {ex.Message}");
            }
            return null;
        }

        private Il2CppScheduleOne.PlayerScripts.Player FindLocalPlayer()
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

                    LoggerInstance.Error("Could not identify local player!");
                }
                else
                {
                    LoggerInstance.Error("PlayerList is null!");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error finding local player: {ex.Message}");
            }
            return null;
        }

        // Method to determine if a player is the local player
        private bool IsLocalPlayer(Il2CppScheduleOne.PlayerScripts.Player player)
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

        private void ServerExecuteDamagePlayer(Il2CppScheduleOne.PlayerScripts.Player targetPlayer, float damageAmount)
        {
            try
            {
                var playerHealth = GetPlayerHealth(targetPlayer);
                if (playerHealth == null)
                {
                    LoggerInstance.Error("PlayerHealth not found on player!");
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
                    LoggerInstance.Error($"Failed using RpcWriter: {e.Message}");
                    try
                    {
                        playerHealth.TakeDamage(damageAmount, true, true);
                    }
                    catch (Exception e2)
                    {
                        LoggerInstance.Error($"All damage methods failed: {e2.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in ServerExecuteDamagePlayer: {ex.Message}");
            }
        }

        private void ServerExecuteKillPlayer(Il2CppScheduleOne.PlayerScripts.Player targetPlayer)
        {
            try
            {
                var playerHealth = GetPlayerHealth(targetPlayer);
                if (playerHealth == null) return;

                // Prefer Server SendDie for remote players
                playerHealth.RpcWriter___Server_SendDie_2166136261();
                playerHealth.RpcWriter___Observers_TakeDamage_3505310624(99999999999999f, true, true);

                LoggerInstance.Msg($"Killed player: {targetPlayer.name}");
                ShowNotification("Player", $"Killed {targetPlayer.name}", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error killing player: {ex.Message}");
                ShowNotification("Error", "Failed to kill player", NotificationType.Error);
            }
        }

        // Command Handlers
        private void DamagePlayerCommand(string[] args)
        {
            if (args.Length < 2 ||
                !int.TryParse(args[0], out int playerIndex) || playerIndex < 1 ||
                !float.TryParse(args[1], out float damage))
            {
                LoggerInstance.Error("Invalid parameters! Please enter valid player index and damage amount.");
                ShowNotification("Error", "Invalid parameters", NotificationType.Error);
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
                        LoggerInstance.Error("Player is null!");
                        ShowNotification("Error", "Player is null", NotificationType.Error);
                        return;
                    }

                    // Check if it's the local player
                    if (IsLocalPlayer(player))
                    {
                        // For local player, we can use the direct method
                        var playerHealth = GetPlayerHealth(player);
                        if (playerHealth != null)
                        {
                            playerHealth.TakeDamage(damage, true, true);
                            ShowNotification("Player", "Damaged local player", NotificationType.Success);
                        }
                        else
                        {
                            LoggerInstance.Error("Local player health component not found!");
                            ShowNotification("Error", "Health component not found", NotificationType.Error);
                        }
                    }
                    else
                    {
                        // For other players, use the server method
                        ServerExecuteDamagePlayer(player, damage);
                        ShowNotification("Player", $"Sent damage request for {player.name}", NotificationType.Success);
                    }
                }
                else
                {
                    LoggerInstance.Error("Player index out of range!");
                    ShowNotification("Error", "Player index out of range", NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error damaging player: {ex.Message}");
                ShowNotification("Error", "Failed to damage player", NotificationType.Error);
            }
        }

        private void KillPlayerCommand(string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int playerIndex) || playerIndex < 1)
            {
                LoggerInstance.Error("Invalid player index! Please enter a valid number.");
                ShowNotification("Error", "Invalid player index", NotificationType.Error);
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
                        LoggerInstance.Error("Player is null!");
                        ShowNotification("Error", "Player is null", NotificationType.Error);
                        return;
                    }

                    //ServerExecuteKillPlayer(player);
                    ServerExecuteDamagePlayer(player, 99999999999999f);
                    ShowNotification("Player", $"Sent kill request for {player.name}", NotificationType.Success);

                    var playerHealth = GetPlayerHealth(player);
                    if (playerHealth != null)
                    {
                        playerHealth.Die();
                        LoggerInstance.Msg("Killed local player");
                        ShowNotification("Player", "Killed local player", NotificationType.Success);
                    }
                }
                else
                {
                    LoggerInstance.Error("Player index out of range!");
                    ShowNotification("Error", "Player index out of range", NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error killing player: {ex.Message}");
                ShowNotification("Error", "Failed to kill player", NotificationType.Error);
            }
        }

        private void KillAllPlayersCommand(string[] args)
        {
            try
            {
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
                if (playerList == null || playerList.Count == 0)
                {
                    LoggerInstance.Error("No players found!");
                    ShowNotification("Players", "No players found", NotificationType.Error);
                    return;
                }

                int killedCount = 0;

                for (int i = 0; i < playerList.Count; i++)
                {
                    var player = playerList[i];
                    if (player == null) continue;

                    // Skip the local player
                    if (IsLocalPlayer(player))
                    {
                        LoggerInstance.Msg($"Skipping local player: {player.name}");
                        continue;
                    }

                    // Kill the remote player
                    ServerExecuteDamagePlayer(player, 99999999999999f);
                    killedCount++;
                }
                ShowNotification("Players", $"Kill requests sent for {killedCount} players", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error killing all players: {ex.Message}");
                ShowNotification("Error", "Failed to kill all players", NotificationType.Error);
            }
        }

        private IEnumerator SpawnItemViaConsoleCoroutine(string itemId, int quantity, Il2CppScheduleOne.ItemFramework.EQuality quality)
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
                LoggerInstance.Error($"Add item failed: {caughtException}");
                yield break;
            }

            // Final cleanup
            try
            {
                CursorManager.Instance.SetCursorAppearance(CursorManager.ECursorType.Default);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Failed to spawn the item: {ex.Message}");
            }
        }

        // Updated spawn caller
        private void SpawnItemViaConsole(string itemId, int quantity, Il2CppScheduleOne.ItemFramework.EQuality quality)
        {
            MelonCoroutines.Start(SpawnItemViaConsoleCoroutine(itemId, quantity, quality));
        }

        // For UI buttons (called from buttons in the UI)
        private void PackageProductCommand(string packageType)
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedItemId))
                {
                    ShowNotification("Package Product", "No item selected", NotificationType.Warning);
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

                ShowNotification("Package Product", $"Item packaged in {packageType}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error packaging product: {ex.Message}");
                ShowNotification("Error", "Failed to package product", NotificationType.Error);
            }
        }

        private void SetItemQuality(Il2CppScheduleOne.ItemFramework.EQuality quality)
        {
            try
            {
                // Create parameter list with just the quality value
                var qualityList = new Il2CppSystem.Collections.Generic.List<string>();
                qualityList.Add(((int)quality).ToString());

                // Execute with the parameter list
                var cmd = new Il2CppScheduleOne.Console.SetQuality();
                cmd.Execute(qualityList);

                ShowNotification("Item Quality", $"Set to {quality} quality", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error setting item quality: {ex.Message}");
                ShowNotification("Error", "Failed to set item quality", NotificationType.Error);
            }
        }

        private void ToggleFreeCam(string[] args)
        {
            try
            {
                _freeCamEnabled = !_freeCamEnabled;

                // Close the menu.
                ToggleUI(false);

                // Toggle player camera
                if (Il2CppScheduleOne.PlayerScripts.PlayerCamera.Instance != null)
                {
                    Il2CppScheduleOne.PlayerScripts.PlayerCamera.Instance.SetCanLook(true);
                }

                // Toggle player movement
                if (Il2CppScheduleOne.PlayerScripts.PlayerMovement.Instance != null)
                {
                    Il2CppScheduleOne.PlayerScripts.PlayerMovement.Instance.canMove = false;
                }

                // Toggle input system
                if (Il2CppScheduleOne.GameInput.Instance != null &&
                    Il2CppScheduleOne.GameInput.Instance.PlayerInput != null)
                {
                    Il2CppScheduleOne.GameInput.Instance.PlayerInput.m_InputActive = true;
                }

                Il2CppScheduleOne.PlayerScripts.PlayerCamera.Instance.SetFreeCam(_freeCamEnabled);

                ShowNotification("Free Camera", _freeCamEnabled ? "Enabled" : "Disabled", _freeCamEnabled ? NotificationType.Success : NotificationType.Info);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error toggling free camera: {ex.Message}");
                ShowNotification("Error", "Failed to toggle free camera", NotificationType.Error);
            }
        }
        private void DrawFreecamOverlay()
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
                LoggerInstance.Error($"Error drawing freecam overlay: {ex.Message}");
            }
        }

        private void ToggleNeverWanted(string[] args)
        {
            try
            {
                _playerNeverWantedEnabled = !_playerNeverWantedEnabled;

                if (_playerNeverWantedEnabled)
                {
                    if (_neverWantedCoroutine == null)
                    {
                        _neverWantedCoroutine = MelonCoroutines.Start(NeverWantedRoutine());
                    }
                    ShowNotification("Never Wanted", "Enabled", NotificationType.Success);
                }
                else
                {
                    if (_neverWantedCoroutine != null)
                    {
                        MelonCoroutines.Stop(_neverWantedCoroutine);
                        _neverWantedCoroutine = null;
                    }

                    ShowNotification("Never Wanted", "Disabled", NotificationType.Info);
                }
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error toggling never wanted: {ex.Message}");
                ShowNotification("Error", "Failed to toggle never wanted", NotificationType.Error);
            }
        }

        private IEnumerator NeverWantedRoutine()
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
                    LoggerInstance.Error($"Error in godmode routine: {ex.Message}");
                }

                // Wait before next health update
                yield return new WaitForSeconds(0.2f);
            }
        }


        // Helper method to find the local player
        private void PatchImpactNetworkMethods()
        {
            try
            {
                LoggerInstance.Msg("Setting up godmode network method patches...");
        
                // Save the local player name for comparison
                var localPlayer = FindLocalPlayer();
                if (localPlayer != null)
                {
                    _localPlayerName = localPlayer.name;
                    LoggerInstance.Msg($"Local player identified as: {_localPlayerName}");
                }
                else
                {
                    LoggerInstance.Error("Failed to identify local player for godmode!");
                    return;
                }

                // Use System.Type instead of Il2CppSystem.Type
                var playerHealthType = typeof(PlayerHealth);

                var blockMethod = typeof(Core).GetMethod("BlockNetworkDamageMethod",
                    BindingFlags.Static | BindingFlags.NonPublic);

                if (blockMethod == null)
                {
                    LoggerInstance.Error("BlockNetworkDamageMethod not found!");
                    return;
                }

                var prefix = new HarmonyMethod(blockMethod);

                // Patch TakeDamage methods
                PatchMethod(playerHealthType, "RpcWriter___Observers_TakeDamage_3505310624", prefix);
                PatchMethod(playerHealthType, "RpcLogic___TakeDamage_3505310624", prefix);
                PatchMethod(playerHealthType, "RpcReader___Observers_TakeDamage_3505310624", prefix);

                // Patch Die methods
                PatchMethod(playerHealthType, "RpcWriter___Observers_Die_2166136261", prefix);
                PatchMethod(playerHealthType, "RpcLogic___Die_2166136261", prefix);
                PatchMethod(playerHealthType, "RpcReader___Observers_Die_2166136261", prefix);

                LoggerInstance.Msg("Successfully patched PlayerHealth network methods");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error patching network damage methods: {ex}");
            }
        }

        // Harmony prefix method must be static
        private static bool BlockNetworkDamageMethod()
        {
            return !_staticPlayerGodmodeEnabled;
        }

        private void PatchMethod(Type targetType, string methodName, HarmonyMethod prefix)
        {
            try
            {
                // Get method with explicit flags for Il2Cpp methods
                var method = targetType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (method == null)
                {
                    LoggerInstance.Error($"Method {methodName} not found!");
                    return;
                }
                _harmony.Patch(method, prefix: prefix);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error patching {methodName}: {ex.Message}");
            }
        }

        // Modify the ToggleGodMode method
        private void ToggleGodmode(string[] args)
        {
            try
            {
                // Toggle godmode state
                _playerGodmodeEnabled = !_playerGodmodeEnabled;
                _staticPlayerGodmodeEnabled = _playerGodmodeEnabled;

                if (_playerGodmodeEnabled)
                {
                    // Update local player name for checking
                    var localPlayer = FindLocalPlayer();
                    if (localPlayer != null)
                    {
                        _localPlayerName = localPlayer.name;
                    }
                    else
                    {
                        LoggerInstance.Error("Failed to identify local player for godmode!");
                    }

                    // Patch network methods to block damage for local player only
                    PatchImpactNetworkMethods();

                    // Start the godmode coroutine
                    if (_godModeCoroutine == null)
                    {
                        _godModeCoroutine = MelonCoroutines.Start(GodModeRoutine());
                    }
            
                    ShowNotification("Godmode", "Enabled network patches.", NotificationType.Success);
                }
                else
                {
                    // Stop the godmode coroutine if it's running
                    if (_godModeCoroutine != null)
                    {
                        MelonCoroutines.Stop(_godModeCoroutine);
                        _godModeCoroutine = null;
                    }

                    // Clear local player name
                    _localPlayerName = "";

                    // Attempt to unpatch methods
                    try
                    {
                        HarmonyLib.Harmony.UnpatchAll();
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Error($"Error unpatching methods: {ex.Message}");
                    }

                    ShowNotification("Godmode", "Disabled network patches.", NotificationType.Info);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error toggling godmode: {ex.Message}");
                ShowNotification("Error", "Failed to toggle godmode", NotificationType.Error);
            }
        }

        private IEnumerator GodModeRoutine()
        {
            while (_staticPlayerGodmodeEnabled)
            {
                try
                {
                    // Find the local player
                    var localPlayer = FindLocalPlayer();
                    if (localPlayer != null)
                    {
                        // Get the health component
                        var playerHealth = GetPlayerHealth(localPlayer);
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
                            {
                                playerHealth.onDie.RemoveAllListeners();
                            }
                        }
                        else
                        {
                            LoggerInstance.Error("Local player health component not found!");
                        }
                    }
                    else
                    {
                        LoggerInstance.Error("Local player not found in godmode routine!");
                    }
                }
                catch (System.Exception ex)
                {
                    LoggerInstance.Error($"Error in godmode routine: {ex.Message}");
                }

                // Wait before next health update
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void RaiseWantedLevel(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.RaisedWanted();
                command.Execute(null);
                ShowNotification("Wanted Level", "Increased", NotificationType.Info);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error raising wanted level: {ex.Message}");
                ShowNotification("Error", "Failed to raise wanted level", NotificationType.Error);
            }
        }

        private void LowerWantedLevel(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.LowerWanted();
                command.Execute(null);
                ShowNotification("Wanted Level", "Decreased", NotificationType.Info);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error lowering wanted level: {ex.Message}");
                ShowNotification("Error", "Failed to lower wanted level", NotificationType.Error);
            }
        }

        private void ClearWantedLevel(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.ClearWanted();
                command.Execute(null);
                ShowNotification("Wanted Level", "Cleared", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error clearing wanted level: {ex.Message}");
                ShowNotification("Error", "Failed to clear wanted level", NotificationType.Error);
            }
        }

        private void ClearWantedLevelEx(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.ClearWanted();
                command.Execute(null);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error clearing wanted level: {ex.Message}");
                ShowNotification("Error", "Failed to clear wanted level", NotificationType.Error);
            }
        }

        private void ClearTrash(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.ClearTrash();
                command.Execute(null);
                ShowNotification("World", "Cleared all trash", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error clearing world trash: {ex.Message}");
                ShowNotification("Error", "Failed to clear trash", NotificationType.Error);
            }
        }

        private void ClearInventory(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.ClearInventoryCommand();
                command.Execute(null);
                LoggerInstance.Msg("Inventory cleared.");
                ShowNotification("Inventory", "Cleared all items", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error: {ex.Message}");
                ShowNotification("Error", "Failed to clear inventory", NotificationType.Error);
            }
        }

        private void EndTutorial(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.EndTutorial();
                command.Execute(null);
                ShowNotification("Tutorial", "Tutorial ended", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error ending tutorial: {ex.Message}");
                ShowNotification("Error", "Failed to end tutorial", NotificationType.Error);
            }
        }

        private void GrowPlants(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.GrowPlants();
                command.Execute(null);

                ShowNotification("World", "All weed plants have been instantly grown!", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error calling GrowPlants: {ex.Message}");
            }
        }

        private void ForceGameSave(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.Save();
                command.Execute(null);
                ShowNotification("Game", "Save completed", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error forcing game save: {ex.Message}");
                ShowNotification("Error", "Failed to save game", NotificationType.Error);
            }
        }

        private void ChangeXP(string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int amount))
            {
                ShowNotification("Error", "Invalid XP amount", NotificationType.Error);
                return;
            }

            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(amount.ToString());

                var cmd = new Il2CppScheduleOne.Console.GiveXP();
                cmd.Execute(commandList);

                ShowNotification("XP", $"Changed by ${amount}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error changing XP: {ex.Message}");
                ShowNotification("Error", "Failed to change XP amount", NotificationType.Error);
            }
        }

        private void ChangeCash(string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int amount))
            {
                ShowNotification("Error", "Invalid cash amount", NotificationType.Error);
                return;
            }

            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(amount.ToString());

                var cmd = new Il2CppScheduleOne.Console.ChangeCashCommand();
                cmd.Execute(commandList);

                ShowNotification("Cash", $"Changed by ${amount}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error changing cash: {ex.Message}");
                ShowNotification("Error", "Failed to change cash amount", NotificationType.Error);
            }
        }

        private void ChangeBalance(string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int amount))
            {
                ShowNotification("Error", "Invalid balance amount", NotificationType.Error);
                return;
            }

            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(amount.ToString());

                var cmd = new Il2CppScheduleOne.Console.ChangeOnlineBalanceCommand();
                cmd.Execute(commandList);

                ShowNotification("Online Balance", $"Changed by ${amount}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error changing balance: {ex.Message}");
                ShowNotification("Error", "Failed to change online balance", NotificationType.Error);
            }
        }

        private void SetWorldTime(string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int time))
            {
                LoggerInstance.Error("Invalid scale! Please enter a number.");
                ShowNotification("Error", "Invalid time scale value", NotificationType.Error);
                return;
            }

            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(time.ToString());

                var cmd = new Il2CppScheduleOne.Console.SetTimeCommand();
                cmd.Execute(commandList);

                ShowNotification("World Time", $"Set world time to {time}!", NotificationType.Success);
            }

            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Unable to set time: {ex.Message}");
            }
        }

        private void SetTimeScale(string[] args)
        {
            if (args.Length < 1 || !float.TryParse(args[0], out float scale))
            {
                LoggerInstance.Error("Invalid scale! Please enter a number.");
                ShowNotification("Error", "Invalid time scale value", NotificationType.Error);
                return;
            }

            try
            {
                // Clamp to reasonable range
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(scale.ToString());

                var cmd = new Il2CppScheduleOne.Console.SetTimeScale();
                cmd.Execute(commandList);

                ShowNotification("Time Scale", $"Set to {scale}.", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Unable to set time scale: {ex.Message}");
            }
        }

        private void SetPlayerMovementSpeed(string[] args)
        {
            if (args.Length < 1)
            {
                LoggerInstance.Error("Movement speed amount required.");
                ShowNotification("Error", "Movement speed reserve amount required.", NotificationType.Error);
            }

            if (!int.TryParse(args[0], out int speed))
            {
                LoggerInstance.Error("Invalid amount, please enter a valid number.");
                ShowNotification("Error", "Invalid movement speed amount specificed.", NotificationType.Error);
            }

            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(speed.ToString());

                var cmd = new Il2CppScheduleOne.Console.SetMoveSpeedCommand();
                cmd.Execute(commandList);

                ShowNotification("Movement Speed", $"Set speed to {speed}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Unable to set player movement speed: {ex.Message}");
            }
        }

        private void SetPlayerStaminaReserve(string[] args)
        {
            if (args.Length < 1)
            {
                LoggerInstance.Error("Stamina amount required.");
                ShowNotification("Error", "Stamina reserve amount required.", NotificationType.Error);
            }

            if (!int.TryParse(args[0], out int reserve))
            {
                LoggerInstance.Error("Invalid amount, please enter a valid number.");
                ShowNotification("Error", "Invalid stamina reserve amount specificed.", NotificationType.Error);
            }
            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(reserve.ToString());

                var cmd = new Il2CppScheduleOne.Console.SetStaminaReserve();
                cmd.Execute(commandList);

                ShowNotification("Stamina Reserve", $"Successfuly set stamina reserve to {reserve}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Unable to set player stamina reserve: {ex.Message}");
            }
        }
        private void SetJumpForce(string[] args)
        {
            if (args.Length < 1)
            {
                LoggerInstance.Error("Force amount required!");
                ShowNotification("Error", "Force amount required", NotificationType.Error);
                return;
            }

            // Try to parse the first argument into an integer
            if (!int.TryParse(args[0], out int force))
            {
                LoggerInstance.Error("Invalid force amount! Please enter a valid number.");
                ShowNotification("Error", "Invalid force value", NotificationType.Error);
                return;
            }

            try
            {
                // Create a list of arguments (using the IL2CPP version of List with string as the type parameter)
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(force.ToString());

                // Create an instance of the SetHealth command
                var cmd = new Il2CppScheduleOne.Console.SetJumpMultiplier();

                // Execute the command with the argument list
                cmd.Execute(commandList);

                ShowNotification("Jump Force", $"Set to {force}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error setting jump force: {ex.Message}");
                ShowNotification("Error", "Failed to set jump force.", NotificationType.Error);
            }
        }

        private void SetLawIntensity(string[] args)
        {
            if (args.Length < 1)
            {
                LoggerInstance.Error("intensity amount required!");
                ShowNotification("Error", "intensity amount required", NotificationType.Error);
                return;
            }

            // Try to parse the first argument into an integer
            if (!int.TryParse(args[0], out int intensity))
            {
                LoggerInstance.Error("Invalid intensity amount! Please enter a valid number.");
                ShowNotification("Error", "Invalid intensity value", NotificationType.Error);
                return;
            }

            try
            {
                // Create a list of arguments (using the IL2CPP version of List with string as the type parameter)
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(intensity.ToString());

                // Create an instance of the SetHealth command
                var cmd = new Il2CppScheduleOne.Console.SetLawIntensity();

                // Execute the command with the argument list
                cmd.Execute(commandList);

                ShowNotification("Law Intensity", $"Set to {intensity}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error setting law intensity: {ex.Message}");
                ShowNotification("Error", "Failed to set law intensity.", NotificationType.Error);
            }
        }

        private unsafe void CacheGameItems()
        {
            try
            {
                // Create dictionaries to store discovered items
                var discoveredItems = new Dictionary<string, string>();
                var qualitySupportCache = new Dictionary<string, bool>();
                var qualityItemCache = new Dictionary<string, List<string>>();

                // Get the registry
                var registry = Registry.Instance;
                if (registry == null)
                {
                    LoggerInstance.Error("Registry instance is NULL!");
                    return;
                }

                // Get quality names for reference
                var qualityNames = Enum.GetNames(typeof(EQuality)).ToList();

                // Get ProductManager instance to access drug product definitions
                var productManager = ProductManager.Instance;
                if (productManager == null)
                {
                    LoggerInstance.Error("ProductManager instance is NULL!");
                }

                // Create a managed list to store all products
                var allProducts = new List<ProductDefinition>();
                if (productManager != null)
                {
                    // Add all products from different sources to our local list
                    // Handle Il2Cpp collections properly
                    if (productManager.AllProducts != null)
                    {
                        for (int i = 0; i < productManager.AllProducts.Count; i++)
                        {
                            allProducts.Add(productManager.AllProducts[i]);
                        }
                    }

                    if (ProductManager.DiscoveredProducts != null)
                    {
                        for (int i = 0; i < ProductManager.DiscoveredProducts.Count; i++)
                        {
                            allProducts.Add(ProductManager.DiscoveredProducts[i]);
                        }
                    }

                    if (productManager.DefaultKnownProducts != null)
                    {
                        for (int i = 0; i < productManager.DefaultKnownProducts.Count; i++)
                        {
                            allProducts.Add(productManager.DefaultKnownProducts[i]);
                        }
                    }

                    // Remove duplicates - using a dictionary to track unique items by ID
                    var uniqueProducts = new Dictionary<string, ProductDefinition>();
                    foreach (var product in allProducts)
                    {
                        if (!uniqueProducts.ContainsKey(product.ID))
                        {
                            uniqueProducts[product.ID] = product;
                        }
                    }

                    allProducts = uniqueProducts.Values.ToList();
                    LoggerInstance.Msg($"Found {allProducts.Count} product definitions from ProductManager");
                }

                // Enumerate all items in the registry
                foreach (var entry in registry.ItemDictionary)
                {
                    try
                    {
                        var itemDefinition = entry.Value.Definition;

                        // Skip null definitions
                        if (itemDefinition == null) continue;

                        // Try to get item ID and name
                        string itemId = itemDefinition.ID;
                        string itemName = itemDefinition.Name;

                        // Determine if thQis is a quality item
                        bool isQualityItem = false;
                        List<string> supportedQualities = null;

                        // Check 1: Explicit QualityItemDefinition type
                        var qualityDef = itemDefinition as QualityItemDefinition;
                        if (qualityDef != null)
                        {
                            isQualityItem = true;
                            supportedQualities = qualityNames;
                        }

                        // Check 2: Check if this is a drug product by comparing with ProductManager items
                        if (!isQualityItem && productManager != null)
                        {
                            // Try to find matching product in the product list
                            ProductDefinition matchingProduct = null;
                            foreach (var product in allProducts)
                            {
                                if (product.ID.Equals(itemId, StringComparison.OrdinalIgnoreCase) ||
                                    product.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase))
                                {
                                    matchingProduct = product;
                                    break;
                                }
                            }

                            if (matchingProduct != null)
                            {
                                isQualityItem = true;

                                // Determine drug type and available qualities based on product type
                                string drugType = matchingProduct.DrugType.ToString();

                                // Log product properties if available
                                if (matchingProduct.Properties != null && matchingProduct.Properties.Count > 0)
                                {
                                    // Handle Il2Cpp List without using LINQ
                                    var propertyNames = new List<string>();
                                    for (int i = 0; i < matchingProduct.Properties.Count; i++)
                                    {
                                        var property = matchingProduct.Properties[i];
                                        propertyNames.Add(property.Name);
                                    }

                                }

                                // For now, we'll use all quality levels, but this could be refined based on the product type
                                supportedQualities = qualityNames;
                            }
                        }

                        if (!string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(itemName))
                        {
                            // Add to main dictionary (display name => ID)
                            discoveredItems[itemName] = itemId;

                            // Track quality support
                            qualitySupportCache[itemId] = isQualityItem;

                            // If it's a quality item, cache quality levels
                            if (isQualityItem)
                            {
                                qualityItemCache[itemId] = supportedQualities ?? qualityNames;
                            }
                        }
                    }
                    catch (Exception innerEx)
                    {
                        LoggerInstance.Error($"Error processing item entry: {innerEx.Message}");
                    }
                }

                // Update class-level caches
                _itemDictionary = discoveredItems;
                _qualitySupportCache = qualitySupportCache;
                _itemQualityCache = qualityItemCache;

                // Prepare standard caches
                _itemCache["qualities"] = qualityNames;
                _itemCache["items"] = discoveredItems.Keys.OrderBy(k => k).ToList();
                _itemCache["slots"] = Enumerable.Range(1, 9).Select(x => x.ToString()).ToList();

                // Count quality items using standard .NET method
                int qualityItemCount = 0;
                foreach (var kvp in qualitySupportCache)
                {
                    if (kvp.Value) qualityItemCount++;
                }

                LoggerInstance.Msg($"Item Discovery Complete:");
                LoggerInstance.Msg($"- Total Items: {discoveredItems.Count}");
                LoggerInstance.Msg($"- Quality Items: {qualityItemCount}");

            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Critical error in item discovery: {ex}");
                ShowNotification("Error", "Failed to discover game items", NotificationType.Error);
            }
        }

        #endregion

        #region Weapons Modifications
        private Equippable_RangedWeapon GetEquippedWeapon()
        {
            try
            {
                // Use cached weapon if recent
                if (_cachedWeapon != null &&
                    Time.time - _lastWeaponCheckTime < WEAPON_CACHE_INTERVAL)
                {
                    return _cachedWeapon;
                }

                // Reset cache time
                _lastWeaponCheckTime = Time.time;

                // Find player object
                var playerObject = FindPlayerNetworkObject();
                if (playerObject == null)
                {
                    LoggerInstance.Error("Cannot find player object for weapon detection.");
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
                    _cachedWeapon = foundWeapon;
                    return foundWeapon;
                }
                return null;
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error in weapon detection: {ex.Message}");
                return null;
            }
        }

        // Toggle unlimited ammo
        public void ToggleUnlimitedAmmo(string[] args)
        {
            try
            {
                _unlimitedAmmoEnabled = !_unlimitedAmmoEnabled;

                if (_unlimitedAmmoEnabled)
                {
                    if (_unlimitedAmmoCoroutine == null)
                    {
                        _unlimitedAmmoCoroutine = MelonCoroutines.Start(UnlimitedAmmoRoutine());
                    }

                    ShowNotification("Unlimited Ammo", "Enabled", NotificationType.Success);
                }
                else
                {
                    if (_unlimitedAmmoCoroutine != null)
                    {
                        MelonCoroutines.Stop(_unlimitedAmmoCoroutine);
                        _unlimitedAmmoCoroutine = null;
                    }

                    ShowNotification("Unlimited Ammo", "Disabled", NotificationType.Info);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error toggling unlimited ammo: {ex.Message}");
                ShowNotification("Error", "Failed to toggle unlimited ammo", NotificationType.Error);
            }
        }

        // Unlimited ammo coroutine
        private IEnumerator UnlimitedAmmoRoutine()
        {
            while (_unlimitedAmmoEnabled)
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
                    LoggerInstance.Error($"Error in unlimited ammo routine: {ex.Message}");
                }

                // Wait slightly longer to reduce performance impact
                yield return new WaitForSeconds(0.5f);
            }
        }

        #endregion


        #region Explosion exploiting

        // Explosion loop functionality
        private Dictionary<string, object> _explodeLoopCoroutines = new Dictionary<string, object>();

        private void StartExplodeLoop(OnlinePlayerInfo playerInfo)
        {
            string playerKey = playerInfo.Player.GetInstanceID().ToString();
            if (_explodeLoopCoroutines.ContainsKey(playerKey))
            {
                MelonCoroutines.Stop(_explodeLoopCoroutines[playerKey]);
            }

            _explodeLoopCoroutines[playerKey] = MelonCoroutines.Start(ExplodeLoopRoutine(playerInfo));
        }

        private void StopExplodeLoop(OnlinePlayerInfo playerInfo)
        {
            try
            {
                string playerKey = playerInfo.Player.GetInstanceID().ToString();

                // Stop the coroutine if it exists
                if (_explodeLoopCoroutines.ContainsKey(playerKey))
                {
                    MelonCoroutines.Stop(_explodeLoopCoroutines[playerKey]);
                    _explodeLoopCoroutines.Remove(playerKey);
                }

                // Reset explode loop state
                playerInfo.ExplodeLoop = false;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error stopping explosion loop: {ex.Message}");
            }
        }

        private IEnumerator ExplodeLoopRoutine(OnlinePlayerInfo playerInfo)
        {
            string playerKey = playerInfo.Player.GetInstanceID().ToString();

            while (playerInfo.ExplodeLoop && playerInfo.Player != null)
            {
                try
                {
                    // Create explosion at player position
                    Vector3 explosionPosition = playerInfo.Player.transform.position;
                    CreateServerSideExplosion(explosionPosition, 99999999999999f, 2f);
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error($"Error in explosion loop: {ex.Message}");
                }

                // Wait before next explosion
                yield return new WaitForSeconds(0.09f);
            }
            _explodeLoopCoroutines.Remove(playerKey);
        }

        private void SpawnVehicle(string[] args)
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

                ShowNotification("Vehicle", $"Spawned {vehicle}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error spawning vehicle: {ex.Message}");
                ShowNotification("Error", "Failed to spawn vehicle", NotificationType.Error);
            }
        }

        private void CreateExplosion(string[] args)
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
                    ShowNotification("Explosion", "No players found!", NotificationType.Error);
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
                LoggerInstance.Error($"Error creating explosion: {ex.Message}");
                ShowNotification("Error", "Failed to create explosion", NotificationType.Error);
            }
        }

        private void CreateServerSideExplosion(Vector3 position, float damage = 50f, float radius = 5f)
        {
            try
            {
                // Create explosion data
                ExplosionData explosionData = new ExplosionData(radius, damage, radius * 2.0f);

                // Get the CombatManager instance
                var combatManager = CombatManager.Instance;
                if (combatManager == null)
                {
                    LoggerInstance.Error("CombatManager instance is NULL!");
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
                    LoggerInstance.Error($"CreateExplosion failed: {createEx.Message}");
                }

                try
                {
                    // Method 2: Explicit Explosion method
                    combatManager.Explosion(position, explosionData, explosionId);
                }
                catch (Exception explodeEx)
                {
                    LoggerInstance.Error($"Explosion method failed: {explodeEx.Message}");
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
                    LoggerInstance.Error($"Observers RPC method failed: {observersEx.Message}");
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
                        LoggerInstance.Error("No explosion prefab found!");
                    }
                }
                catch (Exception prefabEx)
                {
                    LoggerInstance.Error($"Explosion prefab error: {prefabEx.Message}");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Server-side explosion error: {ex.Message}");
                ShowNotification("Error", "Failed to create server-side explosion", NotificationType.Error);
            }
        }

        // Add this method to handle toggling the feature
        private void ToggleAlwaysVisibleCrosshair(string[] args)
        {
            try
            {
                _forceCrosshairAlwaysVisible = !_forceCrosshairAlwaysVisible;

                // Apply patch if turning on, remove patch if turning off
                if (_forceCrosshairAlwaysVisible)
                {
                    ApplyCrosshairPatch();
                    ShowNotification("Crosshair", "Always visible crosshair enabled", NotificationType.Success);
                }
                else
                {
                    RemoveCrosshairPatch();
                    ShowNotification("Crosshair", "Always visible crosshair disabled", NotificationType.Info);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error toggling always visible crosshair: {ex.Message}");
                ShowNotification("Error", "Failed to toggle crosshair visibility", NotificationType.Error);
            }
        }

        // Add the Harmony patch setup
        private void ApplyCrosshairPatch()
        {
            try
            {
                // Find the HUD.SetCrosshairVisible method
                var hudType = typeof(Il2CppScheduleOne.UI.HUD);
                var setCrosshairMethod = hudType.GetMethod("SetCrosshairVisible",
                    BindingFlags.Public | BindingFlags.Instance);

                if (setCrosshairMethod == null)
                {
                    LoggerInstance.Error("Could not find SetCrosshairVisible method!");
                    return;
                }

                // Create and apply the prefix patch
                var patchMethod = typeof(Core).GetMethod("CrosshairVisibilityPatch",
                    BindingFlags.Static | BindingFlags.NonPublic);

                if (patchMethod == null)
                {
                    LoggerInstance.Error("CrosshairVisibilityPatch method not found!");
                    return;
                }

                _harmony.Patch(setCrosshairMethod, prefix: new HarmonyLib.HarmonyMethod(patchMethod));
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error applying crosshair patch: {ex.Message}");
            }
        }

        // Method to remove the patch
        private void RemoveCrosshairPatch()
        {
            try
            {
                // Find the HUD.SetCrosshairVisible method
                var hudType = typeof(Il2CppScheduleOne.UI.HUD);
                var setCrosshairMethod = hudType.GetMethod("SetCrosshairVisible",
                    BindingFlags.Public | BindingFlags.Instance);

                if (setCrosshairMethod == null)
                {
                    LoggerInstance.Error("Could not find SetCrosshairVisible method!");
                    return;
                }

                // Remove the patch
                _harmony.Unpatch(setCrosshairMethod, HarmonyPatchType.Prefix, _harmony.Id);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error removing crosshair patch: {ex.Message}");
            }
        }

        // The Harmony prefix patch method (must be static)
        private static bool CrosshairVisibilityPatch(ref bool vis)
        {
            // If called with false, modify to true to keep crosshair visible
            if (!vis)
            {
                vis = true;
            }
            // Return true to allow original method to run (with our modified parameter)
            return true;
        }

        #endregion

        #region UI Functions
        private void DrawPlayerExploitsUI(CommandCategory category)
        {
            try
            {
                float windowWidth = _windowRect.width - 40f;
                float windowHeight = _windowRect.height - 150f;

                // Player list panel at the top
                float playerPanelHeight = 120f; // Fixed height for player panel

                // Draw player list at the top
                Rect playerListRect = new Rect(20, 100, windowWidth, playerPanelHeight);
                GUI.Box(playerListRect, "", _panelStyle);

                // Player list header
                GUI.Label(
                    new Rect(playerListRect.x + 10, playerListRect.y + 10, 150, 20),
                    "Players Online",
                    _commandLabelStyle ?? _labelStyle
                );

                // Draw player entries in a horizontal layout
                DrawPlayerListHorizontal(playerListRect);

                // Commands section below player list
                _scrollPosition = GUI.BeginScrollView(
                    new Rect(20, 100 + playerPanelHeight + 10, windowWidth, windowHeight - playerPanelHeight - 10),
                    _scrollPosition,
                    new Rect(0, 0, windowWidth - 20, category.Commands.Count * 100f)
                );

                float yOffset = 0f;

                // Draw the standard commands
                foreach (var command in category.Commands)
                {
                    // Command container rectangle
                    Rect commandRect = new Rect(0, yOffset, windowWidth - 40f, 90f);
                    GUI.Box(commandRect, "", _panelStyle);

                    // Command Name
                    GUI.Label(
                        new Rect(commandRect.x + 10f, commandRect.y + 5f, 200f, 25f),
                        command.Name,
                        _commandLabelStyle ?? _labelStyle
                    );

                    // Parameters handling
                    float paramX = commandRect.x + 220f;
                    if (command.Parameters.Count > 0)
                    {
                        foreach (var param in command.Parameters)
                        {
                            Rect paramRect = new Rect(paramX, commandRect.y + 5f, 120f, 25f);

                            if (param.Type == ParameterType.Input)
                            {
                                // Unique key for each parameter
                                string paramKey = $"param_{command.Name}_{param.Name}";

                                // Custom text field
                                if (!_textFields.TryGetValue(paramKey, out var textField))
                                {
                                    textField = new CustomTextField(param.Value ?? "", _inputFieldStyle ?? GUI.skin.textField);
                                    _textFields[paramKey] = textField;
                                }

                                param.Value = textField.Draw(paramRect);
                            }
                            else if (param.Type == ParameterType.Dropdown)
                            {
                                // Dropdown-like button
                                if (GUI.Button(paramRect, param.Value ?? "Select", _buttonStyle))
                                {
                                    ShowDropdownMenu(param);
                                }
                            }

                            paramX += 130f;
                        }
                    }

                    // Execute Button
                    Rect executeRect = new Rect(paramX, commandRect.y + 5f, 120f, 25f);
                    if (GUI.Button(executeRect, "Execute", _buttonStyle))
                    {
                        ExecuteCommand(command);
                    }

                    // Optional description
                    if (!string.IsNullOrEmpty(command.Description))
                    {
                        GUI.Label(
                            new Rect(commandRect.x + 10f, commandRect.y + 35f, windowWidth - 60f, 50f),
                            command.Description,
                            _tooltipStyle ?? GUI.skin.label
                        );
                    }

                    // Increment Y offset for next command
                    yOffset += 100f;
                }

                GUI.EndScrollView();
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error in DrawPlayerExploitsUI: {ex}");
            }
        }

        private void DrawPlayerListHorizontal(Rect containerRect)
        {
            try
            {
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
                if (playerList == null || playerList.Count == 0)
                {
                    GUI.Label(
                        new Rect(containerRect.x + 10, containerRect.y + 30, 200, 25),
                        "No players found",
                        _labelStyle
                    );
                    return;
                }

                float contentY = containerRect.y + 40;

                foreach (var player in playerList)
                {
                    if (player == null) continue;

                    bool isLocal = IsLocalPlayer(player);
                    var playerHealth = GetPlayerHealth(player);

                    // Basic player card
                    Rect playerRect = new Rect(
                        containerRect.x + 10,
                        contentY,
                        containerRect.width - 20,
                        85
                    );

                    // Player card background - solid black for better contrast
                    GUI.color = new Color(0.1f, 0.1f, 0.15f, 1.0f);
                    GUI.DrawTexture(playerRect, Texture2D.whiteTexture);
                    GUI.color = Color.white;

                    // Player name
                    GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
                    nameStyle.fontSize = 18;
                    nameStyle.fontStyle = FontStyle.Bold;
                    nameStyle.normal.textColor = isLocal ? Color.cyan : Color.white;

                    GUI.Label(
                        new Rect(playerRect.x + 10, playerRect.y + 5, playerRect.width - 20, 25),
                        $"{player.name}{(isLocal ? " (YOU)" : "")}",
                        nameStyle
                    );

                    // ALIVE status on right side of name
                    if (playerHealth != null)
                    {
                        bool isAlive = playerHealth.IsAlive;

                        GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
                        statusStyle.fontSize = 16;
                        statusStyle.fontStyle = FontStyle.Bold;
                        statusStyle.normal.textColor = isAlive ? Color.green : Color.red;
                        statusStyle.alignment = TextAnchor.MiddleRight;

                        GUI.Label(
                            new Rect(playerRect.x + playerRect.width - 90, playerRect.y + 5, 80, 25),
                            isAlive ? "ALIVE" : "DEAD",
                            statusStyle
                        );
                    }

                    // Health display
                    if (playerHealth != null)
                    {
                        GUIStyle healthStyle = new GUIStyle(_labelStyle);
                        healthStyle.fontSize = 16;

                        // Only show the "Health:" label
                        GUI.Label(
                            new Rect(playerRect.x + 10, playerRect.y + 35, 50, 20),
                            "Health:",
                            healthStyle
                        );

                        // Make the health bar bigger and more prominent
                        Rect healthBarRect = new Rect(playerRect.x + 70, playerRect.y + 35, playerRect.width - 90, 20);
                        GUI.Box(healthBarRect, "", GUI.skin.box);

                        // Calculate percentage based on integer values to avoid any decimal issues
                        float healthPercent = (int)playerHealth.CurrentHealth / (float)(int)PlayerHealth.MAX_HEALTH;
                        Rect fillRect = new Rect(
                            healthBarRect.x + 2,
                            healthBarRect.y + 2,
                            (healthBarRect.width - 4) * healthPercent,
                            healthBarRect.height - 4
                        );

                        // Better color gradient from red to green
                        Color healthColor = Color.Lerp(Color.red, Color.green, healthPercent);
                        GUI.color = healthColor;
                        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
                        GUI.color = Color.white;
                    }

                    // SteamID at bottom
                    var playerInfo = _onlinePlayers.FirstOrDefault(p => p.Player == player);
                    if (playerInfo != null)
                    {
                        GUIStyle idStyle = new GUIStyle(GUI.skin.label);
                        idStyle.fontSize = 16;
                        idStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

                        GUI.Label(
                            new Rect(playerRect.x + 10, playerRect.y + 60, 300, 20),
                            playerInfo.SteamID,
                            idStyle
                        );

                        // Explode Loop Toggle
                        if (!isLocal)
                        {
                            Rect toggleRect = new Rect(playerRect.x + playerRect.width - 110, playerRect.y + 60, 100, 20);

                            // Custom toggle style with white text for better visibility
                            GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
                            toggleStyle.normal.textColor = Color.white;
                            toggleStyle.onNormal.textColor = Color.white;

                            bool newState = GUI.Toggle(
                                toggleRect,
                                playerInfo.ExplodeLoop,
                                "Explode Loop",
                                toggleStyle
                            );

                            if (newState != playerInfo.ExplodeLoop)
                            {
                                playerInfo.ExplodeLoop = newState;
                                if (newState)
                                    StartExplodeLoop(playerInfo);
                                else
                                    StopExplodeLoop(playerInfo);
                            }
                        }
                    }

                    contentY += 95; // Space between cards
                }

                // Global Explode Loop All button
                Rect explodeLoopAllRect = new Rect(
                    containerRect.x + 10,
                    contentY + 10,
                    containerRect.width - 20,
                    30
                );

                if (GUI.Button(explodeLoopAllRect, "Explode Loop All", _buttonStyle))
                {
                    foreach (var playerInfo in _onlinePlayers)
                    {
                        if (!playerInfo.IsLocal)
                        {
                            playerInfo.ExplodeLoop = true;
                            StartExplodeLoop(playerInfo);
                        }
                    }
                    ShowNotification("Players", "Started explosion loop on all players", NotificationType.Warning);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error drawing horizontal player list: {ex.Message}");
            }
        }
        #endregion


        #region Teleporter
        private GameObject mapTeleportObject;
        private Texture2D _mapTexture;
        private EMapRegion _hoveredRegion = EMapRegion.Downtown; // Default value
        private Dictionary<EMapRegion, Color> _regionColors = new Dictionary<EMapRegion, Color>();
        private Dictionary<EMapRegion, Rect> _regionRects = new Dictionary<EMapRegion, Rect>();
        private Dictionary<string, Vector3> _predefinedTeleports = new Dictionary<string, Vector3>();
        private Rect _mapRect;
        private Vector2 _lastClickPosition = Vector2.zero;
        private Vector2 _dragStartOffset;
        private Vector2 _dragStartPos;
        private Vector2 _mapPanOffset = Vector2.zero;
        private bool _mapInitialized = false;
        private bool _isCapturingMap = false;
        private bool _isDraggingMap = false;
        private float _mapZoom = 1.0f;
        private float _maxZoom = 2.5f;


        private EMapRegion GetRegionAtPosition(Vector2 normalizedPos)
        {
            foreach (var regionPair in _regionRects)
            {
                if (regionPair.Value.Contains(normalizedPos))
                {
                    return regionPair.Key;
                }
            }

            // No matching region found
            return (EMapRegion)(-1);
        }


        private void DrawTeleportManager()
        {
            try
            {
                float windowWidth = _windowRect.width - 40f;
                float windowHeight = _windowRect.height - 150f;

                // ADDED: Current player coordinates - placed below the tabs, centered
                var localPlayer = FindLocalPlayer();
                Vector3 currentPos = Vector3.zero;

                if (localPlayer != null)
                {
                    currentPos = localPlayer.transform.position;

                    // Create a style for coordinates
                    GUIStyle coordStyle = new GUIStyle(_labelStyle);
                    coordStyle.alignment = TextAnchor.MiddleCenter;
                    coordStyle.fontSize = 14;
                    coordStyle.fontStyle = FontStyle.Bold;

                    // Draw position text directly below tabs
                    Rect coordRect = new Rect(0, 90, _windowRect.width, 25);

                    // Format the coordinate text
                    string coordText = $"Current Position: X: {currentPos.x:F1}, Y: {currentPos.y:F1}, Z: {currentPos.z:F1}";

                    // Draw the text centered
                    GUI.Label(coordRect, coordText, coordStyle);
                }

                // Make the map smaller to accommodate coordinate controls in middle
                float mapHeight = windowHeight * 0.45f; // Reduced from 0.6f
                _mapRect = new Rect(20, 115, windowWidth, mapHeight);
                GUI.Box(_mapRect, "", _panelStyle);

                // Draw the map
                DrawInteractiveMap(_mapRect);

                // Check if we need to initialize the map - ADD AUTO LOADING
                if (!_mapInitialized && !_isCapturingMap)
                {
                    // Start map capture automatically
                    MelonCoroutines.Start(CaptureMapCoroutine());
                    _isCapturingMap = true;
                    LoggerInstance.Msg("Auto-starting map capture");
                }

                // MIDDLE SECTION - XYZ Controls
                float middleSectionHeight = 80;
                Rect middleSectionRect = new Rect(20, 115 + mapHeight + 10, windowWidth, middleSectionHeight);
                GUI.Box(middleSectionRect, "", _panelStyle);

                // X, Y, Z input fields - now moved to the middle and made more compact
                float inputWidth = (windowWidth - 100) / 3;
                float inputY = middleSectionRect.y + 10;

                // X field
                GUI.Label(
                    new Rect(middleSectionRect.x + 10, inputY + 5, 20, 25),
                    "X:",
                    _labelStyle
                );

                if (!_textFields.TryGetValue("teleport_x", out var xField))
                {
                    xField = new CustomTextField(localPlayer != null ? currentPos.x.ToString("F1") : "0",
                                                 _inputFieldStyle ?? GUI.skin.textField);
                    _textFields["teleport_x"] = xField;
                }
                xField.Draw(new Rect(middleSectionRect.x + 30, inputY, inputWidth, 25));

                // Y field
                GUI.Label(
                    new Rect(middleSectionRect.x + inputWidth + 40, inputY + 5, 20, 25),
                    "Y:",
                    _labelStyle
                );

                if (!_textFields.TryGetValue("teleport_y", out var yField))
                {
                    yField = new CustomTextField(localPlayer != null ? currentPos.y.ToString("F1") : "0",
                                                 _inputFieldStyle ?? GUI.skin.textField);
                    _textFields["teleport_y"] = yField;
                }
                yField.Draw(new Rect(middleSectionRect.x + inputWidth + 60, inputY, inputWidth, 25));

                // Z field
                GUI.Label(
                    new Rect(middleSectionRect.x + inputWidth * 2 + 70, inputY + 5, 20, 25),
                    "Z:",
                    _labelStyle
                );

                if (!_textFields.TryGetValue("teleport_z", out var zField))
                {
                    zField = new CustomTextField(localPlayer != null ? currentPos.z.ToString("F1") : "0",
                                                 _inputFieldStyle ?? GUI.skin.textField);
                    _textFields["teleport_z"] = zField;
                }
                zField.Draw(new Rect(middleSectionRect.x + inputWidth * 2 + 90, inputY, inputWidth, 25));

                // Button row
                float buttonY = inputY + 35;
                float buttonWidth = 150;
                float buttonSpacing = 30;
                float startX = middleSectionRect.x + (windowWidth - (buttonWidth * 2 + buttonSpacing)) / 2;

                // Teleport button
                if (GUI.Button(
                    new Rect(startX, buttonY, buttonWidth, 25),
                    "Teleport to Coordinates",
                    _buttonStyle))
                {
                    try
                    {
                        float x = float.Parse(xField.Value);
                        float y = float.Parse(yField.Value);
                        float z = float.Parse(zField.Value);

                        TeleportPlayer(new Vector3(x, y, z));
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Error($"Error parsing coordinates: {ex.Message}");
                        ShowNotification("Error", "Invalid coordinates", NotificationType.Error);
                    }
                }

                // Update position button - simplified to use current coordinates directly
                if (GUI.Button(
                    new Rect(startX + buttonWidth + buttonSpacing, buttonY, buttonWidth, 25),
                    "Use Current Position",
                    _buttonStyle) && localPlayer != null)
                {
                    // Copy to input fields
                    if (_textFields.TryGetValue("teleport_x", out var xF))
                        xF.Value = currentPos.x.ToString("F1");

                    if (_textFields.TryGetValue("teleport_y", out var yF))
                        yF.Value = currentPos.y.ToString("F1");

                    if (_textFields.TryGetValue("teleport_z", out var zF))
                        zF.Value = currentPos.z.ToString("F1");

                    ShowNotification("Position", "Current position updated in input fields", NotificationType.Info);
                }

                // Predefined teleports - takes bottom portion, now smaller with more space for the middle section
                float teleportListHeight = windowHeight - mapHeight - middleSectionHeight - 40;
                Rect teleportListRect = new Rect(20, middleSectionRect.y + middleSectionHeight + 10, windowWidth, teleportListHeight);
                GUI.Box(teleportListRect, "", _panelStyle);

                // Title and refresh button in single row
                GUI.Label(
                    new Rect(teleportListRect.x + 10, teleportListRect.y + 5, 200, 25),
                    "Predefined Teleports",
                    _commandLabelStyle ?? _labelStyle
                );

                // Refresh button
                if (GUI.Button(
                    new Rect(teleportListRect.x + teleportListRect.width - 100, teleportListRect.y + 5, 80, 25),
                    "Refresh",
                    _buttonStyle))
                {
                    InitializePredefinedTeleports();
                    ShowNotification("Teleport", "Teleport locations refreshed", NotificationType.Info);
                }

                // Draw the predefined teleport buttons in a scrollable area - now smaller
                float teleportButtonsY = teleportListRect.y + 35;
                float teleportButtonsHeight = teleportListHeight - 45; // Leave space for header
                Rect teleportButtonsRect = new Rect(
                    teleportListRect.x + 10,
                    teleportButtonsY,
                    teleportListRect.width - 20,
                    teleportButtonsHeight
                );

                // Add a scroll view for the teleport locations
                _scrollPosition = GUI.BeginScrollView(
                    teleportButtonsRect,
                    _scrollPosition,
                    new Rect(0, 0, teleportButtonsRect.width - 20, _predefinedTeleports.Count * 30)
                );

                int i = 0;
                foreach (var teleport in _predefinedTeleports)
                {
                    Rect buttonRect = new Rect(5, i * 30, teleportButtonsRect.width - 30, 25);
                    if (GUI.Button(buttonRect, teleport.Key, _buttonStyle))
                    {
                        TeleportPlayer(teleport.Value);
                        ShowNotification("Teleport", $"Teleported to {teleport.Key}", NotificationType.Success);
                    }
                    i++;
                }

                GUI.EndScrollView();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in DrawTeleportManager: {ex.Message}");
            }
        }

        private float GetGroundHeight(Vector3 xzPosition)
        {
            // Start raycast from high above to ensure we're above any terrain
            float raycastHeight = 1000f;
            Vector3 rayStart = new Vector3(xzPosition.x, raycastHeight, xzPosition.z);
            RaycastHit hit;

            // Use a proper layer mask for ground detection
            // Adjust these layer names based on your actual project's layer setup
            int groundLayer = LayerMask.GetMask("Ground", "Terrain", "Default");

            // Perform the raycast
            if (Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Infinity, groundLayer))
            {
                // Add a small offset to prevent players from sinking into the ground
                float heightOffset = 2.0f;
                return hit.point.y + heightOffset;
            }

            if (Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Infinity))
            {
                // Add offset for non-terrain objects too
                return hit.point.y + 2.0f;
            }

            LoggerInstance.Error($"No ground detected at position {xzPosition}, using fallback height");
            return 3f; // Or some other safe default height for your game world
        }

        private void TeleportTargetPlayer(Il2CppScheduleOne.PlayerScripts.Player targetPlayer, Vector3 position)
        {
            try
            {
                if (targetPlayer == null)
                {
                    LoggerInstance.Error("Target player is null!");
                    ShowNotification("Error", "Target player not found", NotificationType.Error);
                    return;
                }

                // Ensure position is valid
                if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z) ||
                    float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z))
                {
                    LoggerInstance.Error("Invalid teleport position!");
                    ShowNotification("Error", "Invalid teleport position", NotificationType.Error);
                    return;
                }

                // Method 1: Try to get PlayerMovement from target player
                var playerMovement = targetPlayer.GetComponent<Il2CppScheduleOne.PlayerScripts.PlayerMovement>();
                if (playerMovement != null)
                {
                    // Direct call to the player's Teleport method
                    LoggerInstance.Msg($"Teleporting {targetPlayer.name} using PlayerMovement.Teleport");
                    playerMovement.Teleport(position);
                    ShowNotification("Teleport", $"Teleported {targetPlayer.name} using movement teleport", NotificationType.Success);
                    return;
                }

                // Method 2: Try to use teleport fields if available
                try
                {
                    // Set player's teleport flag and position
                    var teleportField = targetPlayer.GetType().GetField("teleport", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var teleportPosField = targetPlayer.GetType().GetField("teleportPosition", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (teleportField != null && teleportPosField != null)
                    {
                        // Set teleport flag to true
                        teleportField.SetValue(targetPlayer, true);
                        // Set teleport position
                        teleportPosField.SetValue(targetPlayer, position);

                        LoggerInstance.Msg($"Teleporting {targetPlayer.name} using teleport fields");
                        ShowNotification("Teleport", $"Set teleport flag and position for {targetPlayer.name}", NotificationType.Success);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error($"Error accessing teleport fields: {ex.Message}");
                }

                // Method 3: As a last resort, try to directly set transform position
                LoggerInstance.Msg($"Fallback: Directly setting {targetPlayer.name}'s position");
                targetPlayer.transform.position = position;

                // Try to force position sync if possible
                var netObj = targetPlayer.GetComponent<Il2CppFishNet.Object.NetworkObject>();
                if (netObj != null)
                {
                    // Try to call any transform dirty method
                    var dirtyTransformMethod = netObj.GetType().GetMethods()
                        .FirstOrDefault(m => m.Name.Contains("Transform") && m.Name.Contains("Dirty"));

                    if (dirtyTransformMethod != null)
                    {
                        dirtyTransformMethod.Invoke(netObj, null);
                        LoggerInstance.Msg("Called transform dirty method");
                    }
                }

                ShowNotification("Teleport", $"Direct position set for {targetPlayer.name}", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error teleporting target player: {ex.Message}");
                ShowNotification("Error", "Teleport failed: " + ex.Message, NotificationType.Error);
            }
        }

        private void GenerateDebugInfoCommand(string[] args)
        {
            try
            {
                StringBuilder debugInfo = new StringBuilder();
                debugInfo.AppendLine("=== MODERN CHEAT MENU DEBUG INFORMATION ===");
                debugInfo.AppendLine($"Generated: {DateTime.Now}");
                debugInfo.AppendLine();

                // System information
                debugInfo.AppendLine("=== SYSTEM INFORMATION ===");
                debugInfo.AppendLine($"OS: {SystemInfo.operatingSystem}");
                debugInfo.AppendLine($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
                debugInfo.AppendLine($"RAM: {SystemInfo.systemMemorySize} MB");
                debugInfo.AppendLine($"GPU: {SystemInfo.graphicsDeviceName}");
                debugInfo.AppendLine($"GPU API: {SystemInfo.graphicsDeviceType}");
                debugInfo.AppendLine($"GPU Memory: {SystemInfo.graphicsMemorySize} MB");
                debugInfo.AppendLine($"GPU Driver: {SystemInfo.graphicsDeviceVersion}");
                debugInfo.AppendLine($"Screen Resolution: {Screen.currentResolution.width}x{Screen.currentResolution.height} @{Screen.currentResolution.refreshRate}Hz");
                debugInfo.AppendLine($"Current DPI: {Screen.dpi}");
                debugInfo.AppendLine($"Device Unique ID: {SystemInfo.deviceUniqueIdentifier}");
                debugInfo.AppendLine($"Device Model: {SystemInfo.deviceModel}");
                debugInfo.AppendLine($"HWID (Spoofed): {_generatedHwid}");
                debugInfo.AppendLine();

                // MelonLoader information
                debugInfo.AppendLine("=== MELONLOADER INFORMATION ===");
                try
                {
                    //debugInfo.AppendLine($"MelonLoader Version: {MelonLoader.BuildInfo.Version}");
                    //debugInfo.AppendLine($"MelonLoader Hash: {MelonLoader.BuildInfo.Hash}");
                    //debugInfo.AppendLine($"Game Assembly: {MelonLoader.BuildInfo.GameAssembly}");
                    //debugInfo.AppendLine($"Is Game IL2CPP: {MelonLoader.InternalUtils.UnhollowerSupport.IsGameIl2Cpp()}");

                    var melonAssembly = typeof(MelonLoader.MelonMod).Assembly;
                    debugInfo.AppendLine($"MelonLoader Assembly: {melonAssembly.GetName().Name} v{melonAssembly.GetName().Version}");

                    // List loaded mods
                    var loadedMods = MelonLoader.MelonBase.RegisteredMelons;
                    if (loadedMods != null && loadedMods.Count > 0)
                    {
                        debugInfo.AppendLine($"Loaded Mods ({loadedMods.Count}):");
                        foreach (var mod in loadedMods)
                        {
                            debugInfo.AppendLine($"  - {mod.Info.Name} v{mod.Info.Version} by {mod.Info.Author}");
                        }
                    }
                    else
                    {
                        debugInfo.AppendLine("No other mods loaded");
                    }
                }
                catch (Exception ex)
                {
                    debugInfo.AppendLine($"Error retrieving MelonLoader info: {ex.Message}");
                }
                debugInfo.AppendLine();

                // Game information
                debugInfo.AppendLine("=== GAME INFORMATION ===");
                debugInfo.AppendLine($"Game Name: {Modern_Cheat_Menu.ModInfo.NameOfGame}");
                debugInfo.AppendLine($"Game Developers: {Modern_Cheat_Menu.ModInfo.GameDevelopers}");
                debugInfo.AppendLine($"Unity Version: {Application.unityVersion}");
                debugInfo.AppendLine($"Game Version: {Application.version}");
                debugInfo.AppendLine($"Game Data Path: {Application.dataPath}");
                debugInfo.AppendLine($"Product Name: {Application.productName}");
                debugInfo.AppendLine($"Company Name: {Application.companyName}");
                debugInfo.AppendLine($"Target Frame Rate: {Application.targetFrameRate}");
                debugInfo.AppendLine($"Is Focused: {Application.isFocused}");
                debugInfo.AppendLine($"Is Playing: {Application.isPlaying}");
                debugInfo.AppendLine($"Is Background: {Application.runInBackground}");
                debugInfo.AppendLine($"Quality Level: {QualitySettings.GetQualityLevel()}");
                debugInfo.AppendLine();

                // Mod information
                debugInfo.AppendLine("=== MOD INFORMATION ===");
                debugInfo.AppendLine($"Menu Name: {Modern_Cheat_Menu.ModInfo.Name}");
                debugInfo.AppendLine($"Menu Version: {Modern_Cheat_Menu.ModInfo.Version}");
                debugInfo.AppendLine($"Author: {Modern_Cheat_Menu.ModInfo.Author}");
                debugInfo.AppendLine($"Repository: {Modern_Cheat_Menu.ModInfo.RepositoryUrl}");
                debugInfo.AppendLine($"UI Initialized: {_isInitialized}");
                debugInfo.AppendLine($"UI Scale: {_uiScale}");
                debugInfo.AppendLine($"UI Opacity: {_uiOpacity}");
                debugInfo.AppendLine($"Animations Enabled: {_enableAnimations}");
                debugInfo.AppendLine($"Window Position: X={_windowRect.x}, Y={_windowRect.y}, W={_windowRect.width}, H={_windowRect.height}");
                debugInfo.AppendLine();

                // Active features
                debugInfo.AppendLine("=== FEATURE STATUS ===");
                debugInfo.AppendLine($"Godmode: {_playerGodmodeEnabled}");
                debugInfo.AppendLine($"Never Wanted: {_playerNeverWantedEnabled}");
                debugInfo.AppendLine($"Free Camera: {_freeCamEnabled}");
                debugInfo.AppendLine($"Unlimited Ammo: {_unlimitedAmmoEnabled}");
                debugInfo.AppendLine($"Aimbot: {_aimbotEnabled}");
                debugInfo.AppendLine($"Perfect Accuracy: {_perfectAccuracyEnabled}");
                debugInfo.AppendLine($"No Recoil: {_noRecoilEnabled}");
                debugInfo.AppendLine($"One Hit Kill: {_oneHitKillEnabled}");
                debugInfo.AppendLine($"NPCs Pacified: {_npcsPacifiedEnabled}");
                debugInfo.AppendLine($"Crosshair Always Visible: {_forceCrosshairAlwaysVisible}");
                debugInfo.AppendLine();

                // Current player information
                var localPlayer = FindLocalPlayer();
                if (localPlayer != null)
                {
                    debugInfo.AppendLine("=== PLAYER INFORMATION ===");
                    debugInfo.AppendLine($"Player Name: {localPlayer.name}");
                    debugInfo.AppendLine($"Position: X={localPlayer.transform.position.x:F2}, Y={localPlayer.transform.position.y:F2}, Z={localPlayer.transform.position.z:F2}");
                    debugInfo.AppendLine($"Rotation: X={localPlayer.transform.rotation.eulerAngles.x:F2}, Y={localPlayer.transform.rotation.eulerAngles.y:F2}, Z={localPlayer.transform.rotation.eulerAngles.z:F2}");

                    var playerHealth = GetPlayerHealth(localPlayer);
                    if (playerHealth != null)
                    {
                        debugInfo.AppendLine($"Health: {playerHealth.CurrentHealth}/{PlayerHealth.MAX_HEALTH}");
                        debugInfo.AppendLine($"Is Alive: {playerHealth.IsAlive}");
                    }

                    var playerMovement = localPlayer.GetComponent<Il2CppScheduleOne.PlayerScripts.PlayerMovement>();
                    if (playerMovement != null)
                    {
                        debugInfo.AppendLine($"Movement Speed: {PlayerMovement.WalkSpeed}");
                        debugInfo.AppendLine($"Sprint Multiplier: {PlayerMovement.SprintMultiplier}");
                        debugInfo.AppendLine($"Jump Force: {playerMovement.jumpForce}");
                        debugInfo.AppendLine($"Gravity Multiplier: {PlayerMovement.GravityMultiplier}");
                        debugInfo.AppendLine($"Is Grounded: {playerMovement.IsGrounded}");
                        debugInfo.AppendLine($"Is Crouched: {playerMovement.isCrouched}");
                        debugInfo.AppendLine($"Is Sprinting: {playerMovement.isSprinting}");
                        debugInfo.AppendLine($"Current Stamina: {playerMovement.CurrentStaminaReserve}");
                    }

                    // Network object info
                    var netObj = localPlayer.GetComponent<Il2CppFishNet.Object.NetworkObject>();
                    if (netObj != null)
                    {
                        debugInfo.AppendLine($"Network ID: {netObj.ObjectId}");
                        debugInfo.AppendLine($"Owner ID: {netObj.OwnerId}");
                        debugInfo.AppendLine($"Is Owner: {netObj.IsOwner}");
                        debugInfo.AppendLine($"Is Server: {netObj.IsServer}");
                        debugInfo.AppendLine($"Is Spawned: {netObj.IsSpawned}");
                    }

                    debugInfo.AppendLine();
                }

                // Online players
                debugInfo.AppendLine("=== ONLINE INFORMATION ===");
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
                debugInfo.AppendLine($"Total Players: {(playerList != null ? playerList.Count : 0)}");
                debugInfo.AppendLine($"Server Socket Found: {(_discoveredServerSocket != null)}");

                // List all players
                if (playerList != null && playerList.Count > 0)
                {
                    debugInfo.AppendLine("\nPlayer List:");
                    foreach (var player in playerList)
                    {
                        if (player == null) continue;
                        debugInfo.AppendLine($"- {player.name} (Local: {IsLocalPlayer(player)})");

                        var netObj = player.GetComponent<Il2CppFishNet.Object.NetworkObject>();
                        if (netObj != null)
                        {
                            debugInfo.AppendLine($"  Network ID: {netObj.ObjectId}, Owner ID: {netObj.OwnerId}");
                        }
                    }
                }

                // Server transport information
                var transports = Resources.FindObjectsOfTypeAll<Il2CppFishySteamworks.FishySteamworks>();
                if (transports != null && transports.Length > 0)
                {
                    var transport = transports[0];
                    debugInfo.AppendLine("\nTransport Information:");
                    debugInfo.AppendLine($"Transport Type: {transport.GetType().Name}");
                    debugInfo.AppendLine($"Max Clients: {transport._maximumClients}");

                    if (transport._server != null)
                    {
                        debugInfo.AppendLine($"Server Socket: {transport._server.GetType().Name}");
                        debugInfo.AppendLine($"Server Max Clients: {transport._server._maximumClients}");

                        try
                        {
                            if (transport._server._steamIds != null)
                            {
                                debugInfo.AppendLine($"Connected Steam IDs: {transport._server._steamIds.Count}");
                            }
                        }
                        catch (Exception ex)
                        {
                            debugInfo.AppendLine($"Error accessing steam IDs: {ex.Message}");
                        }
                    }
                }

                // Cached items information
                debugInfo.AppendLine("\n=== CACHE INFORMATION ===");
                debugInfo.AppendLine($"Total Items: {_itemDictionary.Count}");
                debugInfo.AppendLine($"Quality Items: {_qualitySupportCache.Count(kv => kv.Value)}");
                debugInfo.AppendLine($"Vehicle Types: {_vehicleCache.Count}");

                // Exception handling info
                debugInfo.AppendLine("\n=== RUNTIME INFO ===");
                debugInfo.AppendLine($"Current Culture: {System.Globalization.CultureInfo.CurrentCulture.Name}");
                debugInfo.AppendLine($"Current UI Culture: {System.Globalization.CultureInfo.CurrentUICulture.Name}");
                //debugInfo.AppendLine($"Thread Count: {System.Threading.Process.GetCurrentProcess().Threads.Count}");
                debugInfo.AppendLine($"CLR Version: {System.Environment.Version}");
                debugInfo.AppendLine($"Process Start Time: {System.Diagnostics.Process.GetCurrentProcess().StartTime}");
                debugInfo.AppendLine($"Process Working Set: {System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MB");
                debugInfo.AppendLine($"FPS: {1.0f / Time.deltaTime:F1}");
                debugInfo.AppendLine($"Time.time: {Time.time:F1}");
                debugInfo.AppendLine($"Time.unscaledTime: {Time.unscaledTime:F1}");
                debugInfo.AppendLine($"Time.timeScale: {Time.timeScale:F2}");

                // Save the debug information to a file in the game directory
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filePath = Path.Combine(Application.dataPath, $"MCM_Debug_{timestamp}.txt");
                File.WriteAllText(filePath, debugInfo.ToString());

                // Also copy to clipboard for easy sharing
                GUIUtility.systemCopyBuffer = debugInfo.ToString();

                ShowNotification("Debug Info", $"Debug information generated and saved to:\n{filePath}\nAlso copied to clipboard!", NotificationType.Success);
                LoggerInstance.Msg($"Debug information saved to: {filePath}");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error generating debug information: {ex.Message}");
                ShowNotification("Error", "Failed to generate debug information", NotificationType.Error);
            }
        }

        private void TeleportPlayer(Vector3 position)
        {
            try
            {
                var localPlayer = FindLocalPlayer();
                if (localPlayer == null)
                {
                    LoggerInstance.Error("Failed to find local player for teleportation!");
                    ShowNotification("Teleport", "Player not found", NotificationType.Error);
                    return;
                }

                // Directly set player position
                localPlayer.transform.position = position;

                // Also update text fields with new position for reference
                if (_textFields.TryGetValue("teleport_x", out var xField))
                    xField.Value = position.x.ToString("F1");
                if (_textFields.TryGetValue("teleport_y", out var yField))
                    yField.Value = position.y.ToString("F1");
                if (_textFields.TryGetValue("teleport_z", out var zField))
                    zField.Value = position.z.ToString("F1");

                ShowNotification("Teleport", "Teleported successfully", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error teleporting player: {ex.Message}");
                ShowNotification("Error", "Failed to teleport player", NotificationType.Error);
            }
        }

        private void DrawInteractiveMap(Rect mapRect)
        {
            try
            {
                // First, define the container area
                Rect strictMapContainer = new Rect(
                    mapRect.x + 5,
                    mapRect.y + 5,
                    mapRect.width - 10,
                    mapRect.height - 10
                );

                // Draw background
                GUI.color = new Color(0.1f, 0.1f, 0.12f, 1f);
                GUI.DrawTexture(strictMapContainer, Texture2D.whiteTexture);
                GUI.color = Color.white;

                if (_mapTexture != null)
                {
                    // Calculate aspect ratio
                    float texRatio = (float)_mapTexture.width / _mapTexture.height;
                    float rectRatio = strictMapContainer.width / strictMapContainer.height;

                    Rect baseDisplayRect;
                    if (texRatio > rectRatio)
                    {
                        // Fit width
                        float adjustedHeight = strictMapContainer.width / texRatio;
                        baseDisplayRect = new Rect(
                            strictMapContainer.x,
                            strictMapContainer.y + (strictMapContainer.height - adjustedHeight) / 2,
                            strictMapContainer.width,
                            adjustedHeight
                        );
                    }
                    else
                    {
                        // Fit height
                        float adjustedWidth = strictMapContainer.height * texRatio;
                        baseDisplayRect = new Rect(
                            strictMapContainer.x + (strictMapContainer.width - adjustedWidth) / 2,
                            strictMapContainer.y,
                            adjustedWidth,
                            strictMapContainer.height
                        );
                    }

                    // Apply zoom and pan
                    Rect displayRect = ApplyZoomAndPan(baseDisplayRect, strictMapContainer);

                    // Begin clip area to prevent drawing outside container
                    GUI.BeginClip(strictMapContainer);

                    // Adjust coordinates for clipping
                    Rect adjustedDisplayRect = new Rect(
                        displayRect.x - strictMapContainer.x,
                        displayRect.y - strictMapContainer.y,
                        displayRect.width,
                        displayRect.height
                    );

                    // Draw the map texture
                    GUI.DrawTexture(adjustedDisplayRect, _mapTexture, ScaleMode.StretchToFill);

                    // Draw regions and other map elements - adjusted for clipping
                    DrawMapElementsClipped(adjustedDisplayRect);

                    // End clip area
                    GUI.EndClip();

                    // Draw zoom controls (outside the clipped area)
                    DrawZoomControls(mapRect);

                    // Handle mouse interactions (outside the clipped area)
                    HandleMapInteraction(strictMapContainer, displayRect, baseDisplayRect);
                }
                else
                {
                    // No map texture available yet
                    GUIStyle msgStyle = new GUIStyle(GUI.skin.label);
                    msgStyle.alignment = TextAnchor.MiddleCenter;
                    msgStyle.fontSize = 16;
                    GUI.Label(strictMapContainer, _isCapturingMap ? "Loading map data...." : "Map not available", msgStyle);

                    if (!_isCapturingMap && GUI.Button(
                        new Rect(strictMapContainer.x + strictMapContainer.width / 2 - 60,
                                 strictMapContainer.y + strictMapContainer.height / 2 + 30,
                                 120, 30),
                        "Capture Map", _buttonStyle))
                    {
                        MelonCoroutines.Start(CaptureMapCoroutine());
                        _isCapturingMap = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in DrawInteractiveMap: {ex.Message}");
            }
        }

        private void DrawMapElementsClipped(Rect displayRect)
        {
            // Draw region overlays
            foreach (var regionRect in _regionRects)
            {
                // Scale region rect to display
                Rect scaledRect = new Rect(
                    displayRect.x + regionRect.Value.x * displayRect.width,
                    displayRect.y + regionRect.Value.y * displayRect.height,
                    regionRect.Value.width * displayRect.width,
                    regionRect.Value.height * displayRect.height
                );

                // Determine color and opacity
                Color regionColor = _regionColors.ContainsKey(regionRect.Key) ?
                    _regionColors[regionRect.Key] :
                    new Color(0.5f, 0.5f, 0.5f, 0.7f);

                float opacity = (regionRect.Key == _hoveredRegion) ? 0.7f : 0.4f;
                GUI.color = new Color(regionColor.r, regionColor.g, regionColor.b, opacity);

                // Draw region
                GUI.Box(scaledRect, "", _panelStyle ?? GUI.skin.box);
                GUI.color = Color.white;

                // Draw region name
                string regionName = "";
                var mapInstance = Map.Instance;
                if (mapInstance != null)
                {
                    var regionData = mapInstance.GetRegionData(regionRect.Key);
                    if (regionData != null)
                    {
                        regionName = regionData.Name;
                    }
                }

                if (!string.IsNullOrEmpty(regionName))
                {
                    GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
                    nameStyle.alignment = TextAnchor.MiddleCenter;
                    nameStyle.fontStyle = FontStyle.Bold;
                    nameStyle.normal.textColor = Color.white;
                    nameStyle.fontSize = Mathf.Max(10, Mathf.FloorToInt(14 * _mapZoom));

                    GUI.Label(scaledRect, regionName, nameStyle);
                }
            }

            // Draw marker for last clicked position
            if (_lastClickPosition != Vector2.zero)
            {
                // Convert normalized position to display coordinates
                Vector2 markerPos = new Vector2(
                    displayRect.x + _lastClickPosition.x * displayRect.width,
                    displayRect.y + _lastClickPosition.y * displayRect.height
                );

                // Size of the marker
                //float markerSize = 8f * _mapZoom;
                //Rect markerRect = new Rect(
                //    markerPos.x - markerSize / 2,
                //    markerPos.y - markerSize / 2,
                //    markerSize,
                //    markerSize
                //);

                //// Draw a red dot
                //GUI.color = Color.red;
                //GUI.DrawTexture(markerRect, Texture2D.whiteTexture);
                //GUI.color = Color.white;
            }
        }

        private void HandleMapInteraction(Rect mapContainer, Rect displayRect, Rect baseRect)
        {
            // Check if mouse is over the map
            if (mapContainer.Contains(Event.current.mousePosition))
            {
                // Calculate relative mouse position
                Vector2 normalizedPos = new Vector2(
                    Mathf.Clamp01((Event.current.mousePosition.x - displayRect.x) / displayRect.width),
                    Mathf.Clamp01((Event.current.mousePosition.y - displayRect.y) / displayRect.height)
                );

                // Determine hovered region
                _hoveredRegion = GetRegionAtPosition(normalizedPos);

                // Handle middle-click drag (panning)
                if (Event.current.type == EventType.MouseDown && Event.current.button == 2)
                {
                    _isDraggingMap = true;
                    _dragStartPos = Event.current.mousePosition;
                    _dragStartOffset = _mapPanOffset;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseUp && Event.current.button == 2)
                {
                    _isDraggingMap = false;
                    Event.current.Use();
                }

                // Handle drag movement
                if (_isDraggingMap && Event.current.type == EventType.MouseDrag)
                {
                    Vector2 delta = Event.current.mousePosition - _dragStartPos;
                    _mapPanOffset = _dragStartOffset + delta / _mapZoom;
                    Event.current.Use();
                }

                // Handle scroll wheel zooming
                if (Event.current.type == EventType.ScrollWheel)
                {
                    // Only allow zooming in if already at or below default zoom
                    if (Event.current.delta.y > 0 && _mapZoom <= 1.0f)
                    {
                        // Don't allow zooming out more
                    }
                    else
                    {
                        float zoomDelta = -Event.current.delta.y * 0.05f;
                        float newZoom = Mathf.Clamp(_mapZoom + zoomDelta, 1.0f, _maxZoom);

                        // Adjust pan to zoom toward mouse position
                        Vector2 mousePos = Event.current.mousePosition;
                        Vector2 mapCenter = new Vector2(
                            baseRect.x + baseRect.width / 2,
                            baseRect.y + baseRect.height / 2
                        );

                        _mapZoom = newZoom;
                    }

                    Event.current.Use();
                }

                // Handle clicking for teleportation
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    _lastClickPosition = normalizedPos;

                    // Use a more direct world coordinate mapping method
                    Vector3 worldPos = MapToWorldPosition(normalizedPos, mapContainer, displayRect);
                    TeleportPlayer(worldPos);

                    Event.current.Use();
                }
            }
            else
            {
                _hoveredRegion = (EMapRegion)(-1);

                // Cancel dragging if mouse leaves map
                if (_isDraggingMap && Event.current.type == EventType.MouseUp)
                {
                    _isDraggingMap = false;
                }
            }
        }

        private Vector3 MapToWorldPosition(Vector2 normalizedPos, Rect mapContainer, Rect displayRect)
        {
            try
            {
                // Get current player position for height reference
                var localPlayer = FindLocalPlayer();
                if (localPlayer == null)
                {
                    LoggerInstance.Error("Could not find local player!");
                    return Vector3.zero;
                }

                /* 
                 * I think I figured out the game's map coordinates relative to world position 
                 * After spending countless hours analyzing game functions and the various data containers associated with them.
                 * I've managed to discern that the map dimensions are 2048x2048, with a scale factor of 5.006356f, the formula is as follows:
                 * Take X position and subtract it by 0.5f, times it by the X map dimension and then divide it by the scale factor.
                 * The same applies to the Z coordinate, awfully strange how the game developer chose to use the Z axis for lateral movement instead of the Y position.
                 * I guess this approach is best for a 3D space, I wouldn't know.
                 * Example: (positonX - 0.5f) * 2048 / 5.006356, (0.5f - positionZ) * 2048 / 5.006356.
                 * This seems to produce the best results, but it's still not quite accurate, but this could be due to the scaling factor of the UI relative to the mouse click input.
                 * Who knows, who cares, this is good enough for me, if you have a better method, please share it with me. 
                 */
                Vector2 _mapDimensions = new Vector2(2048f, 2048f);
                float _scaleFactor = 5.006356f;

                float worldX = (normalizedPos.x - 0.5f) * _mapDimensions.x / _scaleFactor;
                float worldZ = (0.5f - normalizedPos.y) * _mapDimensions.y / _scaleFactor;

                Vector3 raycastStart = new Vector3(worldX, 100f, worldZ);
                float height = GetGroundHeight(raycastStart);

                return new Vector3(worldX, height, worldZ);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error converting map coordinates: {ex.Message}");
                return Vector3.zero;
            }
        }

        private Rect ApplyZoomAndPan(Rect baseRect, Rect containerRect)
        {
            // Calculate the zoom-adjusted size
            float zoomedWidth = baseRect.width * _mapZoom;
            float zoomedHeight = baseRect.height * _mapZoom;

            // Calculate center position
            float centerX = baseRect.x + baseRect.width / 2;
            float centerY = baseRect.y + baseRect.height / 2;

            // Apply pan offset with improved constraints
            float maxPanX = Math.Max(0, (zoomedWidth - containerRect.width) / 2);
            float maxPanY = Math.Max(0, (zoomedHeight - containerRect.height) / 2);

            // Clamp pan offset to prevent going outside container bounds
            _mapPanOffset.x = Mathf.Clamp(_mapPanOffset.x, -maxPanX, maxPanX);
            _mapPanOffset.y = Mathf.Clamp(_mapPanOffset.y, -maxPanY, maxPanY);

            centerX += _mapPanOffset.x;
            centerY += _mapPanOffset.y;

            // Calculate the zoomed rect with the adjusted center
            Rect zoomedRect = new Rect(
                centerX - zoomedWidth / 2,
                centerY - zoomedHeight / 2,
                zoomedWidth,
                zoomedHeight
            );

            // Improved container bounds checking
            if (zoomedWidth <= containerRect.width)
            {
                // Center horizontally if smaller than container
                zoomedRect.x = containerRect.x + (containerRect.width - zoomedWidth) / 2;
            }
            else
            {
                // Constrain to container edges
                if (zoomedRect.x > containerRect.x)
                    zoomedRect.x = containerRect.x;
                if (zoomedRect.x + zoomedWidth < containerRect.x + containerRect.width)
                    zoomedRect.x = containerRect.x + containerRect.width - zoomedWidth;
            }

            if (zoomedHeight <= containerRect.height)
            {
                // Center vertically if smaller than container
                zoomedRect.y = containerRect.y + (containerRect.height - zoomedHeight) / 2;
            }
            else
            {
                // Constrain to container edges
                if (zoomedRect.y > containerRect.y)
                    zoomedRect.y = containerRect.y;
                if (zoomedRect.y + zoomedHeight < containerRect.y + containerRect.height)
                    zoomedRect.y = containerRect.y + containerRect.height - zoomedHeight;
            }

            return zoomedRect;
        }

        private void InitializePredefinedTeleports()
        {
            _predefinedTeleports.Clear();

            try
            {
                var mapInstance = Map.Instance;
                if (mapInstance == null) return;

                // Add special locations
                if (mapInstance.PoliceStation != null)
                {
                    _predefinedTeleports["Police Station"] = mapInstance.PoliceStation.transform.position;
                }

                if (mapInstance.MedicalCentre != null)
                {
                    _predefinedTeleports["Medical Centre"] = mapInstance.MedicalCentre.transform.position;
                }

                // Add region-based teleports
                var regions = mapInstance.Regions;
                if (regions != null)
                {
                    foreach (var region in regions)
                    {
                        if (region != null && !string.IsNullOrEmpty(region.Name))
                        {
                            // For regions with delivery locations, add those as teleport points
                            var deliveryLocations = region.RegionDeliveryLocations;
                            if (deliveryLocations != null && deliveryLocations.Length > 0)
                            {
                                int count = 0;
                                foreach (var location in deliveryLocations)
                                {
                                    if (location != null && location.transform != null)
                                    {
                                        count++;
                                        string locationName = !string.IsNullOrEmpty(location.name) ?
                                            location.name : $"Location {count}";

                                        _predefinedTeleports[$"{region.Name} - {locationName}"] = location.transform.position;

                                        // Limit to 5 locations per region to avoid cluttering the list
                                        if (count >= 5) break;
                                    }
                                }
                            }
                        }
                    }
                }

                // Add other players' positions
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
                if (playerList != null && playerList.Count > 0)
                {
                    foreach (var player in playerList)
                    {
                        if (player != null && !IsLocalPlayer(player))
                        {
                            _predefinedTeleports[$"Player: {player.name}"] = player.transform.position;
                        }
                    }
                }

                LoggerInstance.Msg($"Initialized {_predefinedTeleports.Count} predefined teleport locations");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error initializing predefined teleports: {ex.Message}");
            }
        }

        private void DrawZoomControls(Rect mapRect)
        {
            // Zoom level indicator
            GUIStyle zoomStyle = new GUIStyle(GUI.skin.label);
            zoomStyle.normal.textColor = Color.white;
            zoomStyle.alignment = TextAnchor.MiddleCenter;
            zoomStyle.fontSize = 12;

            Rect zoomLabelRect = new Rect(
                mapRect.x + 10,
                mapRect.y + 10,
                100,
                20
            );

            GUI.Label(zoomLabelRect, $"Zoom: {_mapZoom:F1}x", zoomStyle);

            // Zoom buttons
            Rect zoomInRect = new Rect(
                mapRect.x + mapRect.width - 70,
                mapRect.y + 10,
                30,
                20
            );

            Rect zoomOutRect = new Rect(
                mapRect.x + mapRect.width - 35,
                mapRect.y + 10,
                30,
                20
            );

            if (GUI.Button(zoomInRect, "+", _buttonStyle ?? GUI.skin.button))
            {
                _mapZoom = Mathf.Clamp(_mapZoom + 0.1f, 1.0f, _maxZoom);
            }

            // Disable zoom out button if already at default zoom
            GUI.enabled = _mapZoom > 1.0f;
            if (GUI.Button(zoomOutRect, "-", _buttonStyle ?? GUI.skin.button))
            {
                _mapZoom = Mathf.Clamp(_mapZoom - 0.1f, 1.0f, _maxZoom);
            }
            GUI.enabled = true;

            // Reset zoom/pan button
            Rect resetRect = new Rect(
                mapRect.x + mapRect.width - 90,
                mapRect.y + 35,
                80,
                20
            );

            if (GUI.Button(resetRect, "Reset View", _buttonStyle ?? GUI.skin.button))
            {
                _mapZoom = 1.0f;
                _mapPanOffset = Vector2.zero;
            }

            // Help text for controls
            GUIStyle helpStyle = new GUIStyle(GUI.skin.label);
            helpStyle.fontSize = 10;
            helpStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);

            Rect helpRect = new Rect(
                mapRect.x + 10,
                mapRect.y + 35,
                200,
                20
            );

            GUI.Label(helpRect, "Middle-click & drag to pan. Scroll to zoom.", helpStyle);
        }

        private IEnumerator CaptureMapCoroutine()
        {
            yield return new WaitForSeconds(0.5f);

            try
            {
                // Get the map instance
                var mapInstance = Map.Instance;
                if (mapInstance == null)
                {
                    LoggerInstance.Error("Map instance not found!");
                    _isCapturingMap = false;
                    yield break;
                }

                // Try to get map texture from the phone's MapApp
                Texture2D mapTexture = null;

                // Find MapApp instance
                var mapApp = Resources.FindObjectsOfTypeAll<Il2CppScheduleOne.UI.Phone.Map.MapApp>()
                    .FirstOrDefault();

                if (mapApp != null)
                {
                    // Check if any of the map sprites are available
                    if (mapApp.MainMapSprite != null && mapApp.MainMapSprite.texture != null)
                    {

                        mapTexture = CreateReadableTexture(mapApp.MainMapSprite.texture);
                    }
                }
                // If we found a map texture, use it
                if (mapTexture != null)
                {
                    _mapTexture = mapTexture;
                }

                _mapTexture.Apply();
                InitializePredefinedTeleports();
                _mapInitialized = true;

            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error capturing map: {ex.Message}");
                LoggerInstance.Error($"Stack trace: {ex.StackTrace}");
            }

            _isCapturingMap = false;
        }

        private Texture2D CreateReadableTexture(Texture sourceTexture)
        {
            if (sourceTexture == null) return null;

            try
            {
                // Get dimensions
                int width = sourceTexture.width;
                int height = sourceTexture.height;

                // Create render texture
                RenderTexture renderTex = RenderTexture.GetTemporary(
                    width, height, 0, RenderTextureFormat.ARGB32);

                // Copy source texture to render texture
                Graphics.Blit(sourceTexture, renderTex);

                // Create result texture
                Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);

                // Save active render texture
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTex;

                // Read pixels
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                result.Apply();

                // Restore active render texture
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTex);

                return result;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error creating readable texture: {ex.Message}");
                return null;
            }
        }
        #endregion


        #region Register Commands

        private void RegisterCommands()
        {
            _categories.Clear();

            // Initialize dropdown option lists
            _itemCache["explosion_targets"] = new List<string> {
                "custom",
                "all",
                "random",
                "nukeall"
            };

           _itemCache["vehicle_targets"] = new List<string> {
                "shitbox",
                "veeper",
                "bruiser",
                "dinkler",
                "hounddog",
                "cheetah"
            };

            _itemCache["predefined_tele_targets"] = new List<string>
            {
                "motel",
                "sweatshop",
                "barn",
                "bungalow",
                "warehouse",
                "docks",
                "manor",
                "postoffice",
                "dealership",
                "tacoticklers",
                "laundromat",
                "carwash",
                "pawnshop",
                "hardwarestore"
            };

            LoggerInstance.Msg($"Added {_itemCache["explosion_targets"].Count} explosion targets to item cache.");
            LoggerInstance.Msg($"Added {_itemCache["vehicle_targets"].Count} vehicles to item cache.");
            LoggerInstance.Msg($"Added {_itemCache["predefined_tele_targets"].Count} predefined teleport locations.");

            var onlineCategory = new CommandCategory { Name = "Online" };
            var playerCategory = new CommandCategory { Name = "Self" };
            var exploitsCategory = new CommandCategory { Name = "Exploits" };
            var itemsCategory = new CommandCategory { Name = "Item Manager" };
            var worldCategory = new CommandCategory { Name = "World" };
            var teleportCategory = new CommandCategory { Name = "Teleport Manager" };
            var vehicleCategory = new CommandCategory { Name = "Vehicle Manager" };
            var systemCategory = new CommandCategory { Name = "Game" };


            /* Player category */
            playerCategory.Commands.Add(new Command
            {
                Name = "Toggle Godmode",
                Description = "Toggles godmode on/off.",
                Handler = ToggleGodmode
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Toggle Unlimited Ammo",
                Description = "Toggles unlimited ammo on/off.",
                Handler = ToggleUnlimitedAmmo
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Toggle Never Wanted",
                Description = "Toggles never wanted on/off.",
                Handler = ToggleNeverWanted
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Give XP",
                Description = "Gives player XP.",
                Handler = ChangeXP,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Amount",
                        Placeholder = "Amount",
                        Type = ParameterType.Input,
                        Value = "25"
                    }
                }
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Give Cash",
                Description = "Sends the quantity of cash to the player's cash balance, can take negative numbers.",
                Handler = ChangeCash,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Amount",
                        Placeholder = "Amount",
                        Type = ParameterType.Input,
                        Value = "1000"
                    }
                }
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Give Online Balance",
                Description = "Sends the quantity of cash to the player's online balance, can take negative numbers.",
                Handler = ChangeBalance,
                Parameters = new List<CommandParameter>
                {
                    new CommandParameter
                    {
                        Name = "Amount",
                        Placeholder = "Amount",
                        Type = ParameterType.Input,
                        Value = "1000"
                    }
                }
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Toggle Always Visible Crosshair",
                Description = "Forces the crosshair to always remain visible, even when using items that would normally hide it.",
                Handler = ToggleAlwaysVisibleCrosshair
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Raise Wanted Level",
                Description = "Raises your wanted level.",
                Handler = RaiseWantedLevel
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Lower Wanted Level",
                Description = "Lowers your wanted level.",
                Handler = LowerWantedLevel
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Clear Wanted Level",
                Description = "Clears your wanted level.",
                Handler = ClearWantedLevel
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Set Movement Speed",
                Description = "Sets the player's movement speed.",
                Handler = SetPlayerMovementSpeed,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Speed",
                        Placeholder = "Speed",
                        Type = ParameterType.Input,
                        Value = "1"
                    }
                }
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Set Jump Force",
                Description = "Sets the player's jump force.",
                Handler = SetJumpForce,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Force",
                        Placeholder = "Force",
                        Type = ParameterType.Input,
                        Value = "1"
                    }
                }
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Set Stamina Reserve",
                Description = "Sets the player's stamina reserve.",
                Handler = SetPlayerStaminaReserve,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Amount",
                        Placeholder = "Amount",
                        Type = ParameterType.Input,
                        Value = "200"
                    }
                }
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Clear Inventory",
                Description = "Clears the player's inventory",
                Handler = ClearInventory
            });


            /* World category */
            worldCategory.Commands.Add(new Command
            {
                Name = "Free Camera",
                Description = "Toggles free camera mode",
                Handler = ToggleFreeCam
            });
            worldCategory.Commands.Add(new Command
            {
                Name = "Set Time",
                Description = "Sets the time of day (24-hour format)",
                Handler = SetWorldTime,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Time",
                        Placeholder = "HHMM (e.g. 1530)",
                        Type = ParameterType.Input,
                        Value = "1200"
                    }
                }
            });
            worldCategory.Commands.Add(new Command
            {
                Name = "Set Time Scale",
                Description = "Sets game time scale (1.0 = normal)",
                Handler = SetTimeScale,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Scale",
                        Placeholder = "Scale",
                        Type = ParameterType.Input,
                        Value = "1.0"
                    }
                }
            });
            worldCategory.Commands.Add(new Command
            {
                Name = "Grow Plants",
                Description = "Instantly grows all weed plants in the world.",
                Handler = GrowPlants
            });
            worldCategory.Commands.Add(new Command
            {
                Name = "Clear World Trash",
                Description = "Forcefully clears all world trash.",
                Handler = ClearTrash
            });
            worldCategory.Commands.Add(new Command
            {
                Name = "Law Intensity",
                Description = "Sets the law intensity (maximum 10)",
                Handler = SetLawIntensity,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Intensity",
                        Placeholder = "6",
                        Type = ParameterType.Input,
                        Value = "6"
                    }
                }
            });            


            /* System category */
            systemCategory.Commands.Add(new Command
            {
                Name = "Save Game",
                Description = "Forces a game save",
                Handler = ForceGameSave
            });
            systemCategory.Commands.Add(new Command
            {
                Name = "End Tutorial",
                Description = "Forcefully ends the tutorial.",
                Handler = EndTutorial
            });


            /* Exploits Category */
            exploitsCategory.Commands.Add(new Command
            {
                Name = "Create Explosion",
                Description = "Create explosions. Options: 'all' (target all players), 'random' (target random player), or custom location.",
                Handler = CreateExplosion,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "Target",
                        Placeholder = "all/random",
                        Type = ParameterType.Dropdown,
                        ItemCacheKey = "explosion_targets",
                        Value = "custom"
                    },
                    new CommandParameter {
                        Name = "Damage",
                        Placeholder = "Damage",
                        Type = ParameterType.Input,
                        Value = "100"
                    },
                    new CommandParameter {
                        Name = "Radius",
                        Placeholder = "Radius",
                        Type = ParameterType.Input,
                        Value = "10"
                    }
                }
            });
            exploitsCategory.Commands.Add(new Command
            {
                Name = "Kill Player",
                Description = "Kills the specified player by index.",
                Handler = KillPlayerCommand,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "PlayerIndex",
                        Placeholder = "Player Index",
                        Type = ParameterType.Input,
                        Value = "1"
                    }
                }
            });
            exploitsCategory.Commands.Add(new Command
            {
                Name = "Damage Player",
                Description = "Damages the specified player by index.",
                Handler = DamagePlayerCommand,
                Parameters = new List<CommandParameter> {
                    new CommandParameter {
                        Name = "PlayerIndex",
                        Placeholder = "Player Index",
                        Type = ParameterType.Input,
                        Value = "1"
                    },
                    new CommandParameter {
                        Name = "Damage",
                        Placeholder = "Damage Amount",
                        Type = ParameterType.Input,
                        Value = "10"
                    }
                }
            });
            exploitsCategory.Commands.Add(new Command
            {
                Name = "Kill All Players",
                Description = "Kills all players except yourself.",
                Handler = KillAllPlayersCommand
            });

            vehicleCategory.Commands.Add(new Command
            {
                Name = "Spawn Vehicle",
                Description = "Spawns a vehicle of your choosing.",
                Handler = SpawnVehicle,
                Parameters = new List<CommandParameter> {
                    new CommandParameter
                    {
                        Name = "Vehicle",
                        Placeholder = "Select vehicle",
                        Type = ParameterType.Dropdown,
                        ItemCacheKey = "vehicle_targets",
                        Value = "Cheetah"  // Default value
                    }
                }
            });

            // Add categories to list
            _categories.Add(onlineCategory);
            _categories.Add(playerCategory);
            _categories.Add(exploitsCategory);
            _categories.Add(itemsCategory);
            _categories.Add(worldCategory);
            _categories.Add(teleportCategory);
            _categories.Add(vehicleCategory);
            _categories.Add(systemCategory);
        }
        #endregion
    }
    #endregion
}