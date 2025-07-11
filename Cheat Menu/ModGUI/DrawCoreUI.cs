using static Modern_Cheat_Menu.Core;
using Modern_Cheat_Menu.Commands;
using Modern_Cheat_Menu.Library;
using Modern_Cheat_Menu.Settings;
using UnityEngine;
using Modern_Cheat_Menu.ModGUI.UIManager;
using Il2CppScheduleOne.Management.Presets.Options;
using UnityEngine.InputSystem;

namespace Modern_Cheat_Menu.ModGUI
{
    internal class DrawCoreUI
    {
        public static string _selectedId;
        public static string _SearchText;
        public static Vector2 _ScrollPosition = Vector2.zero;

        public static void ShowDropdownMenu(CommandCore.CommandParameter param, Rect commandRect)
        {
            // Explicit logging
            if (param == null)
            {
                Debug.LogError("CommandParameter is NULL!");
                return;
            }

            if (!ModData._itemCache.ContainsKey(param.ItemCacheKey))
            {
                Debug.LogError($"NO ITEM CACHE FOR KEY: {param.ItemCacheKey}");
                return;
            }

            ModLogger.Info($"DrawCoreUI: param: {param} rect: {commandRect}");

            ModData._commandParam = param;
            ModData._commandRect = commandRect;
            ModData._isPropertiesE = true;

        }

        public static void DrawDropDownMenu()
        {
            CommandCore.CommandParameter param = ModData._commandParam;
            Rect commandRect = ModData._commandRect;

            var items = ModData._itemCache[param.ItemCacheKey];

            GUILayout.BeginHorizontal(UIs._headerStyle);
            GUILayout.Label("Select choice: ", UIs._titleStyle, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("X", UIs._iconButtonStyle, GUILayout.Width(30), GUILayout.Height(30)))
                ModData._isPropertiesE = false;

            GUILayout.EndHorizontal();

            Rect dropdownRect = new Rect(25f, 50 + commandRect.height, commandRect.width, items.Count * 25f);
            GUI.Box(dropdownRect, "");

            for (int i = 0; i < items.Count; i++)
            {
                Rect itemRect = new Rect(dropdownRect.x, dropdownRect.y + i * 25f, dropdownRect.width, 25f);
                if (GUI.Button(itemRect, items[i]))
                {
                    param.Value = items[i];
                    ModData._isPropertiesE = false;
                }
            }
        }

