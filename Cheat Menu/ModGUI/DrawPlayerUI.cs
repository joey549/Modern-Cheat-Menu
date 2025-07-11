using Il2CppScheduleOne.PlayerScripts.Health;
using static Modern_Cheat_Menu.Core;
using Modern_Cheat_Menu.Commands;
using Modern_Cheat_Menu.Features;
using Modern_Cheat_Menu.Library;
using Modern_Cheat_Menu.Model;
using System.Collections;
using UnityEngine;

namespace Modern_Cheat_Menu.ModGUI
{
    internal class DrawPlayerUI
    {
        public static void DrawPlayerExploitsUI(CommandCore.CommandCategory category)
        {
            try
            {
                float windowWidth = UIs._windowRect.width - 40f;
                float windowHeight = UIs._windowRect.height - 150f;

                // Player list panel at the top
                float playerPanelHeight = 120f; // Fixed height for player panel

                // Draw player list at the top
                Rect playerListRect = new Rect(20, 100, windowWidth, playerPanelHeight);
                GUI.Box(playerListRect, "", UIs._panelStyle);

                // Player list header
                GUI.Label(
                new Rect(playerListRect.x + 10, playerListRect.y + 10, 150, 20),
                    "Players Online",
                    UIs._commandLabelStyle ?? UIs._labelStyle
                );

                // Draw player entries in a horizontal layout
                DrawPlayerListHorizontal(playerListRect);

                // Commands section below player list
                UIs._scrollPosition = GUI.BeginScrollView(
                    new Rect(20, 100 + playerPanelHeight + 10, windowWidth, windowHeight - playerPanelHeight - 10),
                    UIs._scrollPosition,
                    new Rect(0, 0, windowWidth - 20, category.Commands.Count * 100f)
                );

                float yOffset = 0f;

                // Draw the standard commands
                foreach (var command in category.Commands)
                {
                    // Command container rectangle
                    Rect commandRect = new Rect(0, yOffset, windowWidth - 40f, 90f);
                    GUI.Box(commandRect, "", UIs._panelStyle);

                    // Command Name
                    GUI.Label(
                    new Rect(commandRect.x + 10f, commandRect.y + 5f, 200f, 25f),
                        command.Name,
                        UIs._commandLabelStyle ?? UIs._labelStyle
                    );

                    // Parameters handling
                    float paramX = commandRect.x + 220f;
                    if (command.Parameters.Count > 0)
                    {
                        foreach (var param in command.Parameters)
                        {
                            Rect paramRect = new Rect(paramX, commandRect.y + 5f, 120f, 25f);

                            if (param.Type == CommandCore.ParameterType.Input)
                            {
                                // Unique key for each parameter
                                string paramKey = $"param_{command.Name}_{param.Name}";

                                // Custom text field
                                if (!ModData._textFields.TryGetValue(paramKey, out var textField))
                                {
                                    textField = new CustomTextField(param.Value ?? "", UIs._inputFieldStyle ?? GUI.skin.textField);
                                    ModData._textFields[paramKey] = textField;
                                }

                                param.Value = textField.Draw(paramRect);
                            }
                            else if (param.Type == CommandCore.ParameterType.Dropdown)
                            {
                                // Dropdown-like button
                                if (GUI.Button(paramRect, param.Value ?? "Select", UIs._buttonStyle))
                                {
                                    DrawCoreUI.ShowDropdownMenu(param, commandRect);
                                }
                            }

                            paramX += 130f;
                        }
                    }

                    // Execute Button
                    Rect executeRect = new Rect(paramX, commandRect.y + 5f, 120f, 25f);
                    if (GUI.Button(executeRect, "Execute", UIs._buttonStyle))
                    {
                        CommandCore.ExecuteCommand(command);
                    }

                    // Optional description
                    if (!string.IsNullOrEmpty(command.Description))
                    {
                        GUI.Label(
                            new Rect(commandRect.x + 10f, commandRect.y + 35f, windowWidth - 60f, 50f),
                            command.Description,
                            UIs._tooltipStyle ?? GUI.skin.label
                        );
                    }

                    // Increment Y offset for next command
                    yOffset += 100f;
                }

                GUI.EndScrollView();
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error in DrawPlayerExploitsUI: {ex}");
            }
        }

