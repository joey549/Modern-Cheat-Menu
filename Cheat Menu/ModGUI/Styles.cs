using static Modern_Cheat_Menu.Core;
using Modern_Cheat_Menu.Library;
using UnityEngine;

namespace Modern_Cheat_Menu.ModGUI
{
    public class Styles
    {
        #region Textures and Styles
        public void CreateTextures()
        {
            try
            {
                // Background texture
                UIs._backgroundTexture = new Texture2D(1, 1);
                UIs._backgroundTexture.SetPixel(0, 0, UIs._backgroundColor);
                UIs._backgroundTexture.Apply();

                // Panel texture
                UIs._panelTexture = new Texture2D(1, 1);
                UIs._panelTexture.SetPixel(0, 0, UIs._panelColor);
                UIs._panelTexture.Apply();

                // Button textures
                UIs._buttonNormalTexture = new Texture2D(1, 1);
                UIs._buttonNormalTexture.SetPixel(0, 0, new Color(0.18f, 0.18f, 0.24f, 0.8f));
                UIs._buttonNormalTexture.Apply();

                UIs._buttonHoverTexture = new Texture2D(1, 1);
                UIs._buttonHoverTexture.SetPixel(0, 0, new Color(0.25f, 0.25f, 0.35f, 0.9f));
                UIs._buttonHoverTexture.Apply();

                UIs._buttonActiveTexture = new Texture2D(1, 1);
                UIs._buttonActiveTexture.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.4f, 1f));
                UIs._buttonActiveTexture.Apply();

                // Tab textures
                UIs._categoryTabTexture = UIs._buttonNormalTexture;
                UIs._categoryTabActiveTexture = UIs._buttonActiveTexture;
                UIs._categoryTabActiveTexture = UIs._buttonActiveTexture;

                UIs._settingsIconTexture = new Texture2D(16, 16);
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

                UIs._settingsIconTexture.SetPixels(pixels);
                UIs._settingsIconTexture.Apply();

            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error creating textures: {ex.Message}");
            }
        }
        public void InitializeStyles()
        {
            try
            {
                UIs._customSkin = ScriptableObject.CreateInstance<GUISkin>();
                UIs._customSkin.box = new GUIStyle(GUI.skin.box);
                UIs._customSkin.button = new GUIStyle(GUI.skin.button);
                UIs._customSkin.label = new GUIStyle(GUI.skin.label);
                UIs._customSkin.textField = new GUIStyle(GUI.skin.textField);
                UIs._customSkin.toggle = new GUIStyle(GUI.skin.toggle);
                UIs._customSkin.window = new GUIStyle(GUI.skin.window);
                UIs._customSkin.horizontalSlider = new GUIStyle(GUI.skin.horizontalSlider);
                UIs._customSkin.horizontalSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb);

                // Window style
                UIs._windowStyle = new GUIStyle();
                UIs._windowStyle.normal.background = UIs._backgroundTexture;
                UIs._windowStyle.border = new RectOffset(10, 10, 10, 10);
                UIs._windowStyle.padding = new RectOffset(0, 0, 0, 0);
                UIs._windowStyle.margin = new RectOffset(0, 0, 0, 0);

                // Panel style
                UIs._panelStyle = new GUIStyle(GUI.skin.box);
                UIs._panelStyle.normal.background = UIs._panelTexture;
                UIs._panelStyle.border = new RectOffset(8, 8, 8, 8);
                UIs._panelStyle.margin = new RectOffset(10, 10, 10, 10);
                UIs._panelStyle.padding = new RectOffset(10, 10, 10, 10);

                // Title style
                UIs._titleStyle = new GUIStyle(GUI.skin.label);
                UIs._titleStyle.fontSize = 20;
                UIs._titleStyle.fontStyle = FontStyle.Bold;
                UIs._titleStyle.normal.textColor = UIs._textColor;
                UIs._titleStyle.alignment = TextAnchor.MiddleCenter;
                UIs._titleStyle.margin = new RectOffset(0, 0, 10, 15);

                // Header style
                UIs._headerStyle = new GUIStyle(GUI.skin.box);
                UIs._headerStyle.normal.background = UIs._backgroundTexture;
                UIs._headerStyle.border = new RectOffset(2, 2, 2, 2);
                UIs._headerStyle.margin = new RectOffset(0, 0, 0, 10);
                UIs._headerStyle.padding = new RectOffset(10, 10, 8, 8);
                UIs._headerStyle.fontSize = 14;
                UIs._headerStyle.fontStyle = FontStyle.Bold;
                UIs._headerStyle.normal.textColor = UIs._textColor;

                // Button style
                UIs._buttonStyle = new GUIStyle(GUI.skin.button);
                UIs._buttonStyle.normal.background = UIs._buttonNormalTexture;
                UIs._buttonStyle.hover.background = UIs._buttonHoverTexture;
                UIs._buttonStyle.active.background = UIs._buttonActiveTexture;
                UIs._buttonStyle.focused.background = UIs._buttonNormalTexture;
                UIs._buttonStyle.normal.textColor = UIs._textColor;
                UIs._buttonStyle.hover.textColor = Color.white;
                UIs._buttonStyle.fontSize = 12;
                UIs._buttonStyle.alignment = TextAnchor.MiddleCenter;
                UIs._buttonStyle.margin = new RectOffset(5, 5, 2, 2);
                UIs._buttonStyle.padding = new RectOffset(10, 10, 6, 6);
                UIs._buttonStyle.border = new RectOffset(6, 6, 6, 6);

                // Icon button style
                UIs._iconButtonStyle = new GUIStyle(UIs._buttonStyle);
                UIs._iconButtonStyle.padding = new RectOffset(6, 6, 6, 6);

                // Search box style
                UIs._searchBoxStyle = new GUIStyle(GUI.skin.textField);
                UIs._searchBoxStyle.fontSize = 14;
                UIs._searchBoxStyle.margin = new RectOffset(0, 0, 5, 10);

                // Label style
                UIs._labelStyle = new GUIStyle(GUI.skin.label);
                UIs._labelStyle.fontSize = 12;
                UIs._labelStyle.normal.textColor = UIs._textColor;
                UIs._labelStyle.alignment = TextAnchor.MiddleLeft;
                UIs._labelStyle.padding = new RectOffset(5, 5, 2, 2);

                // Category styles
                UIs._categoryButtonStyle = new GUIStyle(UIs._buttonStyle);
                UIs._categoryButtonActiveStyle = new GUIStyle(UIs._buttonStyle);
                UIs._categoryButtonActiveStyle.normal.background = UIs._buttonActiveTexture;

                // Item button style
                UIs._itemButtonStyle = new GUIStyle(UIs._buttonStyle);
                UIs._itemSelectedStyle = new GUIStyle(UIs._itemButtonStyle);
                UIs._itemSelectedStyle.normal.background = UIs._buttonActiveTexture;

                UIs._stylesInitialized = true;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error initializing styles: {ex}");
            }
        }

        #endregion
    }
}