        public static void DrawWindow(int windowId)
        {
            try
            {
                // Draw our custom header with textured buttons
                DrawPlayerUI.DrawHeaderWithTexturedButtons();

                // Main window vertical group - start below header
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                GUILayout.Space(40); // Space for header

                // --- Category Tabs ---
                GUILayout.BeginHorizontal();
                try
                {
                    for (int i = 0; i < ModData._categories.Count; i++)
                    {
                        var style = i == UIs._selectedCategoryIndex ?
                        UIs._categoryButtonActiveStyle : UIs._categoryButtonStyle;

                        if (GUILayout.Button(ModData._categories[i].Name, style, GUILayout.ExpandWidth(true)))
                        {
                            UIs._selectedCategoryIndex = i;
                            UIs._scrollPosition = Vector2.zero;
                        }
                    }
                }
                finally
                {
                    GUILayout.EndHorizontal();
                }

                // --- Content Area ---
                GUILayout.BeginVertical(UIs._panelStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                try
                {
                    if (UIs._showSettings)
                    {
                        DrawSettingsPanel();
                    }
                    else
                    {
                        // Draw based on selected category
                        if (!ModData._isPropertiesE)
                            DrawSelectedCategory();
                        else
                            DrawDropDownMenu();
                    }
                }
                finally
                {
                    GUILayout.EndVertical();
                }

                // Handle window dragging
                InputHandler.HandleWindowDragging();

                GUILayout.EndVertical(); // End main window vertical group
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Window draw error: {ex}");
                GUILayout.EndVertical();
            }
        }
        public static void DrawSelectedCategory()
        {
            if (UIs._selectedCategoryIndex < 0 || UIs._selectedCategoryIndex >= ModData._categories.Count)
                return;

            var category = ModData._categories[UIs._selectedCategoryIndex];

            // Different handling based on category name 
            switch (category.Name)
            {
                case "Item Manager":
                    ItemManager.DrawItemManager();
                    break;
                case "NPC Manager":
                    NPCManager.DrawNPCManager();
                    break;
                case "Online":
                    DrawPlayerMap.DrawOnlinePlayers();
                    break;
                case "Teleport Manager":
                    TeleportS.DrawTeleportManager();
                    break;
                default:
                    DrawCommandUI.DrawCommandCategory(category);
                    break;
            }
        }

        public static void DrawSettingsPanel()
        {
            try
            {
                // Header with title and close button
                GUILayout.BeginHorizontal(UIs._headerStyle);
                GUILayout.Label("Settings", UIs._titleStyle, GUILayout.ExpandWidth(true));

                // Close button
                if (GUILayout.Button("X", UIs._iconButtonStyle, GUILayout.Width(30), GUILayout.Height(30)))
                {
                    UIs._showSettings = false;
                    ModData._isPropertiesE = false; // bad hacky fix
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                // Create scrollview for settings
                UIs._settingsScrollPosition = GUILayout.BeginScrollView(UIs._settingsScrollPosition);

                // Settings content - now organized in sections
                // Visual Settings Section
                GUILayout.BeginVertical(UIs._panelStyle);
                GUILayout.Label("Visual Settings", UIs._subHeaderStyle ?? UIs._labelStyle);

                // UI Scale slider
                GUILayout.BeginHorizontal();
                GUILayout.Label("UI Scale:", GUILayout.Width(120));
                float newScale = GUILayout.HorizontalSlider(UIs._uiScale, 0.7f, 2.5f, UIs._sliderStyle ?? GUI.skin.horizontalSlider,
                                                       UIs._sliderThumbStyle ?? GUI.skin.horizontalSliderThumb, GUILayout.Width(200));
                if (newScale != UIs._uiScale)
                {
                    UIs._uiScale = newScale;
                }
                GUILayout.Label($"{UIs._uiScale:F2}x", GUILayout.Width(50));
                GUILayout.EndHorizontal();

                // UI Opacity slider
                GUILayout.BeginHorizontal();
                GUILayout.Label("UI Opacity:", GUILayout.Width(120));
                float newOpacity = GUILayout.HorizontalSlider(ModStateS._uiOpacity, 0.5f, 1.0f, UIs._sliderStyle ?? GUI.skin.horizontalSlider,
                                                         UIs._sliderThumbStyle ?? GUI.skin.horizontalSliderThumb, GUILayout.Width(200));
                if (newOpacity != ModStateS._uiOpacity)
                {
                    ModStateS._uiOpacity = newOpacity;
                }
                GUILayout.Label($"{(int)(ModStateS._uiOpacity * 100)}%", GUILayout.Width(50));
                GUILayout.EndHorizontal();

                GUILayout.Space(15);

                // Toggle settings
                GUILayout.BeginHorizontal();
                bool newAnimations = GUILayout.Toggle(ModStateS._enableAnimations, "Enable Animations", GUILayout.Width(200));
                if (newAnimations != ModStateS._enableAnimations)
                {
                    ModStateS._enableAnimations = newAnimations;
                    if (!ModStateS._enableAnimations)
                    {
                        // Reset all animations
                        UIs._commandAnimations.Clear();
                        UIs._buttonHoverAnimations.Clear();
                        UIs._itemGridAnimations.Clear();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                ModStateS._enableGlow = GUILayout.Toggle(ModStateS._enableGlow, "Enable Glow Effects", GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                ModStateS._enableBlur = GUILayout.Toggle(ModStateS._enableBlur, "Enable Background Blur", GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                ModStateS._darkTheme = GUILayout.Toggle(ModStateS._darkTheme, "Dark Theme", GUILayout.Width(200));
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

                GUILayout.Space(15);

                // Keybind Settings Section
                GUILayout.BeginVertical(UIs._panelStyle);
                GUILayout.Label("Keyboard Shortcuts", UIs._subHeaderStyle ?? UIs._labelStyle);

                // Menu Toggle Key
                GUILayout.BeginHorizontal();
                GUILayout.Label("Menu Toggle Key:", GUILayout.Width(150));
                string menuKeyText = ModSettings._isCapturingKey && ModSettings._currentKeyCaptureEntry == ModSetting._menuToggleKeyEntry ?
                    "Press any key..." : ModSetting._menuToggleKeyEntry.Value;
                GUILayout.Label(menuKeyText, GUILayout.Width(100));

                if (GUILayout.Button("Change", UIs._buttonStyle, GUILayout.Width(80)))
                {
                    ModSettings.StartCaptureKeybind(ModSetting._menuToggleKeyEntry);
                }
                GUILayout.EndHorizontal();

                // Explosion Key
                GUILayout.BeginHorizontal();
                GUILayout.Label("Explosion Key:", GUILayout.Width(150));
                string explosionKeyText = ModSettings._isCapturingKey && ModSettings._currentKeyCaptureEntry == ModSetting._explosionKeyEntry ?
                    "Press any key..." : ModSetting._explosionKeyEntry.Value;
                GUILayout.Label(explosionKeyText, GUILayout.Width(100));

                if (GUILayout.Button("Change", UIs._buttonStyle, GUILayout.Width(80)))
                {
                    ModSettings.StartCaptureKeybind(ModSetting._explosionKeyEntry);
                }
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

                GUILayout.Space(15);

                // About section
                GUILayout.BeginVertical(UIs._panelStyle);
                GUILayout.Label("About", UIs._subHeaderStyle ?? UIs._labelStyle);
                GUILayout.Label($"{ModInfo.Name} - {ModInfo.Version}");
                GUILayout.Label($"by {ModInfo.Author}");

                // HWID Information
                GUILayout.Space(10);
                GUILayout.Label("HWID Spoofer", UIs._subHeaderStyle ?? UIs._labelStyle);
                GUILayout.Label($"Current HWID: {HWIDSpoofer._generatedHwid}");

                if (GUILayout.Button("Generate New HWID", UIs._buttonStyle, GUILayout.Width(150)))
                {
                    HWIDspoof.GenerateNewHWID(null);
                }
                GUILayout.EndVertical();

                GUILayout.Space(15);

                // Action buttons
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Save Settings", UIs._buttonStyle, GUILayout.Width(150)))
                {
                    ModSetting.SaveSettings();
                }

                if (GUILayout.Button("Reset Settings", UIs._buttonStyle, GUILayout.Width(150)))
                {
                    // Reset to defaults
                    UIs._uiScale = 1.0f;
                    ModStateS._uiOpacity = 0.95f;
                    ModStateS._enableAnimations = true;
                    ModStateS._enableGlow = true;
                    ModStateS._enableBlur = true;
                    ModStateS._darkTheme = true;

                    Notifier.ShowNotification("Settings Reset", "All settings have been reset to defaults", NotificationSystem.NotificationType.Info);
                }
                GUILayout.EndHorizontal();

                GUILayout.EndScrollView();
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error drawing settings panel: {ex.Message}");
            }
        }
        public static void ShowItemTooltip(string itemName, Rect hoverRect)
        {
            string itemId = ModData._itemDictionary[itemName];
            ModStateS._currentTooltip = $"Item: {itemName}\nID: {itemId}";
            ModStateS._tooltipPosition = new Vector2(hoverRect.xMax + 10, hoverRect.y);
            ModStateS._showTooltip = true;
            ModStateS._tooltipTimer = 0f;
        }

        public static void ShowNPCTip(string npcName, Rect hoverRect)
        {
            string npcID = ModData._npcDictionary[npcName];
            ModStateS._currentTooltip = $"NPCName: {npcName}\nID: {npcID}";
            ModStateS._tooltipPosition = new Vector2(hoverRect.xMax + 10, hoverRect.y);
            ModStateS._showTooltip = true;
            ModStateS._tooltipTimer = 0f;
        }
    }
}