        public static void CreateButtonTextures()
        {
            // Create settings button texture (gear icon) - SMALLER (18x18)
            UIs._settingsButtonTexture = new Texture2D(18, 18, TextureFormat.RGBA32, false);
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
            UIs._settingsButtonTexture.SetPixels(settingsPixels);
            UIs._settingsButtonTexture.Apply();
            // Create close button texture (X icon) - SMALLER (18x18)
            UIs._closeButtonTexture = new Texture2D(15, 15, TextureFormat.RGBA32, false);
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
            UIs._closeButtonTexture.SetPixels(closePixels);
            UIs._closeButtonTexture.Apply();
        }

        public static void DrawHeaderWithTexturedButtons()
        {
            // Header background
            Rect headerRect = new Rect(0, 0, UIs._windowRect.width, 40);
            GUI.Box(headerRect, "", UIs._headerStyle ?? GUI.skin.box);

            // Title
            Rect titleRect = new Rect(headerRect.x + 10, headerRect.y, headerRect.width - 80, headerRect.height);
            GUI.Label(titleRect, ModInfo.Name, UIs._titleStyle ?? GUI.skin.label);

            // Settings button - using custom texture
            Rect settingsRect = new Rect(headerRect.width - 70, headerRect.y + 5, 30, 30);
            if (UIs._settingsButtonTexture != null && GUI.Button(settingsRect, UIs._settingsButtonTexture, GUIStyle.none))
            {
                UIs._showSettings = true;
            }

            // Close button - using custom texture
            Rect closeRect = new Rect(headerRect.width - 35, headerRect.y + 5, 30, 30);
            if (UIs._closeButtonTexture != null && GUI.Button(closeRect, UIs._closeButtonTexture, GUIStyle.none))
            {
                ModSetting.ToggleUI(false);
            }
        }
        public static void DrawPlayerListHorizontal(Rect containerRect)
        {
            try
            {
                var playerList = Il2CppScheduleOne.PlayerScripts.Player.PlayerList;
                if (playerList == null || playerList.Count == 0)
                {
                    GUI.Label(
                        new Rect(containerRect.x + 10, containerRect.y + 30, 200, 25),
                        "No players found",
                        UIs._labelStyle
                    );
                    return;
                }

                float contentY = containerRect.y + 40;

                foreach (var player in playerList)
                {
                    if (player == null) continue;

                    bool isLocal = GameplayUtils.IsLocalPlayer(player);
                    var playerHealth = GameplayUtils.GetPlayerHealth(player);

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
                        GUIStyle healthStyle = new GUIStyle(UIs._labelStyle);
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
                    var playerInfo = PlayerCache._onlinePlayers.FirstOrDefault(p => p.Player == player);
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
                                    ExplosionManager.StartExplodeLoop(playerInfo);
                                else
                                    ExplosionManager.StopExplodeLoop(playerInfo);
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

                if (GUI.Button(explodeLoopAllRect, "Explode Loop All", UIs._buttonStyle))
                {
                    foreach (var playerInfo in PlayerCache._onlinePlayers)
                    {
                        if (!playerInfo.IsLocal)
                        {
                            playerInfo.ExplodeLoop = true;
                            ExplosionManager.StartExplodeLoop(playerInfo);
                        }
                    }
                    Notifier.ShowNotification("Players", "Started explosion loop on all players", NotificationSystem.NotificationType.Warning);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error drawing horizontal player list: {ex.Message}");
            }
        }
        public IEnumerator RecreateTexturesAfterDeath()
        {
            // Wait a short moment to ensure any cleanup from death has finished
            yield return new WaitForSeconds(0.5f);

            // Recreate textures and styles
            if (UIs._isInitialized)
            {
                Styler.CreateTextures();
                Styler.InitializeStyles();

                UIs._needsTextureRecreation = false;
                UIs._needsStyleRecreation = false;

                UIs._stylesInitialized = true;
            }
        }
    }
}
