using System.Collections;
using Il2CppInterop.Runtime;
using MelonLoader;
using Il2CppScheduleOne.ItemFramework;
using UnityEngine;
using Modern_Cheat_Menu.ModGUI;
using Modern_Cheat_Menu.Library;
using Modern_Cheat_Menu.Features;
using Modern_Cheat_Menu.Commands;
using Modern_Cheat_Menu.Settings;
using static Modern_Cheat_Menu.Library.GameplayUtils;

[assembly: MelonInfo(typeof(Modern_Cheat_Menu.Core), Modern_Cheat_Menu.ModInfo.Name, Modern_Cheat_Menu.ModInfo.Version, Modern_Cheat_Menu.ModInfo.Author, null)]
[assembly: MelonGame(Modern_Cheat_Menu.ModInfo.GameDevelopers, Modern_Cheat_Menu.ModInfo.NameOfGame)]
[assembly: HarmonyDontPatchAll]

namespace Modern_Cheat_Menu
{
    public class Core : MelonMod
    {
        public static DrawPlayerMap DrawMap = new DrawPlayerMap(); 
        public static ExplosionManager ExplosionManagerS = new ExplosionManager();
        public static HWIDSpoofer HWIDspoof = new HWIDSpoofer();
        public static GameplayUtils GameUtils = new GameplayUtils();
        public static ModState ModStateS = new ModState();
        public static NotificationSystem Notifier = new NotificationSystem();
        public static ModSettings ModSetting = new ModSettings();
        public static Styles Styler = new Styles();
        public static Teleporter TeleportS = new Teleporter();
        public static UIState UIs = new UIState();

        public static PlayerCommand playerCommand = new PlayerCommand();
        public static SpawnCommand spawnCommand = new SpawnCommand();
        public static WorldCommand worldCommand = new WorldCommand();
        
        public override void OnInitializeMelon() // Must stay in core
        {
            try
            {
                // Initialize Harmony
                ModSetting._harmony = new HarmonyLib.Harmony($"{Modern_Cheat_Menu.ModInfo.ComName}");

                // Patch initialization code...
                ModSetting._harmony.PatchAll(typeof(Core).Assembly);

                // Initialize Keybind config
                ModSetting.InitializeKeybindConfig();

                // Initialize Settings system - add this
                ModSetting.InitializeSettingsSystem();

                // Initialize HWID Spoofer
                HWIDSpoofer.InitializeHwidPatch();

                // Register commands
                CommandRegistry.RegisterCommands();

                // Draw Online Players Map
                DrawPlayerMap.InitializePlayerMap();

                LoggerInstance.Msg($"{Modern_Cheat_Menu.ModInfo.Name} successfully initialized.");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to initialize mod: {ex.Message}");
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName) // Must stay in core
        {
            if (sceneName == "Main" || sceneName == "Tutorial")
            {
                LoggerInstance.Msg("Main scene loaded, initializing cheat menu.");

                // Find the server socket
                ModSetting._discoveredServerSocket = GameUtils.FindBestServerSocket();

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
                Styler.CreateTextures();

                // Create button textures
                DrawPlayerUI.CreateButtonTextures();

                // Cache game items
                ModData.CacheGameItems();
                ModData.CacheNPCData(); 
                ModData.CacheEmotionPresets();

                UIs._isInitialized = true;

                // Subscribe to player death event - add this line
                GameUtils.SubscribeToPlayerDeathEvent();


                ModLogger.Info($"[Init] Local player cached: {LocalPlayerCache.Instance?.name}");

                // Show notification
                Notifier.ShowNotification($"{ModInfo.Name} Loaded", $"Press {ModSetting.CurrentMenuToggleKey} to toggle menu visibility", NotificationSystem.NotificationType.Success);

                ModStateS.trashEstCache = UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.Trash.TrashItem>().Count;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"UI SETUP FAILED: {ex}");
                UIs._isInitialized = false;
                Notifier.ShowNotification("Initialization Failed", ex.Message, NotificationSystem.NotificationType.Error);
            }
        }

