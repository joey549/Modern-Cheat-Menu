using Il2CppScheduleOne.PlayerScripts.Health;
using static Modern_Cheat_Menu.Core;
using Modern_Cheat_Menu.Commands;
using Modern_Cheat_Menu.Features;
using Modern_Cheat_Menu.Library;
using Modern_Cheat_Menu.Model;
using UnityEngine;

namespace Modern_Cheat_Menu.ModGUI
{
    public class DrawPlayerMap
    {
        public static bool _showPlayerMap = false;
        public static Texture2D _playerMapTexture;
        public static bool _playerMapInitialized = false;
        public static float _playerMapZoom = 1.0f;
        public static Vector2 _playerMapPanOffset = Vector2.zero;
        public static bool _isDraggingPlayerMap = false;
        public static Vector2 _playerMapDragStart;
        public static Vector2 _playerMapDragStartOffset;
        public static Dictionary<string, Color> _playerColors = new Dictionary<string, Color>();

        public static void DrawOnlinePlayers()
        {
            try
            {
                float windowWidth = UIs._windowRect.width - 40f;
                float windowHeight = UIs._windowRect.height - 150f;

                // Header with refresh button and map toggle
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Refresh Players", UIs._buttonStyle, GUILayout.Width(150), GUILayout.Height(30)))
                {
                    PlayerCache.RefreshOnlinePlayers();
                    PlayerCache._lastPlayerRefreshTime = Time.time;
                    Notifier.ShowNotification("Online", "Player list refreshed", NotificationSystem.NotificationType.Info);
                }

                // Map toggle button
                bool showMap = GUILayout.Toggle(_showPlayerMap, "Show Map", UIs._toggleButtonStyle ?? GUI.skin.toggle, GUILayout.Width(100));
                if (showMap != _showPlayerMap)
                {
                    _showPlayerMap = showMap;
                    if (_showPlayerMap)
                        RefreshPlayerMapPositions();
                }

                // Total players count display
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Total Players: {PlayerCache._onlinePlayers.Count}", UIs._labelStyle, GUILayout.Width(150));
                GUILayout.EndHorizontal();

                GUILayout.Space(5); // Reduced spacing

                // Player Grid Layout
                // Check for refresh time
                if (Time.time - PlayerCache._lastPlayerRefreshTime > PlayerCache.PLAYER_REFRESH_INTERVAL)
                {
                    PlayerCache.RefreshOnlinePlayers();
                    PlayerCache._lastPlayerRefreshTime = Time.time;
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
                int totalRows = Mathf.CeilToInt((float)PlayerCache._onlinePlayers.Count / playersPerRow);
                float contentHeight = totalRows * (playerCardHeight + 10);

                // Create player list scroll view
                UIs._playerScrollPosition = GUILayout.BeginScrollView(
                    UIs._playerScrollPosition,
                    GUILayout.Height(Mathf.Min(contentHeight + 10, playerListMaxHeight))
                );

                if (PlayerCache._onlinePlayers.Count == 0)
                {
                    GUILayout.Label("No players found. Try refreshing the list.", UIs._labelStyle);
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    int playerIndex = 0;

                    foreach (var playerInfo in PlayerCache._onlinePlayers)
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
                        GUI.Box(cardRect, "", UIs._panelStyle);

                        // Player name header with status indicator
                        Rect headerRect = new Rect(cardRect.x + 5, cardRect.y + 5, cardRect.width - 10, 25);
                        GUI.color = playerInfo.IsLocal ? new Color(0.2f, 0.7f, 1f) : Color.white;

                        GUIStyle nameStyle = new GUIStyle(UIs._commandLabelStyle ?? UIs._labelStyle);
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
                        GUIStyle statusStyle = new GUIStyle(UIs._labelStyle);
                        statusStyle.normal.textColor = statusColor;
                        statusStyle.fontStyle = FontStyle.Bold;
                        statusStyle.alignment = TextAnchor.MiddleCenter;
                        GUI.Label(statusRect, statusText, statusStyle);

                        // Health bar if available
                        if (playerInfo.Health != null)
                        {
                            Rect healthLabelRect = new Rect(cardRect.x + 5, cardRect.y + 50, 50, 20);
                            GUI.Label(healthLabelRect, "Health:", UIs._labelStyle);

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
                        GUIStyle smallTextStyle = new GUIStyle(UIs._labelStyle);
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
                            if (GUI.Button(new Rect(cardRect.x + 5, buttonY, buttonWidth, 20), "Kill", UIs._buttonStyle))
                            {
                                PlayerCommand.ServerExecuteKillPlayer(playerInfo.Player);
                                Notifier.ShowNotification("Player", $"Killed {playerInfo.Name}", NotificationSystem.NotificationType.Success);
                            }

                            if (GUI.Button(new Rect(cardRect.x + 10 + buttonWidth, buttonY, buttonWidth, 20), "Explode", UIs._buttonStyle))
                            {
                                ExplosionManager.CreateServerSideExplosion(playerInfo.Player.transform.position, 100f, 5f);
                                Notifier.ShowNotification("Player", $"Exploded {playerInfo.Name}", NotificationSystem.NotificationType.Success);
                            }

                            // Second row - Teleport and Explosion Loop
                            float button2Y = buttonY + 25;
                            if (GUI.Button(new Rect(cardRect.x + 5, button2Y, buttonWidth, 20), "Teleport To", UIs._buttonStyle))
                            {
                                TeleportS.TeleportPlayer(playerInfo.Player.transform.position);
                                Notifier.ShowNotification("Player", $"Teleported to {playerInfo.Name}", NotificationSystem.NotificationType.Success);
                            }

                            bool newLoopState = GUI.Toggle(
                                new Rect(cardRect.x + 10 + buttonWidth, button2Y, buttonWidth, 20),
                                playerInfo.ExplodeLoop,
                                "Loop",
                                UIs._toggleButtonStyle ?? GUI.skin.toggle
                            );

                            if (newLoopState != playerInfo.ExplodeLoop)
                            {
                                playerInfo.ExplodeLoop = newLoopState;
                                if (newLoopState)
                                {
                                    ExplosionManager.StartExplodeLoop(playerInfo);
                                    Notifier.ShowNotification("Player", $"Started explosion loop on {playerInfo.Name}", NotificationSystem.NotificationType.Warning);
                                }
                            }
                        }

                        GUILayout.EndVertical();
                        GUILayout.Space(10);
                        playerIndex++;
                    }

                    // Fill empty slots in the last row for better alignment
                    for (int i = 0; i < playersPerRow - (PlayerCache._onlinePlayers.Count % playersPerRow); i++)
                    {
                        if (PlayerCache._onlinePlayers.Count % playersPerRow == 0) break;
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
                    GUIStyle mapTitleStyle = new GUIStyle(UIs._titleStyle ?? UIs._labelStyle);
                    mapTitleStyle.alignment = TextAnchor.MiddleCenter;
                    GUILayout.Label("Player Positions", mapTitleStyle);

                    // Fixed height for map
                    float mapHeight = 200f;
                    Rect mapContainerRect = GUILayoutUtility.GetRect(UIs._windowRect.width - 40, mapHeight);
                    GUI.Box(mapContainerRect, "", UIs._panelStyle);

                    // Draw the map with proper integration
                    DrawPlayerPositionsMap(mapContainerRect);

                    GUILayout.EndVertical();
                }

                GUILayout.Space(5); // Reduced spacing

                // Global actions row at the bottom - always visible
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Kill All", UIs._buttonStyle, GUILayout.Height(30)))
                {
                    PlayerCommand.KillAllPlayersCommand(null);
                }

                if (GUILayout.Button("Explode All", UIs._buttonStyle, GUILayout.Height(30)))
                {
                    ExplosionManager.CreateExplosion(new string[] { "all", "99999999999999", "2" });
                }

                if (GUILayout.Button("Teleport All To Me", UIs._buttonStyle, GUILayout.Height(30)))
                {
                    Teleporter.TeleportAllPlayersToMe();
                }

                if (GUILayout.Button("Increase Lobby Size", UIs._buttonStyle, GUILayout.Height(30)))
                {
                    GameUtils.ApplyLobbyPatch();
                }

                GUILayout.EndHorizontal();
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error in DrawOnlinePlayers: {ex.Message}");
            }
        }
        public static void DrawPlayerPositionsMap(Rect mapRect)
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
                    foreach (var playerInfo in PlayerCache._onlinePlayers)
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
                        "Initialize Map", UIs._buttonStyle))
                    {
                        InitializePlayerMap();
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error drawing player map: {ex.Message}");
            }
        }

        public static void HandlePlayerMapInteraction(Rect mapContainer, Rect displayRect, Rect baseRect)
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

        public static Rect ApplyPlayerMapZoomAndPan(Rect baseRect, Rect containerRect)
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

        public static void DrawPlayerMapMarker(Vector2 position, Color color, bool isLocal, string playerName)
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
                    GUIStyle tooltipStyle = new GUIStyle(UIs._labelStyle);
                    tooltipStyle.normal.textColor = Color.white;
                    tooltipStyle.alignment = TextAnchor.MiddleCenter;
                    tooltipStyle.fontSize = 12;

                    GUI.Label(tooltipRect, playerName, tooltipStyle);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error drawing player marker: {ex.Message}");
            }
        }

        // Initialize player map (using the existing map if available)
        public static void InitializePlayerMap()
        {
            try
            {
                // Try to use existing map texture if available
                if (TeleportS._mapTexture != null)
                {
                    _playerMapTexture = TeleportS._mapTexture;
                    _playerMapInitialized = true;
                    return;
                }

                // Otherwise try to capture from the game
                var mapApp = Resources.FindObjectsOfTypeAll<Il2CppScheduleOne.UI.Phone.Map.MapApp>()
                    .FirstOrDefault();

                if (mapApp != null && mapApp.MainMapSprite != null && mapApp.MainMapSprite.texture != null)
                {
                    _playerMapTexture = TeleportS.CreateReadableTexture(mapApp.MainMapSprite.texture);
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
                ModLogger.Error($"Error initializing player map: {ex.Message}");
            }
        }

        // Helper method to refresh player map positions
        public static void RefreshPlayerMapPositions()
        {
            // Just refresh player list
            PlayerCache.RefreshOnlinePlayers();

            // Ensure map is initialized
            if (!_playerMapInitialized)
            {
                InitializePlayerMap();
            }

            // Assign unique colors to new players
            foreach (var player in PlayerCache._onlinePlayers)
            {
                if (player != null && !_playerColors.ContainsKey(player.SteamID))
                {
                    _playerColors[player.SteamID] = GetUniqueColor(player.SteamID);
                }
            }
        }

        // Convert world position to map position
        public static Vector2 WorldToMapPosition(Vector3 worldPos)
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
                ModLogger.Error($"Error converting world to map position: {ex.Message}");
                return new Vector2(0.5f, 0.5f); // Default to center of map
            }
        }

        // Draw map legend

        public static void DrawPlayerMapLegend(Rect mapRect)
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
                ModLogger.Error($"Error drawing map legend: {ex.Message}");
            }
        }


        // Improved zoom controls
        public static void DrawPlayerMapZoomControls(Rect mapRect)
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
                    UIs._buttonStyle))
                {
                    _playerMapZoom = 1.0f;
                    TeleportS._mapPanOffset = Vector2.zero;
                }

                // Zoom buttons
                if (GUI.Button(
                    new Rect(controlsRect.x + 55, controlsRect.y + 30, 35, 25),
                    "+",
                    UIs._buttonStyle))
                {
                    _playerMapZoom = Mathf.Clamp(_playerMapZoom + 0.2f, 1.0f, 3.0f);
                }

                GUI.enabled = _playerMapZoom > 1.0f;
                if (GUI.Button(
                    new Rect(controlsRect.x + 10, controlsRect.y + 30, 35, 25),
                    "-",
                    UIs._buttonStyle))
                {
                    _playerMapZoom = Mathf.Clamp(_playerMapZoom - 0.2f, 1.0f, 3.0f);
                }
                GUI.enabled = true;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error drawing zoom controls: {ex.Message}");
            }
        }


        // Generate a unique color based on player ID
        public static Color GetUniqueColor(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return Color.gray;

            // If the player is local, use green
            var localPlayer = GameplayUtils.FindLocalPlayer();
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
    }
}
