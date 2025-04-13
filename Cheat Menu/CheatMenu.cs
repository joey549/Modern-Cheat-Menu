﻿using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
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
using static Il2CppSystem.Net.ServicePointManager;
using Il2CppScheduleOne.Persistence;
using System.Text.RegularExpressions;
using UnityEngine.InputSystem;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Combat;
using Il2CppScheduleOne.Equipping;
using Il2CppFishNet.Object;
using Il2CppScheduleOne.AvatarFramework.Equipping;
using UnityEngine.UIElements.Internal;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.PlayerScripts.Health;
//using UniverseLib;
//using UniverseLib.Config;
//using UniverseLib.Input;
//using UniverseLib.Runtime;
//using static UniverseLib.Il2CppReflection;
//using UniverseLib.UI;
using System.Runtime.InteropServices;
using Il2CppInterop.Common;
using Il2CppNewtonsoft.Json;
using Il2CppNewtonsoft.Json.Linq;
using Il2CppNewtonsoft.Json.Converters;
using HarmonyLib;
using Il2CppFishNet.Transporting;
using Il2CppFishySteamworks;
using static Modern_Cheat_Menu.Core;
using Il2CppSteamworks;
using Unity.Collections;
using UnityEngine.Playables;

/*
 * List of functions to add for commands.
 * packageproduct, spawnvehicle, 
 * teleport, setowned, setqueststate, setquestentrystate, setemotion, setunlocked, setrelationship, addemployee
 * setdiscovered,
 */