        public override void OnUpdate() // Must stay in core
        {
            if (!UIs._isInitialized)
                return;

            // Toggle menu visibility
            if (Input.GetKeyDown(ModSetting.CurrentMenuToggleKey) && !ModStateS._freeCamEnabled)
            {
                ModSetting.ToggleUI(!UIs._uiVisible);
            }

            // Handle ESC key for exiting freecam
            if (ModStateS._freeCamEnabled && Input.GetKeyDown(KeyCode.Escape))
            {
                // Disable freecam and restore normal controls
                ModStateS._freeCamEnabled = false;
                ModSetting.togglePlayerControllable(true);
                Notifier.ShowNotification("Free Camera", "Disabled", NotificationSystem.NotificationType.Info);
            }

            // Disable explosion key if we're in freecam mode as well as when the menu is open
            if (!((ModStateS._freeCamEnabled) || (UIs._uiVisible)))
            {
                if (Input.GetKeyDown(ModSetting.CurrentExplosionAtCrosshairKey) && ExplosionManager.Enabled)
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
                    ExplosionManager.CreateServerSideExplosion(explosionPosition, 99999999999999f, 2f);
                }
            }

            // Update animations for menu
            if (UIs._uiVisible)
            {
                // Menu animation
                UIs._menuAnimationTime += Time.deltaTime * (ModStateS._enableAnimations ? 1.0f : 10.0f);
                if (UIs._menuAnimationTime > 1.0f)
                    UIs._menuAnimationTime = 1.0f;

                // Update fade in animation
                UIs._fadeInProgress += Time.deltaTime * 5f; // Adjust speed as needed
                if (UIs._fadeInProgress > 1.0f)
                    UIs._fadeInProgress = 1.0f;

                // Update tooltip timer
                if (ModStateS._showTooltip)
                {
                    ModStateS._tooltipTimer += Time.deltaTime;
                    if (ModStateS._tooltipTimer > 0.5f) // Show tooltip after 0.5 sec hover
                    {
                        ModStateS._showTooltip = true;
                    }
                }

                // Update button hover animations
                List<string> keysToRemove = new List<string>();
                foreach (var key in UIs._buttonHoverAnimations.Keys)
                {
                    float value = UIs._buttonHoverAnimations[key];
                    if (value > 0)
                    {
                        value -= Time.deltaTime * 4f;
                        if (value <= 0)
                        {
                            value = 0;
                            keysToRemove.Add(key);
                        }
                        UIs._buttonHoverAnimations[key] = value;
                    }
                }

                // Clean up completed animations
                foreach (var key in keysToRemove)
                {
                    UIs._buttonHoverAnimations.Remove(key);
                }

                // Update toggle animations
                keysToRemove.Clear();
                foreach (var key in UIs._toggleAnimations.Keys)
                {
                    bool isOn = false;

                    // Determine if toggle is on based on key
                    if (key == "Godmode")
                        isOn = ModStateS._playerGodmodeEnabled;
                    else if (key == "NeverWanted")
                        isOn = ModStateS._playerNeverWantedEnabled;
                    else if (key == "FreeCamera")
                        isOn = ModStateS._freeCamEnabled;

                    float targetValue = isOn ? 1.0f : 0.0f;
                    float currentValue = UIs._toggleAnimations[key];

                    if (currentValue != targetValue)
                    {
                        if (isOn)
                            currentValue += Time.deltaTime * 4f;
                        else
                            currentValue -= Time.deltaTime * 4f;

                        currentValue = Mathf.Clamp01(currentValue);
                        UIs._toggleAnimations[key] = currentValue;

                        if (currentValue == targetValue)
                            keysToRemove.Add(key);
                    }
                }

                // Clean up completed animations
                foreach (var key in keysToRemove)
                {
                    UIs._toggleAnimations.Remove(key);
                }

                // Update notification animations
                Notifier.UpdateNotifications();
            }
            else
            {
                // Reset menu animation when hidden
                UIs._menuAnimationTime = 0f;
                UIs._fadeInProgress = 0f;

                // Update notification animations even when menu is hidden
                Notifier.UpdateNotifications();
            }

