using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Management;
using System.Linq;
using System;
using static Il2CppScheduleOne.Console;
using static Il2CppScheduleOne.GameInput;
using HarmonyLib;
using UnityEngine.Playables;

[assembly: MelonInfo(typeof(Modern_Cheat_Menu.Core), "Modern Cheat Menu", "2.0.0", "darkness", null)]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
[assembly: VerifyLoaderVersion(0, 6, 1, true)]

namespace Modern_Cheat_Menu
{
    // Custom text field implementation to replace Unity's TextField
    public class CustomTextField
    {
        private string _value;
        private GUIStyle _style;
        private bool _isFocused;
        private int _id;
        private static int _nextId = 1000;
        private static int _focusedId = -1;

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
            bool wasMouseDown = current.type == EventType.MouseDown && position.Contains(current.mousePosition);

            // Handle focus
            if (wasMouseDown)
            {
                _focusedId = _id;
                _isFocused = true;
                GUI.FocusControl(_id.ToString());
                current.Use();
            }
            else if (current.type == EventType.MouseDown && _isFocused)
            {
                _isFocused = false;
                _focusedId = -1;
            }

            // Draw the field background
            GUI.Box(position, text, style);

            // Handle keyboard input if focused
            if (_isFocused && current.type == EventType.KeyDown)
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
                        _isFocused = false;
                        _focusedId = -1;
                        current.Use();
                        break;
                    default:
                        // Add character if it's a valid key input
                        if (!char.IsControl(current.character) && current.character != '\0')
                        {
                            _value += current.character;
                            current.Use();
                        }
                        break;
                }
            }

            return _value;
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
        private HarmonyLib.Harmony _harmony;

        // Add dictionary for text fields
        private Dictionary<string, CustomTextField> _textFields = new Dictionary<string, CustomTextField>();

        // Toggle key for menu
        private const KeyCode ToggleMenuKey = KeyCode.F10;

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
        private bool _playerGodmodeEnabled = false;
        private object _godModeCoroutine = null;
        private bool _playerNeverWantedEnabled = false;
        private object _neverWantedCoroutine = null;

        // Free camera settings
        private bool _freeCamEnabled = false;
        private Camera _mainCamera;

        // Categories and commands
        private List<CommandCategory> _categories = new List<CommandCategory>();
        private Dictionary<string, string> _itemDictionary = new Dictionary<string, string>();
        private Dictionary<string, string> _qualitiesDictionary = new Dictionary<string, string>();
        private Dictionary<string, List<string>> _itemCache = new Dictionary<string, List<string>>();

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
                _harmony = new HarmonyLib.Harmony("com.darkness.menu");

                // Register commands
                RegisterCommands();

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
            if (!_isInitialized || !_uiVisible)
            {
                // Draw notifications even when menu is hidden
                if (_activeNotifications.Count > 0)
                {
                    DrawNotifications();
                }
                return;
            }

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

            // Draw notifications
            DrawNotifications();

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
                    if (GUILayout.Button("?", _iconButtonStyle,
                        GUILayout.Width(30), GUILayout.Height(30)))
                    {
                        _showSettings = true;
                    }

                    // Close button
                    if (GUILayout.Button("?", _iconButtonStyle,
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
                    else if (_selectedCategoryIndex >= 0 && _selectedCategoryIndex < _categories.Count)
                    {
                        var category = _categories[_selectedCategoryIndex];
                        if (category.Name == "Item Manager")
                        {
                            DrawItemManager();
                        }
                        else
                        {
                            DrawCommandCategory(category);
                        }
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

        private void HandleWindowDragging()
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                var headerRect = new Rect(_windowRect.x, _windowRect.y, _windowRect.width, 40);
                if (headerRect.Contains(Event.current.mousePosition))
                {
                    _isDragging = true;
                    _dragOffset = new Vector2(
                        _windowRect.x - Event.current.mousePosition.x,
                        _windowRect.y - Event.current.mousePosition.y
                    );
                }
            }
            else if (Event.current.type == EventType.MouseDrag && _isDragging)
            {
                _windowRect.x = Event.current.mousePosition.x + _dragOffset.x;
                _windowRect.y = Event.current.mousePosition.y + _dragOffset.y;
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                _isDragging = false;
            }
        }

        private void DrawCommandCategory(CommandCategory category)
        {
            const float paramSpacing = 5f;
            const float buttonWidth = 120f;

            GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            try
            {
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

                try
                {
                    foreach (var command in category.Commands)
                    {
                        GUILayout.BeginVertical(_panelStyle);
                        try
                        {
                            // Command Header
                            GUILayout.BeginHorizontal();
                            try
                            {
                                GUILayout.Label(command.Name, _commandLabelStyle ?? _labelStyle,
                                              GUILayout.Width(200));

                                // Parameters
                                if (command.Parameters.Count > 0)
                                {
                                    GUILayout.FlexibleSpace();

                                    foreach (var param in command.Parameters)
                                    {
                                        GUILayout.Space(paramSpacing);

                                        // Input Field with custom text field implementation
                                        if (param.Type == ParameterType.Input)
                                        {
                                            // Use our safe custom text field
                                            string paramKey = $"param_{command.Name}_{param.Name}";
                                            param.Value = SafeCustomTextField(paramKey, param.Value ?? "", _inputFieldStyle ?? GUI.skin.textField, GUILayout.Width(120));
                                        }
                                        // Dropdown implementation
                                        else if (param.Type == ParameterType.Dropdown)
                                        {
                                            Rect dropdownRect = GUILayoutUtility.GetRect(
                                                120f, 25f,
                                                GUILayout.Width(120f));

                                            if (GUI.Button(dropdownRect, param.Value ?? "Select", _buttonStyle))
                                            {
                                                ShowDropdownMenu(param);
                                            }
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                GUILayout.EndHorizontal();
                            }

                            // Execute Button
                            Rect buttonRect = GUILayoutUtility.GetRect(
                                buttonWidth, 30f,
                                GUILayout.Width(buttonWidth));

                            if (GUI.Button(buttonRect, "Execute", _buttonStyle))
                            {
                                ExecuteCommand(command);
                            }

                            // Description
                            if (!string.IsNullOrEmpty(command.Description))
                            {
                                GUILayout.Label(command.Description, _tooltipStyle ?? GUI.skin.label);
                            }
                        }
                        finally
                        {
                            GUILayout.EndVertical();
                        }

                        GUILayout.Space(10f);
                    }
                }
                finally
                {
                    GUILayout.EndScrollView();
                }
            }
            finally
            {
                GUILayout.EndVertical();
            }
        }

        private void DrawItemManager()
        {
            const float itemSize = 100f;
            const float spacing = 10f;

            GUILayout.BeginVertical();

            // Search Bar
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(60));
            _itemSearchText = SafeCustomTextField("itemSearch", _itemSearchText, _searchBoxStyle ?? GUI.skin.textField);
            GUILayout.EndHorizontal();

            // Item Grid
            _itemScrollPosition = GUILayout.BeginScrollView(_itemScrollPosition);

            try
            {
                if (!_itemCache.TryGetValue("items", out var allItems)) return;

                // Filter items based on search
                var filteredItems = allItems
                    .Where(item => item.IndexOf(_itemSearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                int columns = Mathf.FloorToInt((_windowRect.width - 40f) / (itemSize + spacing));
                columns = Mathf.Max(columns, 1);

                for (int i = 0; i < filteredItems.Count; i += columns)
                {
                    GUILayout.BeginHorizontal();
                    for (int j = 0; j < columns; j++)
                    {
                        int index = i + j;
                        if (index >= filteredItems.Count) break;

                        string itemName = filteredItems[index];
                        DrawItemButton(itemName, itemSize);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(spacing);
                }
            }
            finally
            {
                GUILayout.EndScrollView();
            }

            // Item details and action panel
            if (!string.IsNullOrEmpty(_selectedItemId))
            {
                GUILayout.BeginVertical(_panelStyle);
                GUILayout.Label("Selected Item: " + _selectedItemId, _labelStyle);

                // Quantity input
                GUILayout.BeginHorizontal();
                GUILayout.Label("Quantity:", GUILayout.Width(70));
                _quantityInput = SafeCustomTextField("quantityInput", _quantityInput, _inputFieldStyle ?? GUI.skin.textField, GUILayout.Width(60));
                GUILayout.EndHorizontal();

                // Slot input
                GUILayout.BeginHorizontal();
                GUILayout.Label("Slot:", GUILayout.Width(70));
                _slotInput = SafeCustomTextField("slotInput", _slotInput, _inputFieldStyle ?? GUI.skin.textField, GUILayout.Width(60));
                GUILayout.EndHorizontal();

                // Quality selection
                GUILayout.BeginHorizontal();
                GUILayout.Label("Quality:", GUILayout.Width(70));
                if (_itemCache.TryGetValue("qualities", out var qualities))
                {
                    for (int i = 0; i < qualities.Count; i++)
                    {
                        var style = i == _selectedQualityIndex ? _itemSelectedStyle : _itemButtonStyle;
                        if (GUILayout.Button(qualities[i], style ?? _buttonStyle, GUILayout.Width(80)))
                        {
                            _selectedQualityIndex = i;
                        }
                    }
                }
                GUILayout.EndHorizontal();

                // Spawn button
                if (GUILayout.Button("Spawn Item", _buttonStyle, GUILayout.Height(30)))
                {
                    SpawnItem();
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
        }

        private void DrawItemButton(string itemName, float size)
        {
            if (string.IsNullOrEmpty(itemName) || !_itemDictionary.ContainsKey(itemName))
                return;

            Rect btnRect = GUILayoutUtility.GetRect(size, size,
                                                  GUILayout.Width(size),
                                                  GUILayout.Height(size));

            bool isSelected = _selectedItemId == _itemDictionary[itemName];
            var style = isSelected ? _itemSelectedStyle : _itemButtonStyle;
            style = style ?? _buttonStyle;

            if (GUI.Button(btnRect, itemName, style))
            {
                _selectedItemId = _itemDictionary[itemName];
            }

            if (btnRect.Contains(Event.current.mousePosition))
            {
                ShowItemTooltip(itemName, btnRect);
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
            if (GUILayout.Button("?", _iconButtonStyle, GUILayout.Width(30), GUILayout.Height(30)))
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
            if (!_itemCache.ContainsKey(param.ItemCacheKey)) return;

            var items = _itemCache[param.ItemCacheKey];

            // Default to first item if none selected
            if (string.IsNullOrEmpty(param.Value) && items.Count > 0)
                param.Value = items[0];

            // For now, just cycle to the next value in the list
            int currentIndex = items.IndexOf(param.Value);
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

        private void SpawnItem()
        {
            if (string.IsNullOrEmpty(_selectedItemId))
            {
                ShowNotification("Error", "No item selected", NotificationType.Error);
                return;
            }

            int quantity = 1;
            if (!int.TryParse(_quantityInput, out quantity) || quantity < 1)
            {
                quantity = 1;
            }

            try
            {
                GiveItem(new string[] { _selectedItemId, quantity.ToString() });
                ShowNotification("Item Spawned", $"Added {quantity}x {_selectedItemId}", NotificationType.Success);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to spawn item: {ex.Message}");
                ShowNotification("Error", $"Failed to spawn item: {ex.Message}", NotificationType.Error);
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

            float startY = 40f;
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

        #region Command Implementations

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

        private void ToggleGodmode(string[] args)
        {
            try
            {
                // Toggle godmode state
                _playerGodmodeEnabled = !_playerGodmodeEnabled;

                if (_playerGodmodeEnabled)
                {
                    // Start the godmode coroutine if it's not already running
                    if (_godModeCoroutine == null)
                    {
                        _godModeCoroutine = MelonCoroutines.Start(GodModeRoutine());
                    }
                    LoggerInstance.Msg("Godmode enabled.");
                    ShowNotification("Godmode", "Enabled", NotificationType.Success);
                }
                else
                {
                    // Stop the godmode coroutine if it's running
                    if (_godModeCoroutine != null)
                    {
                        MelonCoroutines.Stop(_godModeCoroutine);
                        _godModeCoroutine = null;
                    }

                    // Reset health to normal
                    SetPlayerHealth(new string[] { "100" });
                    LoggerInstance.Msg("Godmode disabled.");
                    ShowNotification("Godmode", "Disabled", NotificationType.Info);
                }
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error toggling godmode: {ex.Message}");
                ShowNotification("Error", "Failed to toggle godmode", NotificationType.Error);
            }
        }

        private IEnumerator GodModeRoutine()
        {
            while (true)
            {
                try
                {
                    // Set health to high value
                    SetPlayerHealth(new string[] { "1000" });
                }
                catch (System.Exception ex)
                {
                    LoggerInstance.Error($"Error in godmode routine: {ex.Message}");
                }

                // Wait before next health update
                yield return new WaitForSeconds(0.1f);
            }
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

        private void GiveItem(string[] args)
        {
            if (args.Length < 1)
            {
                LoggerInstance.Error("Item name required!");
                ShowNotification("Error", "Item name required", NotificationType.Error);
                return;
            }

            string itemDisplayName = args[0];
            LoggerInstance.Msg($"Got item display name: {itemDisplayName}");

            // Try to get the item ID directly from the dictionary
            string itemId;
            if (!_itemDictionary.TryGetValue(itemDisplayName, out itemId))
            {
                // If not found, fall back to lowercase (though this is less reliable)
                itemId = itemDisplayName.ToLower();
                LoggerInstance.Error($"Could not find exact mapping for '{itemDisplayName}', using '{itemId}' as fallback");
            }
            else
            {
                LoggerInstance.Msg($"Matched to internal ID: {itemId}");
            }

            int quantity = 1;
            if (args.Length >= 2 && int.TryParse(args[1], out int parsedQuantity))
            {
                quantity = parsedQuantity;
            }

            try
            {
                var commandList = new Il2CppSystem.Collections.Generic.List<string>();
                commandList.Add(itemId);
                if (quantity > 1)
                {
                    commandList.Add(quantity.ToString());
                }

                var cmd = new Il2CppScheduleOne.Console.AddItemToInventoryCommand();
                cmd.Execute(commandList);

                LoggerInstance.Msg($"Added {quantity}x {itemId}");
                ShowNotification("Items", $"Added {quantity}x {itemId}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error adding item: {ex.Message}");
                ShowNotification("Error", $"Failed to add item: {ex.Message}", NotificationType.Error);
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

        private void SetGameTime(string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int timeValue))
            {
                LoggerInstance.Error("Invalid time format! Please use HHMM format (e.g. 1530 for 3:30 PM)");
                ShowNotification("Error", "Invalid time format", NotificationType.Error);
                return;
            }

            // Convert time format (e.g., 1530) to hours and minutes
            int hours = timeValue / 100;
            int minutes = timeValue % 100;

            if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59)
            {
                LoggerInstance.Error("Invalid time! Hours must be 0-23 and minutes 0-59");
                ShowNotification("Error", "Invalid time values", NotificationType.Error);
                return;
            }

            try
            {
                var managers = UnityEngine.Object.FindObjectsOfType(Il2CppType.Of<MonoBehaviour>());

                foreach (var obj in managers)
                {
                    MonoBehaviour mb = obj.Cast<MonoBehaviour>();
                    string typeName = mb.GetType().Name;

                    if (typeName.Contains("Time") || typeName.Contains("Day") ||
                        typeName.Contains("Weather") || typeName.Contains("World"))
                    {
                        // Look for time property
                        var timeProperty = mb.GetType().GetProperty("TimeOfDay");
                        if (timeProperty != null)
                        {
                            // Convert to normalized time (0-1)
                            float normalizedTime = (hours + (minutes / 60f)) / 24f;

                            // Set new time
                            timeProperty.SetValue(mb, normalizedTime);
                            LoggerInstance.Msg($"Time set to {hours:D2}:{minutes:D2}");
                            ShowNotification("Game Time", $"Set to {hours:D2}:{minutes:D2}", NotificationType.Success);
                            return;
                        }
                    }
                }

                LoggerInstance.Error("Could not find time controller!");
                ShowNotification("Error", "Could not find time controller", NotificationType.Error);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error setting time: {ex.Message}");
                ShowNotification("Error", "Failed to set time", NotificationType.Error);
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

            // Clamp to reasonable range
            scale = Mathf.Clamp(scale, 0.1f, 10f);

            // Set Unity time scale
            Time.timeScale = scale;
            LoggerInstance.Msg($"Time scale set to {scale}x");
            ShowNotification("Time Scale", $"Set to {scale:F1}x", NotificationType.Success);
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

                LoggerInstance.Msg($"Player health set to {health}");
                ShowNotification("Health", $"Set to {health}", NotificationType.Success);
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error setting player health: {ex.Message}");
                ShowNotification("Error", "Failed to set health", NotificationType.Error);
            }
        }

        private void CacheGameItems()
        {
            try
            {
                var qualitiesDict = new Dictionary<string, string>();

                qualitiesDict.Add("Trash", "trash");
                qualitiesDict.Add("Poor", "poor");
                qualitiesDict.Add("Standard", "standard");
                qualitiesDict.Add("Premium", "premium");
                qualitiesDict.Add("Heavenly", "heavenly");

                _qualitiesDictionary = qualitiesDict;

                // Store the display names for dropdown
                var qualityNames = qualitiesDict.Keys.OrderBy(name => name).ToList();
                _itemCache["qualities"] = qualityNames;

                // Add hotbar slots
                _itemCache["slots"] = Enumerable.Range(1, 9)
                    .Select(x => x.ToString())
                    .ToList();

                LoggerInstance.Msg("Loading predefined item list...");

                // Create a dictionary to map display names to ItemIDs
                var itemsDict = new Dictionary<string, string>();

                // Add all items from our predefined list
                itemsDict.Add("TV", "tv");
                itemsDict.Add("Acid", "acid");
                itemsDict.Add("Addy", "addy");
                itemsDict.Add("Air Pot", "airpot");
                itemsDict.Add("Antique Wall Lamp", "antiquewalllamp");
                itemsDict.Add("Apron", "apron");
                itemsDict.Add("Artwork (Beach Day)", "artworkbeachday");
                itemsDict.Add("Artwork (Lines)", "artworklines");
                itemsDict.Add("Artwork (Menace)", "artworkmenace");
                itemsDict.Add("Artwork (Millie)", "artworkmillie");
                itemsDict.Add("Artwork (Offer)", "artworkoffer");
                itemsDict.Add("Artwork (Rapscallion)", "artworkrapscallion");
                itemsDict.Add("Baby Blue", "babyblue");
                itemsDict.Add("Baggie", "baggie");
                itemsDict.Add("Banana", "banana");
                itemsDict.Add("Baseball Bat", "baseballbat");
                itemsDict.Add("Battery", "battery");
                itemsDict.Add("Bed", "bed");
                itemsDict.Add("Belt", "belt");
                itemsDict.Add("Big Sprinkler", "bigsprinkler");
                itemsDict.Add("Biker Crank", "bikercrank");
                itemsDict.Add("Blazer", "blazer");
                itemsDict.Add("Brick", "brick");
                itemsDict.Add("Brick Press", "brickpress");
                itemsDict.Add("Brut du Gloop", "brutdugloop");
                itemsDict.Add("Bucket Hat", "buckethat");
                itemsDict.Add("Button-Up Shirt", "buttonup");
                itemsDict.Add("Cap", "cap");
                itemsDict.Add("Cargo Pants", "cargopants");
                itemsDict.Add("Cash", "cash");
                itemsDict.Add("Cauldron", "cauldron");
                itemsDict.Add("Chateau La Peepee", "chateaulapeepee");
                itemsDict.Add("Cheap Skateboard", "cheapskateboard");
                itemsDict.Add("Chef Hat", "chefhat");
                itemsDict.Add("Chemistry Station", "chemistrystation");
                itemsDict.Add("Chili", "chili");
                itemsDict.Add("Cocaine", "cocaine");
                itemsDict.Add("Cocaine Base", "cocainebase");
                itemsDict.Add("Coca Leaf", "cocaleaf");
                itemsDict.Add("Coca Seed", "cocaseed");
                itemsDict.Add("Coffee Table", "coffeetable");
                itemsDict.Add("Collar Jacket", "collarjacket");
                itemsDict.Add("Combat Boots", "combatboots");
                itemsDict.Add("Cowboy Hat", "cowboyhat");
                itemsDict.Add("Cruiser", "cruiser");
                itemsDict.Add("Cuke", "cuke");
                itemsDict.Add("Default Weed", "defaultweed");
                itemsDict.Add("Display Cabinet", "displaycabinet");
                itemsDict.Add("Donut", "donut");
                itemsDict.Add("Dress Shoes", "dressshoes");
                itemsDict.Add("Drying Rack", "dryingrack");
                itemsDict.Add("Dumpster", "dumpster");
                itemsDict.Add("Electric Plant Trimmers", "electrictrimmers");
                itemsDict.Add("Energy Drink", "energydrink");
                itemsDict.Add("Extra Long-Life Soil", "extralonglifesoil");
                itemsDict.Add("Fertilizer", "fertilizer");
                itemsDict.Add("Filing Cabinet", "filingcabinet");
                itemsDict.Add("Fingerless Gloves", "fingerlessgloves");
                itemsDict.Add("Flannel Shirt", "flannelshirt");
                itemsDict.Add("Flashlight", "flashlight");
                itemsDict.Add("Flat Cap", "flatcap");
                itemsDict.Add("Flats", "flats");
                itemsDict.Add("Floor Lamp", "floorlamp");
                itemsDict.Add("Flu Medicine", "flumedicine");
                itemsDict.Add("Frying Pan", "fryingpan");
                itemsDict.Add("Full Spectrum Grow Light", "fullspectrumgrowlight");
                itemsDict.Add("Gasoline", "gasoline");
                itemsDict.Add("Glass", "glass");
                itemsDict.Add("Gloves", "gloves");
                itemsDict.Add("Gold Bar", "goldbar");
                itemsDict.Add("Gold Chain", "goldchain");
                itemsDict.Add("Gold Watch", "goldwatch");
                itemsDict.Add("Golden Skateboard", "goldenskateboard");
                itemsDict.Add("Grandaddy Purple", "granddaddypurple");
                itemsDict.Add("Grandaddy Purple Seed", "granddaddypurpleseed");
                itemsDict.Add("Grandfather Clock", "grandfatherclock");
                itemsDict.Add("Green Crack", "greencrack");
                itemsDict.Add("Green Crack Seed", "greencrackseed");
                itemsDict.Add("Grow Tent", "growtent");
                itemsDict.Add("Halogen Grow Light", "halogengrowlight");
                itemsDict.Add("High-Quality Pseudo", "highqualitypseudo");
                itemsDict.Add("Horse Semen", "horsesemen");
                itemsDict.Add("Iodine", "iodine");
                itemsDict.Add("Jar", "jar");
                itemsDict.Add("Jeans", "jeans");
                itemsDict.Add("Jorts", "jorts");
                itemsDict.Add("Lab Oven", "laboven");
                itemsDict.Add("Large Storage Rack", "largestoragerack");
                itemsDict.Add("Laundering Station", "launderingstation");
                itemsDict.Add("LED Grow Light", "ledgrowlight");
                itemsDict.Add("Legend Sunglasses", "legendsunglasses");
                itemsDict.Add("Lightweight Skateboard", "lightweightskateboard");
                itemsDict.Add("Baby Blue (Liquid)", "liquidbabyblue");
                itemsDict.Add("Biker Crank (Liquid)", "liquidbikercrank");
                itemsDict.Add("Glass (Liquid)", "liquidglass");
                itemsDict.Add("Meth (Liquid)", "liquidmeth");
                itemsDict.Add("Long-Life Soil", "longlifesoil");
                itemsDict.Add("Long Skirt", "longskirt");
                itemsDict.Add("Low-Quality Pseudo", "lowqualitypseudo");
                itemsDict.Add("M1911", "m1911");
                itemsDict.Add("M1911 Magazine", "m1911mag");
                itemsDict.Add("Machete", "machete");
                itemsDict.Add("Management Clipboard", "managementclipboard");
                itemsDict.Add("Medium Storage Rack", "mediumstoragerack");
                itemsDict.Add("Mega Bean", "megabean");
                itemsDict.Add("Metal Sign", "metalsign");
                itemsDict.Add("Metal Square Table", "metalsquaretable");
                itemsDict.Add("Meth", "meth");
                itemsDict.Add("Mixing Station", "mixingstation");
                itemsDict.Add("Mixing Station Mk2", "mixingstationmk2");
                itemsDict.Add("Modern Wall Lamp", "modernwalllamp");
                itemsDict.Add("Moisture-Preserving Pot", "moisturepreservingpot");
                itemsDict.Add("Motor Oil", "motoroil");
                itemsDict.Add("Mouth Wash", "mouthwash");
                itemsDict.Add("OG Kush", "ogkush");
                itemsDict.Add("OG Kush Seed", "ogkushseed");
                itemsDict.Add("Ol' Man Jimmy's", "oldmanjimmys");
                itemsDict.Add("Overalls", "overalls");
                itemsDict.Add("Packaging Station", "packagingstation");
                itemsDict.Add("Packaging Station Mk2", "packagingstationmk2");
                itemsDict.Add("Paracetamol", "paracetamol");
                itemsDict.Add("PGR", "pgr");
                itemsDict.Add("Phosphorus", "phosphorus");
                itemsDict.Add("Plastic Pot", "plasticpot");
                itemsDict.Add("Porkpie Hat", "porkpiehat");
                itemsDict.Add("Pot Sprinkler", "potsprinkler");
                itemsDict.Add("Pseudo", "pseudo");
                itemsDict.Add("Rectangle Frame Glasses", "rectangleframeglasses");
                itemsDict.Add("Revolver", "revolver");
                itemsDict.Add("Revolver Cylinder", "revolvercylinder");
                itemsDict.Add("Button-Up Shirt (Rolled)", "rolledbuttonup");
                itemsDict.Add("Safe", "safe");
                itemsDict.Add("Sandals", "sandals");
                itemsDict.Add("Saucepan", "saucepan");
                itemsDict.Add("Silver Chain", "silverchain");
                itemsDict.Add("Silver Watch", "silverwatch");
                itemsDict.Add("Skateboard", "skateboard");
                itemsDict.Add("Skirt", "skirt");
                itemsDict.Add("Small Round Glasses", "smallroundglasses");
                itemsDict.Add("Small Storage Rack", "smallstoragerack");
                itemsDict.Add("Small Trash Can", "smalltrashcan");
                itemsDict.Add("Sneakers", "sneakers");
                itemsDict.Add("Soil", "soil");
                itemsDict.Add("Soil Pourer", "soilpourer");
                itemsDict.Add("Sour Diesel", "sourdiesel");
                itemsDict.Add("Sour Diesel Seed", "sourdieselseed");
                itemsDict.Add("Speed Dealer Shades", "speeddealershades");
                itemsDict.Add("Speed Grow", "speedgrow");
                itemsDict.Add("Suspension Rack", "suspensionrack");
                itemsDict.Add("Tactical Vest", "tacticalvest");
                itemsDict.Add("Test Weed", "testweed");
                itemsDict.Add("Trash Bag", "trashbag");
                itemsDict.Add("Trash Can", "trashcan");
                itemsDict.Add("Trash Grabber", "trashgrabber");
                itemsDict.Add("Plant Trimmers", "trimmers");
                itemsDict.Add("T-Shirt", "tshirt");
                itemsDict.Add("Vest", "vest");
                itemsDict.Add("Viagra", "viagra");
                itemsDict.Add("V-Neck Shirt", "vneck");
                itemsDict.Add("Wall Clock", "wallclock");
                itemsDict.Add("Wall-Mounted Shelf", "wallmountedshelf");
                itemsDict.Add("Watering Can", "wateringcan");
                itemsDict.Add("Wooden Sign", "woodensign");
                itemsDict.Add("Wooden Square Table", "woodsquaretable");

                //Create a class variable to store the dictionary for later use
                _itemDictionary = itemsDict;

                //Store the display names for dropdown
                var displayNames = itemsDict.Keys.OrderBy(name => name).ToList();
                _itemCache["items"] = displayNames;

                _itemCache["items"] = displayNames ?? new List<string>();
                _itemCache["qualities"] = qualityNames ?? new List<string>();
                _itemCache["slots"] = Enumerable.Range(1, 9).Select(x => x.ToString()).ToList();

                LoggerInstance.Msg($"Items cached: {_itemCache["items"].Count}, " +
                                  $"Qualities: {_itemCache["qualities"].Count}, " +
                                  $"Slots: {_itemCache["slots"].Count}");

                LoggerInstance.Msg($"Successfully loaded {displayNames.Count} items for the modern cheat menu");
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error($"Error loading item list: {ex.Message}");
                ShowNotification("Error", "Failed to load item list", NotificationType.Error);
            }
        }

        #endregion

        #region Register Commands

        private void RegisterCommands()
        {
            _categories.Clear();

            // Player category
            var playerCategory = new CommandCategory { Name = "Player" };
            playerCategory.Commands.Add(new Command
            {
                Name = "Toggle Godmode",
                Description = "Toggles godmode on/off.",
                Handler = ToggleGodmode
            });
            playerCategory.Commands.Add(new Command
            {
                Name = "Toggle Never Wanted",
                Description = "Toggles never wanted on/off.",
                Handler = ToggleNeverWanted
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

            // World category
            var worldCategory = new CommandCategory { Name = "World" };
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
                Handler = SetGameTime,
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

            // Items Category
            var itemsCategory = new CommandCategory { Name = "Item Manager" };

            // Misc category
            var miscCategory = new CommandCategory { Name = "Misc" };
            miscCategory.Commands.Add(new Command
            {
                Name = "Raise Wanted Level",
                Description = "Raises your wanted level.",
                Handler = RaiseWantedLevel
            });
            miscCategory.Commands.Add(new Command
            {
                Name = "Lower Wanted Level",
                Description = "Lowers your wanted level.",
                Handler = LowerWantedLevel
            });
            miscCategory.Commands.Add(new Command
            {
                Name = "Clear Wanted Level",
                Description = "Clears your wanted level.",
                Handler = ClearWantedLevel
            });

            // System category
            var systemCategory = new CommandCategory { Name = "System" };
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
            systemCategory.Commands.Add(new Command
            {
                Name = "Clear Inventory",
                Description = "Clears the player's inventory",
                Handler = ClearInventory
            });
            systemCategory.Commands.Add(new Command
            {
                Name = "Clear World Trash",
                Description = "Forcefully clears all world trash.",
                Handler = ClearTrash
            });

            // Add categories to list
            _categories.Add(playerCategory);
            _categories.Add(itemsCategory);
            _categories.Add(worldCategory);
            _categories.Add(miscCategory);
            _categories.Add(systemCategory);
        }

        #endregion
    }
    #endregion
}