[assembly: MelonInfo(typeof(Modern_Cheat_Menu.Core), "Modern Cheat Menu", "2.0.0", "darkness", null)]
[assembly: MelonGame("TVGS", "Schedule I")]
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
        private Dictionary<string, List<string>> _itemCache = new Dictionary<string, List<string>>();

        private Dictionary<string, bool> _qualitySupportCache = new Dictionary<string, bool>();
        private Dictionary<string, List<string>> _itemQualityCache = new Dictionary<string, List<string>>();

        private Il2CppFishySteamworks.Server.ServerSocket _discoveredServerSocket;


        // Toggle key for menu
        private const KeyCode ToggleMenuKey = KeyCode.F10;


        // Keybind for explosion at crosshair
        private const KeyCode explosionAtCrosshair = KeyCode.LeftAlt;


        // Add to your Core class
        private Vector2 _playerScrollPosition = Vector2.zero;

        // Player network interaction category
        public class NetworkPlayerCategory
        {
            public string Name { get; set; }
            public List<Command> Commands { get; set; } = new List<Command>();
        }

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

                // Calculate layout for current player (left side)
                float contentY = containerRect.y + 40;
                float leftSideWidth = 300f;

                // Find local player first
                Il2CppScheduleOne.PlayerScripts.Player localPlayer = null;
                PlayerHealth localPlayerHealth = null;

                foreach (var player in playerList)
                {
                    if (player != null && IsLocalPlayer(player))
                    {
                        localPlayer = player;
                        localPlayerHealth = GetPlayerHealth(player);
                        break;
                    }
                }

                // Display local player info
                if (localPlayer != null)
                {
                    // Local player name with YOU tag
                    GUI.Label(
                        new Rect(containerRect.x + 10, contentY, leftSideWidth - 10, 20),
                        $"{localPlayer.name} (YOU)",
                        _commandLabelStyle ?? _labelStyle
                    );

                    // Health info
                    if (localPlayerHealth != null)
                    {
                        GUI.Label(
                            new Rect(containerRect.x + 10, contentY + 20, leftSideWidth - 10, 20),
                            $"Health: {localPlayerHealth.CurrentHealth} / {PlayerHealth.MAX_HEALTH}",
                            _labelStyle
                        );

                        GUI.Label(
                            new Rect(containerRect.x + 10, contentY + 40, leftSideWidth - 10, 20),
                            $"Status: {(localPlayerHealth.IsAlive ? "Alive" : "Dead")}",
                            _labelStyle
                        );
                    }
                }

                // Right side - Kill All and Heal All buttons
                float actionsX = containerRect.x + containerRect.width - 220;

                // Kill All button
                if (GUI.Button(
                    new Rect(actionsX, contentY, 100, 30),
                    "Kill All",
                    _buttonStyle))
                {
                    KillAllPlayersCommand(null);
                }

                // Heal All button
                //if (GUI.Button(
                //    new Rect(actionsX + 110, contentY, 100, 30),
                //    "Heal All",
                //    _buttonStyle))
                //{
                //    HealAllPlayersCommand(null);
                //}

                // Player count (below buttons)
                GUI.Label(
                    new Rect(actionsX, contentY + 40, 220, 20),
                    $"Total Players: {playerList.Count}",
                    _labelStyle
                );
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error drawing horizontal player list: {ex.Message}");
            }
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

        // Free camera settings
        private bool _freeCamEnabled = false;
        private Camera _mainCamera;


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
                LoggerInstance.Msg($"Found player NetworkObject: {_cachedPlayerObject.name}");
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


        // Custom SafeTextField method that uses our custom implementation

        private string SafeCustomTextField(string key, string value, GUIStyle style, params GUILayoutOption[] options)
        {
            try
            {
                if (!_textFields.TryGetValue(key, out var textField))
                {
                    textField = new CustomTextField(value, style);
                    _textFields[key] = textField;
                }
                else
                {
                    textField.Value = value;
                }

                return textField.DrawLayout(options);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"TextField error: {ex.Message}");
                return value;
            }
        }

        public override void OnInitializeMelon()
        {
            try
            {
                // Initialize Harmony
                _harmony = new HarmonyLib.Harmony("com.darkness.modern_cheats.menu");

                // Other initialization code...
                _harmony.PatchAll(typeof(Core).Assembly);

                // Initialize UniverseLib
                //UniverseLib.Universe.Init();

                // Initialize HWID Spoofer
                InitializeHwidPatch();

                // Register commands
                RegisterCommands();

                // Patch lobby
                // ApplyLobbyPatch();

                LoggerInstance.Msg("Modern Cheat Menu successfully initialized.");
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
                // Find main camera first
                _mainCamera = Camera.main;
                if (_mainCamera == null)
                    throw new NullReferenceException("Main camera not found!");

                // Create textures (still safe to do here)
                CreateTextures();

                // Cache game items
                CacheGameItems();

                _isInitialized = true;

                // Show notification
                ShowNotification("Modern Cheat Menu Loaded", "Press F10 to toggle menu visibility", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"UI SETUP FAILED: {ex}");
                _isInitialized = false;
                ShowNotification("Initialization Failed", ex.Message, NotificationType.Error);
            }
        }

        #region Textures and Styles

        // Create fallback textures
        private void CreateFallbackTextures()
        {
            _backgroundTexture = new Texture2D(1, 1);
            _backgroundTexture.SetPixel(0, 0, Color.black);
            _backgroundTexture.Apply();

            _buttonNormalTexture = new Texture2D(1, 1);
            _buttonNormalTexture.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f));
            _buttonNormalTexture.Apply();

            _panelTexture = new Texture2D(1, 1);
            _panelTexture.SetPixel(0, 0, new Color(0.15f, 0.15f, 0.15f));
            _panelTexture.Apply();

            _categoryTabTexture = _buttonNormalTexture;
            _categoryTabActiveTexture = _buttonNormalTexture;
            _buttonHoverTexture = _buttonNormalTexture;
            _buttonActiveTexture = _buttonNormalTexture;
        }

        private void CreateTextures()
        {
            try
            {
                // Create basic fallback textures first
                CreateFallbackTextures();

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
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error creating textures: {ex.Message}");
                // Use fallback textures
                CreateFallbackTextures();
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

        public override void OnUpdate()
        {
            if (!_isInitialized)
                return;

            // Toggle menu visibility
            if (Input.GetKeyDown(ToggleMenuKey))
            {
                ToggleUI(!_uiVisible);
            }

            if (Input.GetKeyDown(explosionAtCrosshair))
            {
                // Get ray from center of screen
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

                CreateServerSideExplosion(explosionPosition, 100f, 10f); // Default damage and radius
            }

            // Only allow free camera when menu is not visible
            if (!_uiVisible && _freeCamEnabled && _mainCamera != null)
            {
                HandleFreeCamMovement();
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
        }

        private void HandleFreeCamMovement()
        {
            if (_mainCamera == null)
            {
                LoggerInstance.Error("Main camera not found in free camera movement!");
                return;
            }

            // Movement speed based on shift key
            float speedMultiplier = Input.GetKey(KeyCode.LeftShift) ? 2f : 1f;

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            float upDown = 0f;

            if (Input.GetKey(KeyCode.Space))
                upDown += 1f;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                upDown -= 1f;

            Vector3 movement = new Vector3(horizontal, upDown, vertical) * 10f * speedMultiplier * Time.deltaTime;
            _mainCamera.transform.Translate(movement, Space.Self);

            // Mouse look only when right mouse button is held
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * 3f;
                float mouseY = Input.GetAxis("Mouse Y") * 3f;

                _mainCamera.transform.Rotate(Vector3.up, mouseX, Space.World);
                _mainCamera.transform.Rotate(Vector3.right, -mouseY, Space.Self);
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

                // Show cursor and unlock
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                // Attempt to pause game or disable player input
                try
                {
                    // Look for a game manager or player controller to pause
                    var pauseManagers = UnityEngine.Object.FindObjectsOfType(Il2CppType.Of<MonoBehaviour>());
                    foreach (var manager in pauseManagers)
                    {
                        MonoBehaviour mb = manager.Cast<MonoBehaviour>();

                        // Check for common pause-related methods
                        var pauseMethod = mb.GetType().GetMethod("Pause");
                        var setPauseMethod = mb.GetType().GetMethod("SetPaused");

                        if (pauseMethod != null)
                        {
                            pauseMethod.Invoke(mb, null);
                            LoggerInstance.Msg($"Paused via {mb.GetType().Name}.Pause()");
                        }
                        else if (setPauseMethod != null)
                        {
                            setPauseMethod.Invoke(mb, new object[] { true });
                            LoggerInstance.Msg($"Paused via {mb.GetType().Name}.SetPaused(true)");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    LoggerInstance.Error($"Error attempting to pause game: {ex.Message}");
                }
            }
            else
            {
                // Hide cursor and lock
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                // Attempt to unpause game
                try
                {
                    var pauseManagers = UnityEngine.Object.FindObjectsOfType(Il2CppType.Of<MonoBehaviour>());
                    foreach (var manager in pauseManagers)
                    {
                        MonoBehaviour mb = manager.Cast<MonoBehaviour>();

                        var unpauseMethod = mb.GetType().GetMethod("Unpause");
                        var setPauseMethod = mb.GetType().GetMethod("SetPaused");

                        if (unpauseMethod != null)
                        {
                            unpauseMethod.Invoke(mb, null);
                            LoggerInstance.Msg($"Unpaused via {mb.GetType().Name}.Unpause()");
                        }
                        else if (setPauseMethod != null)
                        {
                            setPauseMethod.Invoke(mb, new object[] { false });
                            LoggerInstance.Msg($"Unpaused via {mb.GetType().Name}.SetPaused(false)");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    LoggerInstance.Error($"Error attempting to unpause game: {ex.Message}");
                }
            }
        }

        #region OnGUI and UI Drawing
        public override void OnGUI()
        {
            if (!_isInitialized)
                return;

            // Draw notifications even when menu is hidden
            if (_activeNotifications.Count > 0)
            {
                DrawNotifications();
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
            float windowX = Mathf.Lerp(-_windowRect.width, 20, menuAnim);
            _windowRect.x = windowX;

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

        private void DrawWindow(int windowId)
        {
            try
            {
                // Main window vertical group
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                // --- Header Section ---
                GUILayout.BeginHorizontal(_headerStyle);
                try
                {
                    GUILayout.Label("Modern Cheat Menu", _titleStyle, GUILayout.ExpandWidth(true));

                    // Settings button
                    if (GUILayout.Button("⚙", _iconButtonStyle,
                        GUILayout.Width(30), GUILayout.Height(30)))
                    {
                        _showSettings = true;
                    }

                    // Close button
                    if (GUILayout.Button("✕", _iconButtonStyle,
                        GUILayout.Width(30), GUILayout.Height(30)))
                    {
                        ToggleUI(false);
                    }
                }
                finally
                {
                    GUILayout.EndHorizontal();
                }

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
            if (category.Name == "Item Manager")
            {
                DrawItemManager();
            }
            else if (category.Name == "Online")
            {
                DrawOnlinePlayers();
            }
            else
            {
                DrawCommandCategory(category);
            }
        }

        private void DrawOnlinePlayers()
        {
            // Check for refresh time
            if (Time.time - _lastPlayerRefreshTime > PLAYER_REFRESH_INTERVAL)
            {
                RefreshOnlinePlayers();
                _lastPlayerRefreshTime = Time.time;
            }

            // Refresh button
            if (GUILayout.Button("Refresh", _buttonStyle, GUILayout.Width(100), GUILayout.Height(30)))
            {
                RefreshOnlinePlayers();
                _lastPlayerRefreshTime = Time.time;
                ShowNotification("Online", "Player list refreshed", NotificationType.Info);
            }

            GUILayout.Space(10);

            // Begin scrollview for player list
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            if (_onlinePlayers.Count == 0)
            {
                GUILayout.Label("No players found. Try refreshing the list.", _labelStyle);
            }
            else
            {
                foreach (var playerInfo in _onlinePlayers)
                {
                    if (playerInfo == null || playerInfo.Player == null)
                        continue;

                    // Player entry box
                    GUILayout.BeginVertical(_panelStyle);

                    // Player name
                    string localTag = playerInfo.IsLocal ? " (YOU)" : "";
                    GUILayout.Label($"{playerInfo.Name}{localTag}", _commandLabelStyle ?? _labelStyle);

                    // Health
                    string healthStatus = playerInfo.Health != null ?
                        $"Health: {playerInfo.Health.CurrentHealth}/{PlayerHealth.MAX_HEALTH} - " : "";
                    string aliveStatus = playerInfo.Health != null ?
                        (playerInfo.Health.IsAlive ? "Alive" : "Dead") : "Status: Unknown";
                    GUILayout.Label($"{healthStatus}{aliveStatus}", _labelStyle);

                    // Network info
                    GUILayout.Label($"Steam ID: {playerInfo.SteamID}", _labelStyle);
                    GUILayout.Label($"IP: {playerInfo.ClientAddress}", _labelStyle);

                    // Actions for remote players
                    if (!playerInfo.IsLocal)
                    {
                        GUILayout.BeginHorizontal();

                        if (GUILayout.Button("Kill", _buttonStyle))
                        {
                            ServerExecuteKillPlayer(playerInfo.Player);
                            ShowNotification("Player", $"Killed {playerInfo.Name}", NotificationType.Success);
                        }

                        if (GUILayout.Button("Heal", _buttonStyle))
                        {
                            ServerExecuteRevivePlayer(playerInfo.Player);
                            ShowNotification("Player", $"Revived {playerInfo.Name}", NotificationType.Success);
                        }

                        if (GUILayout.Button("Explode", _buttonStyle))
                        {
                            CreateServerSideExplosion(playerInfo.Player.transform.position, 100f, 5f);
                            ShowNotification("Player", $"Exploded {playerInfo.Name}", NotificationType.Success);
                        }

                        if (GUILayout.Button("Damage", _buttonStyle))
                        {
                            ServerExecuteDamagePlayer(playerInfo.Player, 10f);
                            ShowNotification("Player", $"Damaged {playerInfo.Name}", NotificationType.Success);
                        }

                        bool newState = GUILayout.Toggle(playerInfo.ExplodeLoop, "Loop");
                        if (newState != playerInfo.ExplodeLoop)
                        {
                            playerInfo.ExplodeLoop = newState;
                            if (newState)
                            {
                                StartExplodeLoop(playerInfo);
                                ShowNotification("Player", $"Started explosion loop on {playerInfo.Name}", NotificationType.Warning);
                            }
                        }

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.EndVertical();
                    GUILayout.Space(5);
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Global actions
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Kill All", _buttonStyle))
            {
                KillAllPlayersCommand(null);
            }

            //if (GUILayout.Button("Heal All", _buttonStyle))
            //{
            //    HealAllPlayersCommand(null);
            //}

            if (GUILayout.Button("Explode All", _buttonStyle))
            {
                CreateExplosion(new string[] { "all", "100", "5" });
            }
            GUILayout.EndHorizontal();
        }

        private void HandleWindowDragging()
        {
            // Make the drag area just the header section
            Rect dragRect = new Rect(0, 0, _windowRect.width, 40);

            // Check for mousedown event inside the drag area
            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                dragRect.Contains(Event.current.mousePosition))
            {
                _isDragging = true;
                _dragOffset = new Vector2(
                    Event.current.mousePosition.x - _windowRect.x,
                    Event.current.mousePosition.y - _windowRect.y
                );
                Event.current.Use(); // Prevent this event from being processed further
            }
            // Handle dragging movement
            else if (_isDragging && Event.current.type == EventType.MouseDrag)
            {
                _windowRect.x = Event.current.mousePosition.x - _dragOffset.x;
                _windowRect.y = Event.current.mousePosition.y - _dragOffset.y;

                // Keep window on screen
                _windowRect.x = Mathf.Clamp(_windowRect.x, -_windowRect.width + 100, Screen.width - 100);
                _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - 50);

                Event.current.Use(); // Prevent this event from being processed further
            }
            // Handle end of dragging
            else if (Event.current.type == EventType.MouseUp && _isDragging)
            {
                _isDragging = false;
                Event.current.Use(); // Prevent this event from being processed further
            }
        }

        private Dictionary<int, string> _playerIPCache = new Dictionary<int, string>();

        private void InitializeNetworkHooks()
        {
            try
            {
                LoggerInstance.Msg("Setting up network hooks...");

                // Hook the method that processes incoming connections
                var connectionMethod = typeof(Il2CppFishySteamworks.Server.ServerSocket).GetMethod("OnRemoteConnectionState",
                    BindingFlags.Public | BindingFlags.Instance);

                if (connectionMethod != null)
                {
                    var postfix = typeof(Core).GetMethod("OnConnectionStateChangedPostfix",
                        BindingFlags.Static | BindingFlags.NonPublic);

                    _harmony.Patch(connectionMethod, postfix: new HarmonyLib.HarmonyMethod(postfix));
                    LoggerInstance.Msg("Successfully hooked OnRemoteConnectionState");
                }
                else
                {
                    LoggerInstance.Error("Failed to find OnRemoteConnectionState method");
                }

                // Also hook GetConnectionAddress to ensure we can see what addresses are being used
                var addressMethod = typeof(Il2CppFishySteamworks.Server.ServerSocket).GetMethod("GetConnectionAddress",
                    BindingFlags.Public | BindingFlags.Instance);

                if (addressMethod != null)
                {
                    var postfix = typeof(Core).GetMethod("GetConnectionAddressPostfix",
                        BindingFlags.Static | BindingFlags.NonPublic);

                    _harmony.Patch(addressMethod, postfix: new HarmonyLib.HarmonyMethod(postfix));
                    LoggerInstance.Msg("Successfully hooked GetConnectionAddress");
                }
                else
                {
                    LoggerInstance.Error("Failed to find GetConnectionAddress method");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error setting up network hooks: {ex}");
            }
        }

        // Add these static methods to receive the hooked information
        private static void OnConnectionStateChangedPostfix(Il2CppFishySteamworks.Server.ServerSocket __instance,
            SteamNetConnectionStatusChangedCallback_t args)
        {
            try
            {
                // Extract connection info
                int connectionId = 0;
                string address = "Unknown";

                // Get connection ID using the actual available methods
                if (__instance._steamConnections != null)
                {
                    // Use the First dictionary directly (HSteamNetConnection -> int)
                    var connections = __instance._steamConnections.First;
                    if (connections != null)
                    {
                        // Check if dictionary contains key and get value
                        int value;
                        if (connections.TryGetValue(args.m_hConn, out value))
                        {
                            connectionId = value;
                            MelonLogger.Msg($"Found connection ID: {connectionId} for handle: {args.m_hConn}");
                        }
                        else
                        {
                            MelonLogger.Error($"Connection handle {args.m_hConn} not found in dictionary");
                        }
                    }
                }

                // Extract IP address information from connection info
                try
                {
                    // Use ToString method for address if available
                    var addrRemote = args.m_info.m_addrRemote;
                    if (addrRemote != null)
                    {
                        // Check if valid first
                        if (addrRemote.GetType().GetMethod("IsValid") != null)
                        {
                            var isValidMethod = addrRemote.GetType().GetMethod("IsValid");
                            bool isValid = (bool)isValidMethod.Invoke(addrRemote, null);

                            if (isValid)
                            {
                                // Try ToString()
                                var toStringMethod = addrRemote.GetType().GetMethod("ToString",
                                    BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);

                                if (toStringMethod != null)
                                {
                                    var result = toStringMethod.Invoke(addrRemote, null);
                                    if (result != null)
                                    {
                                        address = result.ToString();
                                        MelonLogger.Msg($"Extracted address: {address}");
                                    }
                                }
                            }
                        }

                        // If we still don't have the address, try direct field access
                        if (address == "Unknown")
                        {
                            // Try to get IPv4 address which is stored in first 4 bytes of m_ipv6
                            var ipv6Field = addrRemote.GetType().GetField("m_ipv6",
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                            var portField = addrRemote.GetType().GetField("m_port",
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

                            if (ipv6Field != null && portField != null)
                            {
                                // Attempt to get the byte array and port
                                var ipBytes = ipv6Field.GetValue(addrRemote) as byte[];
                                ushort port = 0;

                                if (portField.GetValue(addrRemote) != null)
                                {
                                    port = (ushort)portField.GetValue(addrRemote);
                                }

                                if (ipBytes != null && ipBytes.Length >= 4)
                                {
                                    // Format as IPv4 address (first 4 bytes contain the IPv4 address)
                                    address = $"{ipBytes[3]}.{ipBytes[2]}.{ipBytes[1]}.{ipBytes[0]}:{port}";
                                    MelonLogger.Msg($"Extracted IPv4 address: {address}");
                                }
                            }
                        }
                    }
                }
                catch (Exception addrEx)
                {
                    MelonLogger.Error($"Address extraction error: {addrEx.Message}");
                }

                // Get connection state
                string stateStr = args.m_info.m_eState.ToString();
                MelonLogger.Msg($"Connection state changed - ID: {connectionId}, Address: {address}, State: {stateStr}");

                // Cache the connection
                if (connectionId != 0 && address != "Unknown")
                {
                    Instance._playerIPCache[connectionId] = address;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in connection hook: {ex.Message}");
            }
        }


        private static void GetConnectionAddressPostfix(Il2CppFishySteamworks.Server.ServerSocket __instance,
            int connectionId, ref string __result)
        {
            MelonLogger.Msg($"GetConnectionAddress called for ID {connectionId}, returned: {__result}");

            // Cache the result for our player list
            if (!string.IsNullOrEmpty(__result) && __result != "unknown")
            {
                Instance._playerIPCache[connectionId] = __result;
            }
        }

        // Make the Core class instance accessible for the static methods
        private static Core Instance;


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

                LoggerInstance.Msg($"Total players in list: {playerList.Count}");

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
                                                    LoggerInstance.Msg($"Mapped Connection ID {connId} to Steam ID {steamId}");
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

                        // Optional: Dump detailed network information for debugging
                        DumpNetworkInformation(player, netObj);
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

                    LoggerInstance.Msg($"Processed Player:");
                    LoggerInstance.Msg($"Name: {playerInfo.Name}");
                    LoggerInstance.Msg($"Steam ID: {steamId}");
                    LoggerInstance.Msg($"Network Info: {networkInfo}");
                    LoggerInstance.Msg($"IP Address: {ipAddress}");
                    LoggerInstance.Msg($"Is Local: {isLocal}");
                }

                LoggerInstance.Msg($"Refreshed online players. Found {_onlinePlayers.Count} players.");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error refreshing online players: {ex.Message}");
            }
        }

        private void DumpNetworkInformation(Il2CppScheduleOne.PlayerScripts.Player player, NetworkObject netObj)
        {
            try
            {
                LoggerInstance.Msg($"============= DETAILED NETWORK INFO DUMP FOR {player.name} =============");

                // First dump general NetworkObject info
                if (netObj != null)
                {
                    LoggerInstance.Msg($"NetworkObject Details:");
                    LoggerInstance.Msg($"  IsSpawned: {netObj.IsSpawned}");
                    LoggerInstance.Msg($"  IsOwner: {netObj.IsOwner}");
                    LoggerInstance.Msg($"  IsClient: {netObj.IsClient}");
                    LoggerInstance.Msg($"  IsServer: {netObj.IsServer}");
                    LoggerInstance.Msg($"  IsHost: {netObj.IsHost}");
                    LoggerInstance.Msg($"  OwnerId: {netObj.OwnerId}");

                    // Get FishySteamworks transport instance
                    var fishyTransports = Resources.FindObjectsOfTypeAll<Il2CppFishySteamworks.FishySteamworks>();
                    if (fishyTransports != null && fishyTransports.Length > 0)
                    {
                        var transport = fishyTransports[0];
                        LoggerInstance.Msg($"Transport found: {transport != null}");

                        // Dump transport info
                        LoggerInstance.Msg($"Transport Details:");
                        LoggerInstance.Msg($"  LocalUserSteamID: {transport.LocalUserSteamID}");
                        LoggerInstance.Msg($"  Port: {transport._port}");
                        LoggerInstance.Msg($"  Server bind address: {transport._serverBindAddress}");
                        LoggerInstance.Msg($"  Client address: {transport._clientAddress}");
                        LoggerInstance.Msg($"  Maximum clients: {transport._maximumClients}");
                        LoggerInstance.Msg($"  Peer to peer: {transport._peerToPeer}");

                        // Get connection address through transport
                        string address = "Unknown";
                        try
                        {
                            int connId = (int)netObj.OwnerId;
                            address = transport.GetConnectionAddress(connId);
                            LoggerInstance.Msg($"  Connection address for ID {connId}: {address}");
                        }
                        catch (Exception addrEx)
                        {
                            LoggerInstance.Error($"  Error getting connection address: {addrEx.Message}");
                        }

                        // Dump server socket details
                        var serverSocket = transport._server;
                        if (serverSocket != null)
                        {
                            LoggerInstance.Msg($"Server Socket Details:");
                            LoggerInstance.Msg($"  Maximum clients: {serverSocket._maximumClients}");
                            LoggerInstance.Msg($"  Next connection ID: {serverSocket._nextConnectionId}");
                            LoggerInstance.Msg($"  Client host started: {serverSocket._clientHostStarted}");

                            // Try getting all connections
                            if (serverSocket._steamConnections != null)
                            {
                                var fwdDict = serverSocket._steamConnections.First;
                                var bwdDict = serverSocket._steamConnections.Second;

                                LoggerInstance.Msg($"  Connections: {serverSocket._steamConnections.Count}");

                                // Try to enumerate all connections
                                if (fwdDict != null)
                                {
                                    LoggerInstance.Msg($"  Connection Dictionary:");
                                    try
                                    {
                                        // Use enumeration through reflection since the dictionary is Il2Cpp type
                                        var getEnumerator = fwdDict.GetType().GetMethod("GetEnumerator");
                                        if (getEnumerator != null)
                                        {
                                            var enumerator = getEnumerator.Invoke(fwdDict, null);
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
                                                                LoggerInstance.Msg($"    Connection: {key} -> {value}");

                                                                // Try to get address for this connection
                                                                try
                                                                {
                                                                    int connId = (int)value;
                                                                    string connAddress = serverSocket.GetConnectionAddress(connId);
                                                                    LoggerInstance.Msg($"    Address for ID {connId}: {connAddress}");
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    LoggerInstance.Error($"    Error getting address: {ex.Message}");
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
                                        LoggerInstance.Error($"  Error enumerating connections: {ex.Message}");
                                    }
                                }

                                // Try to get steam IDs
                                if (serverSocket._steamIds != null)
                                {
                                    LoggerInstance.Msg($"  Steam IDs Dictionary:");
                                    try
                                    {
                                        // Use enumeration through reflection
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
                                                                    LoggerInstance.Msg($"    Steam ID: {key} -> ConnectionId: {value}");
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
                                        LoggerInstance.Error($"  Error enumerating Steam IDs: {ex.Message}");
                                    }
                                }
                            }
                        }

                        // Try client socket too
                        var clientSocket = transport._client;
                        if (clientSocket != null)
                        {
                            LoggerInstance.Msg($"Client Socket Details:");
                            LoggerInstance.Msg($"  Host Steam ID: {clientSocket._hostSteamID}");
                            LoggerInstance.Msg($"  Connection socket: {clientSocket._socket}");
                            LoggerInstance.Msg($"  Connection state: {clientSocket._connectionState}");
                            LoggerInstance.Msg($"  Connect timeout: {clientSocket._connectTimeout}");
                        }
                    }
                }

                LoggerInstance.Msg($"=================================================================");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error dumping network info: {ex.Message}");
            }
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
                float verticalOffset = 50f;

                // Search Bar
                Rect searchRect = new Rect(20f, 50f + verticalOffset, windowWidth - 40f, 30f);
                GUI.Label(new Rect(searchRect.x, searchRect.y - 25f, 100f, 25f), "Search:", _labelStyle);

                // Use custom text field for search
                if (!_textFields.TryGetValue("itemSearch", out var searchField))
                {
                    searchField = new CustomTextField(_itemSearchText, _searchBoxStyle ?? GUI.skin.textField);
                    _textFields["itemSearch"] = searchField;
                }
                _itemSearchText = searchField.Draw(searchRect);

                // Item Grid Scroll View
                Rect scrollViewRect = new Rect(20f, 100f + verticalOffset, windowWidth - 40f, windowHeight - 250f);

                // Calculate dynamic content height based on filtered items
                if (!_itemCache.TryGetValue("items", out var allItems)) return;
                if (!_itemCache.TryGetValue("qualities", out var qualities)) return;

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

                        // Add Set Quality Button
                        Rect setQualityRect = new Rect(
                            detailsRect.x + detailsRect.width - 130f,
                            detailsRect.y + 70f,
                            120f,
                            25f
                        );

                        if (GUI.Button(setQualityRect, "Set Quality", _buttonStyle))
                        {
                            // Convert selected quality index to enum value
                            var quality = (Il2CppScheduleOne.ItemFramework.EQuality)_selectedQualityIndex;
                            SetItemQuality(quality);
                        }
                    }

                    // Spawn Button - FIXED ENUM CONVERSION
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
            // Header with title and close button
            GUILayout.BeginHorizontal(_headerStyle);
            GUILayout.Label("Settings", _titleStyle, GUILayout.ExpandWidth(true));

            // Close button
            if (GUILayout.Button("✕", _iconButtonStyle, GUILayout.Width(30), GUILayout.Height(30)))
            {
                _showSettings = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Settings content
            GUILayout.BeginVertical(_panelStyle);

            GUILayout.Label("Visual Settings", _subHeaderStyle ?? _labelStyle);

            // UI Scale slider
            GUILayout.BeginHorizontal();
            GUILayout.Label("UI Scale:", GUILayout.Width(120));
            _uiScale = GUILayout.HorizontalSlider(_uiScale, 0.7f, 1.5f, _sliderStyle ?? GUI.skin.horizontalSlider,
                                                 _sliderThumbStyle ?? GUI.skin.horizontalSliderThumb, GUILayout.Width(200));
            GUILayout.Label($"{_uiScale:F2}x", GUILayout.Width(50));
            GUILayout.EndHorizontal();

            // UI Opacity slider
            GUILayout.BeginHorizontal();
            GUILayout.Label("UI Opacity:", GUILayout.Width(120));
            _uiOpacity = GUILayout.HorizontalSlider(_uiOpacity, 0.5f, 1.0f, _sliderStyle ?? GUI.skin.horizontalSlider,
                                                   _sliderThumbStyle ?? GUI.skin.horizontalSliderThumb, GUILayout.Width(200));
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

            GUILayout.Space(15);

            // About section
            GUILayout.Label("About", _subHeaderStyle ?? _labelStyle);
            GUILayout.Label("Modern Cheat Menu v2.0.0");
            GUILayout.Label("by darkness");

            // Reset button
            GUILayout.Space(20);
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

            GUILayout.EndVertical();
        }

        private void ShowDropdownMenu(CommandParameter param)
        {
            // Explicit logging
            Debug.Log($"ShowDropdownMenu CALLED: ItemCacheKey = {param?.ItemCacheKey ?? "NULL"}");

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
            Debug.Log($"Dropdown items: {string.Join(", ", items)}");

            // Default to first item if none selected
            if (string.IsNullOrEmpty(param.Value) && items.Count > 0)
                param.Value = items[0];

            // Get current index
            int currentIndex = items.IndexOf(param.Value);
            Debug.Log($"Current value: {param.Value}, Current index: {currentIndex}");

            // Cycle to the next value, wrapping around
            int nextIndex = (currentIndex + 1) % items.Count;
            param.Value = items[nextIndex];

            Debug.Log($"New selected value: {param.Value}");
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


        //private string GetItemIDFromVisual(string visualName)
        //{
        //    // Clean up visual name - remove any remaining special characters
        //    visualName = visualName.Replace("Visual", "").Replace("_", "").Replace("(Clone)", "").ToLower();

        //    // First, try exact matching
        //    foreach (var entry in _itemDictionary)
        //    {
        //        string key = entry.Key.ToLower().Replace("_", "");
        //        string value = entry.Value.ToLower();

        //        // Exact match
        //        if (key == visualName || value == visualName)
        //        {
        //            return entry.Value;
        //        }
        //    }

        //    // If no exact match, try more flexible matching
        //    foreach (var entry in _itemDictionary)
        //    {
        //        string key = entry.Key.ToLower().Replace("_", "");
        //        string value = entry.Value.ToLower();

        //        // Contains match
        //        if (key.Contains(visualName) || value.Contains(visualName) ||
        //            visualName.Contains(key) || visualName.Contains(value))
        //        {
        //            LoggerInstance.Msg($"Flexible match: {visualName} => {entry.Key} (ID: {entry.Value})");
        //            return entry.Value;
        //        }
        //    }

        //    // If all else fails, log all potential matches
        //    LoggerInstance.Error($"No match found for visual name: {visualName}");
        //    LoggerInstance.Msg("Available items:");
        //    foreach (var entry in _itemDictionary)
        //    {
        //        LoggerInstance.Msg($"{entry.Key} (ID: {entry.Value})");
        //    }

        //    return visualName; // Fallback to the input
        //}

        // Comprehensive method to parse hotbar items
        //private List<HotbarItemInfo> ParseHotbarItems()
        //{
        //    List<HotbarItemInfo> hotbarItems = new List<HotbarItemInfo>();

        //    try
        //    {
        //        LoggerInstance.Msg("Parsing hotbar items...");

        //        // Find UI path
        //        GameObject uiRoot = GameObject.Find("UI");
        //        if (uiRoot == null)
        //        {
        //            LoggerInstance.Error("UI root not found!");
        //            return hotbarItems;
        //        }

        //        Transform slotsContainer = uiRoot.transform.Find("HUD/HotbarContainer/Slots");
        //        if (slotsContainer == null)
        //        {
        //            LoggerInstance.Error("Hotbar slots container not found!");
        //            return hotbarItems;
        //        }

        //        // Iterate through each slot
        //        for (int i = 0; i < slotsContainer.childCount; i++)
        //        {
        //            Transform slotTransform = slotsContainer.GetChild(i);
        //            GameObject slotObj = slotTransform.gameObject;

        //            // Check if this is a valid hotbar slot
        //            if (slotObj.name.Contains("HotbarSlotUI"))
        //            {
        //                int slotIndex = i + 1;
        //                Transform itemContainer = slotTransform.Find("ItemContainer");

        //                if (itemContainer != null && itemContainer.childCount > 0)
        //                {
        //                    // Get the item UI element
        //                    Transform itemUITransform = itemContainer.GetChild(0);
        //                    GameObject itemUIObj = itemUITransform.gameObject;

        //                    // Create item info
        //                    HotbarItemInfo itemInfo = new HotbarItemInfo
        //                    {
        //                        SlotIndex = slotIndex,
        //                        ItemUIType = itemUIObj.name
        //                    };

        //                    // Try to find icon
        //                    Transform iconTransform = itemUITransform.Find("Icon");
        //                    if (iconTransform != null)
        //                    {
        //                        UnityEngine.UI.Image iconImage = iconTransform.GetComponent<UnityEngine.UI.Image>();
        //                        if (iconImage != null && iconImage.sprite != null)
        //                        {
        //                            string spriteName = iconImage.sprite.name;
        //                            itemInfo.IconName = spriteName;

        //                            // Extract visual name from icon name
        //                            if (spriteName.EndsWith("_Icon"))
        //                            {
        //                                itemInfo.VisualName = spriteName.Substring(0, spriteName.Length - 5);

        //                                // Clean up any "(Clone)" suffix
        //                                if (itemInfo.VisualName.Contains("(Clone)"))
        //                                {
        //                                    itemInfo.VisualName = itemInfo.VisualName.Replace("(Clone)", "");
        //                                }

        //                                // Now try to match with actual item ID
        //                                itemInfo.ItemID = GetItemIDFromVisual(itemInfo.VisualName);
        //                            }
        //                        }
        //                    }

        //                    // Try to find quantity
        //                    if (itemUIObj.name.Contains("Integer"))
        //                    {
        //                        Transform valueTransform = itemUITransform.Find("Value");
        //                        if (valueTransform != null)
        //                        {
        //                            UnityEngine.UI.Text valueText = valueTransform.GetComponent<UnityEngine.UI.Text>();
        //                            if (valueText != null && !string.IsNullOrEmpty(valueText.text))
        //                            {
        //                                if (int.TryParse(valueText.text, out int quantity))
        //                                {
        //                                    itemInfo.Quantity = quantity;
        //                                }
        //                            }
        //                        }
        //                    }

        //                    // Try to find quality
        //                    Transform qualityTransform = itemUITransform.Find("Quality");
        //                    if (qualityTransform != null)
        //                    {
        //                        UnityEngine.UI.Text qualityText = qualityTransform.GetComponent<UnityEngine.UI.Text>();
        //                        if (qualityText != null && !string.IsNullOrEmpty(qualityText.text))
        //                        {
                                    
        //                        }
        //                    }

        //                    // Try to find type icon (for clothing items)
        //                    if (itemUIObj.name.Contains("Clothing"))
        //                    {
        //                        Transform typeIconTransform = itemUITransform.Find("TypeIcon");
        //                        if (typeIconTransform != null)
        //                        {
        //                            UnityEngine.UI.Image typeIconImage = typeIconTransform.GetComponent<UnityEngine.UI.Image>();
        //                            if (typeIconImage != null && typeIconImage.sprite != null)
        //                            {
        //                                itemInfo.ItemType = typeIconImage.sprite.name;
        //                            }
        //                        }
        //                    }

        //                    hotbarItems.Add(itemInfo);

        //                    // Detailed logging
        //                    LoggerInstance.Msg($"Slot {slotIndex}: {itemInfo.VisualName}" +
        //                                       (itemInfo.ItemID != null ? $" (ItemID: {itemInfo.ItemID})" : " (No matching ItemID found)") +
        //                                       (itemInfo.Quantity > 1 ? $" x{itemInfo.Quantity}" : "") +
        //                                       (itemInfo.Quality != null ? $" (Quality: {itemInfo.Quality})" : ""));
        //                }
        //                else
        //                {
        //                    LoggerInstance.Msg($"Slot {slotIndex}: Empty");
        //                }
        //            }
        //        }

        //        LoggerInstance.Msg($"Found {hotbarItems.Count} items in hotbar");
        //    }
        //    catch (Exception ex)
        //    {
        //        LoggerInstance.Error($"Error parsing hotbar items: {ex.Message}\n{ex.StackTrace}");
        //    }

        //    return hotbarItems;
        //}


        // Helper class to store hotbar item information
        public class HotbarItemInfo
        {
            public int SlotIndex { get; set; }
            public string ItemUIType { get; set; }
            public string IconName { get; set; }
            public string VisualName { get; set; }
            public string ItemID { get; set; }
            public string Quality { get; set; }
            public int Quantity { get; set; }
            public string ItemType { get; set; }
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

        private void InitializeHwidPatch()
        {
            try
            {
                LoggerInstance.Msg("Initializing HWID Patch...");

                // Create preferences for HWID
                var hwidEntry = MelonPreferences.CreateEntry("CheatMenu", "HWID", "", is_hidden: true);
                var newId = hwidEntry.Value;

                // Generate a new HWID if needed
                if (string.IsNullOrEmpty(newId) || newId.Length != SystemInfo.deviceUniqueIdentifier.Length)
                {
                    var random = new System.Random(Environment.TickCount);
                    var bytes = new byte[SystemInfo.deviceUniqueIdentifier.Length / 2];
                    random.NextBytes(bytes);
                    newId = string.Join("", bytes.Select(it => it.ToString("x2")));
                    LoggerInstance.Msg("Generated and saved a new HWID");
                    hwidEntry.Value = newId;
                }

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
                            LoggerInstance.Msg($"Found local player via IsLocalPlayer flag: {player.name}");
                            return player;
                        }
                    }

                    // Second check: Try to find player with name matching device name
                    foreach (var player in playerList)
                    {
                        if (player != null && player.name.Contains(SystemInfo.deviceName))
                        {
                            LoggerInstance.Msg($"Found local player via device name: {player.name}");
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
                                LoggerInstance.Msg($"Found local player via IsOwner flag: {player.name}");
                                return player;
                            }

                            // Check for NetworkObject and IsOwner
                            var netObject = player.GetComponent<Il2CppFishNet.Object.NetworkObject>();
                            if (netObject != null && netObject.IsOwner)
                            {
                                LoggerInstance.Msg($"Found local player via NetworkObject IsOwner flag: {player.name}");
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
                                            LoggerInstance.Msg($"Found local player via property {prop.Name}: {player.name}");
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
                        LoggerInstance.Msg($"Assuming single player is local player: {playerList[0].name}");
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

        // Player manipulation command handlers
        private void ListPlayersCommand(string[] args)
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

                LoggerInstance.Msg("=== Players in game ===");
                for (int i = 0; i < playerList.Count; i++)
                {
                    var player = playerList[i];
                    string isLocal = IsLocalPlayer(player) ? " (YOU)" : "";
                    LoggerInstance.Msg($"{i + 1}. {player.name}{isLocal}");
                }

                ShowNotification("Players", $"Found {playerList.Count} players", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error listing players: {ex.Message}");
                ShowNotification("Error", "Failed to list players", NotificationType.Error);
            }
        }

        private void ServerExecuteDamagePlayer(Il2CppScheduleOne.PlayerScripts.Player targetPlayer, float damageAmount)
        {
            try
            {
                LoggerInstance.Msg($"Attempting to damage player {targetPlayer.name} via server...");

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
                    LoggerInstance.Msg($"Server damage request sent for player {targetPlayer.name}");
                }
                catch (Exception e)
                {
                    LoggerInstance.Error($"Failed using RpcWriter: {e.Message}");

                    // Fallback to direct method
                    try
                    {
                        playerHealth.TakeDamage(damageAmount, true, true);
                        LoggerInstance.Msg("Fallback to direct TakeDamage method");
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

                LoggerInstance.Msg($"Killed player: {targetPlayer.name}");
                ShowNotification("Player", $"Killed {targetPlayer.name}", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error killing player: {ex.Message}");
                ShowNotification("Error", "Failed to kill player", NotificationType.Error);
            }
        }

        private void ServerExecuteRevivePlayer(Il2CppScheduleOne.PlayerScripts.Player targetPlayer)
        {
            try
            {
                LoggerInstance.Msg($"Attempting to revive player {targetPlayer.name} via server...");

                var playerHealth = GetPlayerHealth(targetPlayer);
                if (playerHealth == null)
                {
                    LoggerInstance.Error("PlayerHealth not found on player!");
                    ShowNotification("Error", "Player health component not found", NotificationType.Error);
                    return;
                }

                Vector3 position = targetPlayer.transform.position;
                Quaternion rotation = targetPlayer.transform.rotation;

                // Use the Server SendRevive method for remote players
                try
                {
                    playerHealth.RpcWriter___Server_SendRevive_3848837105(position, rotation);
                    LoggerInstance.Msg($"Revived player {targetPlayer.name} via Server SendRevive");
                    ShowNotification("Player", $"Revived {targetPlayer.name}", NotificationType.Success);
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error($"Server SendRevive failed: {ex.Message}");
                    ShowNotification("Error", "Failed to revive player via server method", NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in ServerExecuteRevivePlayer: {ex.Message}");
                ShowNotification("Error", "Failed to revive player", NotificationType.Error);
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

                    LoggerInstance.Msg($"Attempting to damage player: {player.name} (index: {playerIndex}, damage: {damage})");

                    // Check if it's the local player
                    if (IsLocalPlayer(player))
                    {
                        // For local player, we can use the direct method
                        var playerHealth = GetPlayerHealth(player);
                        if (playerHealth != null)
                        {
                            playerHealth.TakeDamage(damage, true, true);
                            LoggerInstance.Msg($"Damaged local player for {damage} damage");
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

                    LoggerInstance.Msg($"Attempting to kill player: {player.name} (index: {playerIndex})");

                    //ServerExecuteKillPlayer(player);
                    ServerExecuteDamagePlayer(player, 9999999999999f);
                    ShowNotification("Player", $"Sent kill request for {player.name}", NotificationType.Success);

                    var playerHealth = GetPlayerHealth(player);
                    if (playerHealth != null)
                    {
                        playerHealth.Die();
                        LoggerInstance.Msg("Killed local player");
                        ShowNotification("Player", "Killed local player", NotificationType.Success);
                    }

                    // Check if it's the local player
                    //if (IsLocalPlayer(player))
                    //{
                    //    // For local player, we can use the direct method
                    //    var playerHealth = GetPlayerHealth(player);
                    //    if (playerHealth != null)
                    //    {
                    //        playerHealth.Die();
                    //        LoggerInstance.Msg("Killed local player");
                    //        ShowNotification("Player", "Killed local player", NotificationType.Success);
                    //    }
                    //    else
                    //    {
                    //        LoggerInstance.Error("Local player health component not found!");
                    //        ShowNotification("Error", "Health component not found", NotificationType.Error);
                    //    }
                    //}
                    //else
                    //{
                    //    // For other players, use the server method
                    //    ServerExecuteKillPlayer(player);
                    //    ShowNotification("Player", $"Sent kill request for {player.name}", NotificationType.Success);
                    //}
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

                LoggerInstance.Msg("Attempting to kill all players except local player");

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
                    ServerExecuteDamagePlayer(player, 9999999999999f);
                    killedCount++;
                }

                LoggerInstance.Msg($"Kill requests sent for {killedCount} players");
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

        private void SetItemQualityCommand(string[] args)
        {
            try
            {
                // Default to Heavenly if no argument or invalid argument
                Il2CppScheduleOne.ItemFramework.EQuality quality = Il2CppScheduleOne.ItemFramework.EQuality.Heavenly;

                // If an argument is provided, try to parse it
                if (args.Length > 0)
                {
                    // Try parsing as an integer first
                    if (int.TryParse(args[0], out int qualityValue))
                    {
                        // Ensure the value is within the valid range (0-4)
                        qualityValue = Mathf.Clamp(qualityValue, 0, 4);
                        quality = (Il2CppScheduleOne.ItemFramework.EQuality)qualityValue;
                    }
                    // Then try parsing as an enum string
                    else if (Enum.TryParse(args[0], out Il2CppScheduleOne.ItemFramework.EQuality parsedQuality))
                    {
                        quality = parsedQuality;
                    }
                }

                // Create parameter list with just the quality value
                var qualityList = new Il2CppSystem.Collections.Generic.List<string>();
                qualityList.Add(((int)quality).ToString());
        
                // Execute the set quality command
                var cmd = new Il2CppScheduleOne.Console.SetQuality();
                cmd.Execute(qualityList);

                LoggerInstance.Msg($"Set active item to {quality} quality");
                ShowNotification("Item Quality", $"Set to {quality} quality", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error setting item quality: {ex.Message}");
                ShowNotification("Error", "Failed to set item quality", NotificationType.Error);
            }
        }

        // For UI buttons (called from buttons in the UI)
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

                LoggerInstance.Msg($"Set active item to {quality} quality");
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

                // Use existing game command if available
                var command = new Il2CppScheduleOne.Console.FreeCamCommand();
                command.Execute(null);

                LoggerInstance.Msg($"Free camera mode {(_freeCamEnabled ? "enabled" : "disabled")}.");
                ShowNotification("Free Camera", _freeCamEnabled ? "Enabled" : "Disabled",
                    _freeCamEnabled ? NotificationType.Success : NotificationType.Info);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error toggling free camera: {ex.Message}");
                ShowNotification("Error", "Failed to toggle free camera", NotificationType.Error);
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
                    LoggerInstance.Msg("Never wanted enabled.");
                    ShowNotification("Never Wanted", "Enabled", NotificationType.Success);
                }
                else
                {
                    if (_neverWantedCoroutine != null)
                    {
                        MelonCoroutines.Stop(_neverWantedCoroutine);
                        _neverWantedCoroutine = null;
                    }

                    LoggerInstance.Msg("Never wanted disabled.");
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
                    ClearWantedLevel(null);
                }
                catch (System.Exception ex)
                {
                    LoggerInstance.Error($"Error in godmode routine: {ex.Message}");
                }

                // Wait before next health update
                yield return new WaitForSeconds(0.1f);
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
                var method = targetType.GetMethod(methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (method == null)
                {
                    LoggerInstance.Error($"Method {methodName} not found!");
                    return;
                }

                _harmony.Patch(method, prefix: prefix);
                LoggerInstance.Msg($"Patched {methodName}");
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
                        LoggerInstance.Msg($"Godmode enabled for local player: {_localPlayerName}");
                    }
                    else
                    {
                        LoggerInstance.Error("Failed to identify local player for godmode!");
                    }

                    // Patch network methods to block damage for local player only
                    PatchImpactNetworkMethods();

                    // Start the comprehensive godmode coroutine
                    if (_godModeCoroutine == null)
                    {
                        _godModeCoroutine = MelonCoroutines.Start(GodModeRoutine());
                    }
            
                    ShowNotification("Godmode", "Enabled for YOU only", NotificationType.Success);
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
                        LoggerInstance.Msg("Unpatched all Harmony patches");
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Error($"Error unpatching methods: {ex.Message}");
                    }

                    LoggerInstance.Msg("Godmode disabled.");
                    ShowNotification("Godmode", "Disabled", NotificationType.Info);
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
            LoggerInstance.Msg("Starting godmode routine for local player only...");

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

            LoggerInstance.Msg("Godmode routine stopped.");
        }

        private void RaiseWantedLevel(string[] args)
        {
            try
            {
                var command = new Il2CppScheduleOne.Console.RaisedWanted();
                command.Execute(null);
                LoggerInstance.Msg("Raised wanted level.");
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
                LoggerInstance.Msg("Lowered wanted level.");
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
                LoggerInstance.Msg("Cleared player wanted level.");
                ShowNotification("Wanted Level", "Cleared", NotificationType.Success);
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
                LoggerInstance.Msg("Cleared all world trash.");
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
                LoggerInstance.Msg("Forcefully ending tutorial...");
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
                LoggerInstance.Msg("Grow plants executed successfully.");
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
                LoggerInstance.Msg("Forced game save successful.");
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
                LoggerInstance.Error("Invalid amount! Please enter a number.");
                ShowNotification("Error", "Invalid XP amount", NotificationType.Error);
                return;
            }

            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(amount.ToString());

                var cmd = new Il2CppScheduleOne.Console.GiveXP();
                cmd.Execute(commandList);

                LoggerInstance.Msg($"XP changed by {amount}");
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
                LoggerInstance.Error("Invalid amount! Please enter a number.");
                ShowNotification("Error", "Invalid cash amount", NotificationType.Error);
                return;
            }

            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(amount.ToString());

                var cmd = new Il2CppScheduleOne.Console.ChangeCashCommand();
                cmd.Execute(commandList);

                LoggerInstance.Msg($"Cash changed by {amount}");
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
                LoggerInstance.Error("Invalid amount! Please enter a number.");
                ShowNotification("Error", "Invalid balance amount", NotificationType.Error);
                return;
            }

            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(amount.ToString());

                var cmd = new Il2CppScheduleOne.Console.ChangeOnlineBalanceCommand();
                cmd.Execute(commandList);

                LoggerInstance.Msg($"Online balance changed by {amount}");
                ShowNotification("Online Balance", $"Changed by ${amount}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error changing online balance: {ex.Message}");
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
                LoggerInstance.Msg($"World time set to {time}");
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
                LoggerInstance.Msg($"Time scale set to {scale}.");
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
                LoggerInstance.Msg($"Set player movement speed to {speed}!");
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

                LoggerInstance.Msg($"Player stamina reserve set to {reserve}");
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

                LoggerInstance.Msg($"Jump Force set to {force}");
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

                LoggerInstance.Msg($"Law intensity set to {intensity}");
                ShowNotification("Law Intensity", $"Set to {intensity}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error setting law intensity: {ex.Message}");
                ShowNotification("Error", "Failed to set law intensity.", NotificationType.Error);
            }
        }

        private void SetPlayerHealth(string[] args)
        {
            if (args.Length < 1)
            {
                LoggerInstance.Error("Health amount required!");
                ShowNotification("Error", "Health amount required", NotificationType.Error);
                return;
            }

            // Try to parse the first argument into an integer
            if (!int.TryParse(args[0], out int health))
            {
                LoggerInstance.Error("Invalid health amount! Please enter a valid number.");
                ShowNotification("Error", "Invalid health value", NotificationType.Error);
                return;
            }

            try
            {
                // Create a list of arguments (using the IL2CPP version of List with string as the type parameter)
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(health.ToString());

                // Create an instance of the SetHealth command
                var cmd = new Il2CppScheduleOne.Console.SetHealth();

                // Execute the command with the argument list
                cmd.Execute(commandList);

                //LoggerInstance.Msg($"Player health set to {health}");
                //ShowNotification("Health", $"Set to {health}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error setting player health: {ex.Message}");
                ShowNotification("Error", "Failed to set health", NotificationType.Error);
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

                        // Determine if this is a quality item
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
                                if (prop.Name.Contains("Weapon") ||
                                    prop.Name.Contains("Equipped") ||
                                    prop.Name.Contains("CurrentItem"))
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
                    LoggerInstance.Msg($"Found weapon: {foundWeapon.GetType().Name}, " +
                                       $"GameObject: {foundWeapon.gameObject.name}");
                    return foundWeapon;
                }

                //LoggerInstance.Error("No ranged weapon found.");
                return null;
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Comprehensive error in weapon detection: {ex.Message}");
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
                    LoggerInstance.Msg("Unlimited ammo enabled.");
                    ShowNotification("Unlimited Ammo", "Enabled", NotificationType.Success);
                }
                else
                {
                    if (_unlimitedAmmoCoroutine != null)
                    {
                        MelonCoroutines.Stop(_unlimitedAmmoCoroutine);
                        _unlimitedAmmoCoroutine = null;
                    }

                    LoggerInstance.Msg("Unlimited ammo disabled.");
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
            LoggerInstance.Msg("Starting unlimited ammo routine...");

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
                yield return new WaitForSeconds(0.2f);
            }

            LoggerInstance.Msg("Unlimited ammo routine stopped.");
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

        private IEnumerator ExplodeLoopRoutine(OnlinePlayerInfo playerInfo)
        {
            string playerKey = playerInfo.Player.GetInstanceID().ToString();
            LoggerInstance.Msg($"Starting explosion loop for player: {playerInfo.Name}");

            while (playerInfo.ExplodeLoop && playerInfo.Player != null)
            {
                try
                {
                    // Create explosion at player position
                    Vector3 explosionPosition = playerInfo.Player.transform.position;
                    CreateServerSideExplosion(explosionPosition, 10000f, 2f); // Lower values to prevent instant death
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error($"Error in explosion loop: {ex.Message}");
                }

                // Wait before next explosion
                yield return new WaitForSeconds(0.1f);
            }

            LoggerInstance.Msg($"Ended explosion loop for player: {playerInfo.Name}");
            _explodeLoopCoroutines.Remove(playerKey);
        }

        private void CreateExplosion(string[] args)
        {
            try
            {
                // Parse optional parameters (damage and radius)
                float damage = 50f;
                float radius = 5f;
                string target = "custom";
                bool serverSide = true;

                // Parse arguments with more flexibility
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i].ToLowerInvariant();

                    // Check for server-side flag
                    //if (arg == "server")
                    //{
                    //    serverSide = true;
                    //    continue;
                    //}

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
                    LoggerInstance.Error("No players found to target!");
                    ShowNotification("Explosion", "No players found!", NotificationType.Error);
                    return;
                }

                // Explosion positions
                List<Vector3> explosionPositions = new List<Vector3>();

                switch (target)
                {
                    case "nukeall":
                        damage = 99999999f;
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

                LoggerInstance.Msg($"Explosion created: Target={target}, Damage={damage}, Radius={radius}, Server-side=true");
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
                    LoggerInstance.Msg("Explosion created via CreateExplosion");
                }
                catch (Exception createEx)
                {
                    LoggerInstance.Error($"CreateExplosion failed: {createEx.Message}");
                }

                try
                {
                    // Method 2: Explicit Explosion method
                    combatManager.Explosion(position, explosionData, explosionId);
                    LoggerInstance.Msg("Explosion created via Explosion method");
                }
                catch (Exception explodeEx)
                {
                    LoggerInstance.Error($"Explosion method failed: {explodeEx.Message}");
                }

                try
                {
                    // Method 3: Observers RPC method
                    var observersMethod = combatManager.GetType().GetMethod(
                        "RpcWriter___Observers_Explosion_2907189355",
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance
                    );

                    if (observersMethod != null)
                    {
                        observersMethod.Invoke(combatManager, new object[] { position, explosionData, explosionId });
                        LoggerInstance.Msg("Explosion created via Observers RPC method");
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
                        LoggerInstance.Msg($"Explosion Prefab found: {explosionPrefab.name}");

                        // Instantiate explosion prefab manually
                        var instantiatedExplosion = UnityEngine.Object.Instantiate(explosionPrefab.gameObject, position, Quaternion.identity);
                        instantiatedExplosion.transform.position = position;
                        LoggerInstance.Msg("Manually instantiated explosion prefab");
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

                ShowNotification("Explosion", "Server-side explosion created", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Comprehensive server-side explosion error: {ex.Message}");
                ShowNotification("Error", "Failed to create server-side explosion", NotificationType.Error);
            }
        }

        #endregion

        #region Register Commands

        private void RegisterCommands()
        {
            _categories.Clear();

            var onlineCategory = new CommandCategory { Name = "Online" };
            var playerCategory = new CommandCategory { Name = "Self" };
            var exploitsCategory = new CommandCategory { Name = "Exploits" };
            var itemsCategory = new CommandCategory { Name = "Item Manager" };
            var worldCategory = new CommandCategory { Name = "World" };
            var systemCategory = new CommandCategory { Name = "Game" };


            if (!_itemCache.ContainsKey("explosion_targets"))
            {
                _itemCache["explosion_targets"] = new List<string> {
                    "custom",
                    "all",
                    "random",
                    "nukeall"
                };
            }


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
                Name = "List Players",
                Description = "Lists all players in the game with their indices.",
                Handler = ListPlayersCommand
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

            

            // Add categories to list
            _categories.Add(onlineCategory);
            _categories.Add(playerCategory);
            _categories.Add(exploitsCategory);
            _categories.Add(itemsCategory);
            _categories.Add(worldCategory);
            _categories.Add(systemCategory);
        }
        #endregion
    }
    #endregion
}