            // Key capture logic for settings
            if (ModSettings._isCapturingKey && Input.anyKeyDown)
            {
                foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(key))
                    {
                        // Prevent capturing Escape or other special keys
                        if (key != KeyCode.Escape)
                        {
                            ModSetting.SaveKeybind(ModSettings._currentKeyCaptureEntry, key);
                        }

                        ModSettings._isCapturingKey = false;
                        ModSettings._currentKeyCaptureEntry = null;
                        break;
                    }
                }
            }
        }

        public override void OnGUI() // Must stay in core
        {
            if (!UIs._isInitialized)
                return;

            // Check if textures/styles need recreation
            if (UIs._needsTextureRecreation)
            {
                Styler.CreateTextures();
                DrawPlayerUI.CreateButtonTextures();
                UIs._needsTextureRecreation = false;
            }

            if (UIs._needsStyleRecreation)
            {
                Styler.InitializeStyles();
                UIs._stylesInitialized = true;
                UIs._needsStyleRecreation = false;
            }

            // Draw notifications even when menu is hidden
            if (ModStateS._activeNotifications.Count > 0)
            {
                Notifier.DrawNotifications();
            }

            // Draw "Freecam Enabled" overlay when in freecam mode
            if (ModStateS._freeCamEnabled && !UIs._uiVisible)
            {
                WorldCommand.DrawFreecamOverlay();
            }

            // Don't process UI when not visible
            if (!UIs._uiVisible)
                return;

            if (!UIs._stylesInitialized)
            {
                Styler.InitializeStyles();
            }

            // Apply custom GUI skin
            GUI.skin = UIs._customSkin;

            // Draw menu with fade in and scale animation
            Color originalColor = GUI.color;
            GUI.color = new Color(1, 1, 1, UIs._fadeInProgress);

            // Apply UI scale
            Matrix4x4 originalMatrix = GUI.matrix;
            if (UIs._uiScale != 1.0f)
            {
                Vector2 center = new Vector2(Screen.width / 1.8f, Screen.height / 2);
                GUI.matrix = Matrix4x4.TRS(
                    center,
                    Quaternion.identity,
                    new Vector3(UIs._uiScale, UIs._uiScale, 1)
                ) * Matrix4x4.TRS(
                    -center,
                    Quaternion.identity,
                    Vector3.one
                );
            }

            // Animation for menu appearance
            float menuAnim = Mathf.SmoothStep(0, 1, UIs._menuAnimationTime);

            // Ensure initial positioning is smooth
            if (UIs._windowRect.x <= -UIs._windowRect.width)
            {
                UIs._windowRect.x = Mathf.Lerp(-UIs._windowRect.width, 20, menuAnim);
            }

            // Draw the main window without a title (we'll add our own)
            UIs._windowRect = GUI.Window(
                0,
                UIs._windowRect,
                DelegateSupport.ConvertDelegate<GUI.WindowFunction>(DrawCoreUI.DrawWindow),
                "",
                UIs._windowStyle
            );

            // Draw tooltip if needed
            if (ModStateS._showTooltip && ModStateS._tooltipTimer > 0.5f)
            {
                Vector2 mousePos = Event.current.mousePosition;
                float tooltipWidth = 250;
                float tooltipHeight = GUI.skin.box.CalcHeight(new GUIContent(ModStateS._currentTooltip), tooltipWidth);

                // Adjust position to keep on screen
                float tooltipX = mousePos.x + 20;
                if (tooltipX + tooltipWidth > Screen.width)
                    tooltipX = Screen.width - tooltipWidth - 10;

                float tooltipY = mousePos.y + 20;
                if (tooltipY + tooltipHeight > Screen.height)
                    tooltipY = mousePos.y - tooltipHeight - 10;

                Rect tooltipRect = new Rect(tooltipX, tooltipY, tooltipWidth, tooltipHeight);
                GUI.Box(tooltipRect, ModStateS._currentTooltip, UIs._tooltipStyle ?? GUI.skin.box);
            }

            // Restore original settings
            GUI.matrix = originalMatrix;
            GUI.color = originalColor;

            // Handle escape key to close menu
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                if (UIs._showSettings)
                {
                    UIs._showSettings = false;
                }
                else
                {
                    ModSetting.ToggleUI(false);
                    ModData._isPropertiesE = false;
                }
                Event.current.Use();
            }



        }
    }
}