using MelonLoader;
using static Modern_Cheat_Menu.Core;
using Modern_Cheat_Menu.Library;
using Modern_Cheat_Menu.Model;
using Modern_Cheat_Menu.ModGUI;
using Il2CppScheduleOne.Map;
using System.Collections;
using UnityEngine;

namespace Modern_Cheat_Menu.Features
{
    public class Teleporter
    {
        #region Teleporter
        private GameObject mapTeleportObject;
        public Texture2D _mapTexture;
        private EMapRegion _hoveredRegion = EMapRegion.Downtown; // Default value
        private Dictionary<EMapRegion, Color> _regionColors = new Dictionary<EMapRegion, Color>();
        private Dictionary<EMapRegion, Rect> _regionRects = new Dictionary<EMapRegion, Rect>();
        private Dictionary<string, Vector3> _predefinedTeleports = new Dictionary<string, Vector3>();
        private Rect _mapRect;
        private Vector2 _lastClickPosition = Vector2.zero;
        private Vector2 _dragStartOffset;
        private Vector2 _dragStartPos;
        public Vector2 _mapPanOffset = Vector2.zero;
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
        

        public void DrawTeleportManager()
        {
            try
            {
                float windowWidth = UIs._windowRect.width - 40f;
                float windowHeight = UIs._windowRect.height - 150f;

                // ADDED: Current player coordinates - placed below the tabs, centered
                var localPlayer = GameplayUtils.FindLocalPlayer();
                Vector3 currentPos = Vector3.zero;

                if (localPlayer != null)
                {
                    currentPos = localPlayer.transform.position;

                    // Create a style for coordinates
                    GUIStyle coordStyle = new GUIStyle(UIs._labelStyle);
                    coordStyle.alignment = TextAnchor.MiddleCenter;
                    coordStyle.fontSize = 14;
                    coordStyle.fontStyle = FontStyle.Bold;

                    // Draw position text directly below tabs
                    Rect coordRect = new Rect(0, 90, UIs._windowRect.width, 25);

                    // Format the coordinate text
                    string coordText = $"Current Position: X: {currentPos.x:F1}, Y: {currentPos.y:F1}, Z: {currentPos.z:F1}";

                    // Draw the text centered
                    GUI.Label(coordRect, coordText, coordStyle);
                }

                // Make the map smaller to accommodate coordinate controls in middle
                float mapHeight = windowHeight * 0.45f; // Reduced from 0.6f
                _mapRect = new Rect(20, 115, windowWidth, mapHeight);
                GUI.Box(_mapRect, "", UIs._panelStyle);

                // Draw the map
                DrawInteractiveMap(_mapRect);

                // Check if we need to initialize the map - ADD AUTO LOADING
                if (!_mapInitialized && !_isCapturingMap)
                {
                    // Start map capture automatically
                    MelonCoroutines.Start(CaptureMapCoroutine());
                    _isCapturingMap = true;
                    ModLogger.Info("Auto-starting map capture");
                }

                // MIDDLE SECTION - XYZ Controls
                float middleSectionHeight = 80;
                Rect middleSectionRect = new Rect(20, 115 + mapHeight + 10, windowWidth, middleSectionHeight);
                GUI.Box(middleSectionRect, "", UIs._panelStyle);

                // X, Y, Z input fields - now moved to the middle and made more compact
                float inputWidth = (windowWidth - 100) / 3;
                float inputY = middleSectionRect.y + 10;

                // X field
                GUI.Label(
                    new Rect(middleSectionRect.x + 10, inputY + 5, 20, 25),
                    "X:",
                    UIs._labelStyle
                );

                if (!ModData._textFields.TryGetValue("teleport_x", out var xField))
                {
                    xField = new CustomTextField(localPlayer != null ? currentPos.x.ToString("F1") : "0",
                                                 UIs._inputFieldStyle ?? GUI.skin.textField);
                    ModData._textFields["teleport_x"] = xField;
                }
                xField.Draw(new Rect(middleSectionRect.x + 30, inputY, inputWidth, 25));

                // Y field
                GUI.Label(
                    new Rect(middleSectionRect.x + inputWidth + 40, inputY + 5, 20, 25),
                    "Y:",
                    UIs._labelStyle
                );

                if (!ModData._textFields.TryGetValue("teleport_y", out var yField))
                {
                    yField = new CustomTextField(localPlayer != null ? currentPos.y.ToString("F1") : "0",
                                                 UIs._inputFieldStyle ?? GUI.skin.textField);
                    ModData._textFields["teleport_y"] = yField;
                }
                yField.Draw(new Rect(middleSectionRect.x + inputWidth + 60, inputY, inputWidth, 25));

                // Z field
                GUI.Label(
                    new Rect(middleSectionRect.x + inputWidth * 2 + 70, inputY + 5, 20, 25),
                    "Z:",
                    UIs._labelStyle
                );

                if (!ModData._textFields.TryGetValue("teleport_z", out var zField))
                {
                    zField = new CustomTextField(localPlayer != null ? currentPos.z.ToString("F1") : "0",
                                                 UIs._inputFieldStyle ?? GUI.skin.textField);
                    ModData._textFields["teleport_z"] = zField;
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
                    UIs._buttonStyle))
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
                        ModLogger.Error($"Error parsing coordinates: {ex.Message}");
                        Notifier.ShowNotification("Error", "Invalid coordinates", NotificationSystem.NotificationType.Error);
                    }
                }

                // Update position button - simplified to use current coordinates directly
                if (GUI.Button(
                    new Rect(startX + buttonWidth + buttonSpacing, buttonY, buttonWidth, 25),
                    "Use Current Position",
                    UIs._buttonStyle) && localPlayer != null)
                {
                    // Copy to input fields
                    if (ModData._textFields.TryGetValue("teleport_x", out var xF))
                        xF.Value = currentPos.x.ToString("F1");

                    if (ModData._textFields.TryGetValue("teleport_y", out var yF))
                        yF.Value = currentPos.y.ToString("F1");

                    if (ModData._textFields.TryGetValue("teleport_z", out var zF))
                        zF.Value = currentPos.z.ToString("F1");

                    Notifier.ShowNotification("Position", "Current position updated in input fields", NotificationSystem.NotificationType.Info);
                }

                // Predefined teleports - takes bottom portion, now smaller with more space for the middle section
                float teleportListHeight = windowHeight - mapHeight - middleSectionHeight - 40;
                Rect teleportListRect = new Rect(20, middleSectionRect.y + middleSectionHeight + 10, windowWidth, teleportListHeight);
                GUI.Box(teleportListRect, "", UIs._panelStyle);

                // Title and refresh button in single row
                GUI.Label(
                    new Rect(teleportListRect.x + 10, teleportListRect.y + 5, 200, 25),
                    "Predefined Teleports",
                   UIs._commandLabelStyle ?? UIs._labelStyle
                );

                // Refresh button
                if (GUI.Button(
                    new Rect(teleportListRect.x + teleportListRect.width - 100, teleportListRect.y + 5, 80, 25),
                    "Refresh",
                    UIs._buttonStyle))
                {
                    InitializePredefinedTeleports();
                    Notifier.ShowNotification("Teleport", "Teleport locations refreshed", NotificationSystem.NotificationType.Info);
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
                UIs._scrollPosition = GUI.BeginScrollView(
                    teleportButtonsRect,
                    UIs._scrollPosition,
                    new Rect(0, 0, teleportButtonsRect.width - 20, _predefinedTeleports.Count * 30)
                );

                int i = 0;
                foreach (var teleport in _predefinedTeleports)
                {
                    Rect buttonRect = new Rect(5, i * 30, teleportButtonsRect.width - 30, 25);
                    if (GUI.Button(buttonRect, teleport.Key, UIs._buttonStyle))
                    {
                        TeleportPlayer(teleport.Value);
                        Notifier.ShowNotification("Teleport", $"Teleported to {teleport.Key}", NotificationSystem.NotificationType.Success);
                    }
                    i++;
                }

                GUI.EndScrollView();
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error in DrawTeleportManager: {ex.Message}");
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

            ModLogger.Error($"No ground detected at position {xzPosition}, using fallback height");
            return 3f; // Or some other safe default height for your game world
        }

        public void TeleportPlayer(Vector3 position)
        {
            try
            {
                var localPlayer = GameplayUtils.FindLocalPlayer();
                if (localPlayer == null)
                {
                    ModLogger.Error("Failed to find local player for teleportation!");
                    Notifier.ShowNotification("Teleport", "Player not found", NotificationSystem.NotificationType.Error);
                    return;
                }

                // Directly set player position
                localPlayer.transform.position = position;

                // Also update text fields with new position for reference
                if (ModData._textFields.TryGetValue("teleport_x", out var xField))
                    xField.Value = position.x.ToString("F1");
                if (ModData._textFields.TryGetValue("teleport_y", out var yField))
                    yField.Value = position.y.ToString("F1");
                if (ModData._textFields.TryGetValue("teleport_z", out var zField))
                    zField.Value = position.z.ToString("F1");

                Notifier.ShowNotification("Teleport", "Teleported successfully", NotificationSystem.NotificationType.Success);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error teleporting player: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to teleport player", NotificationSystem.NotificationType.Error);
            }
        }

        //// Add this method to your OnInitializeMelon or SetupUI method
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
                        "Capture Map", UIs._buttonStyle))
                    {
                        MelonCoroutines.Start(CaptureMapCoroutine());
                        _isCapturingMap = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error in DrawInteractiveMap: {ex.Message}");
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
                GUI.Box(scaledRect, "", UIs._panelStyle ?? GUI.skin.box);
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
                var localPlayer = GameplayUtils.FindLocalPlayer();
                if (localPlayer == null)
                {
                    ModLogger.Error("Could not find local player!");
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
                ModLogger.Error($"Error converting map coordinates: {ex.Message}");
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
                        if (player != null && !GameplayUtils.IsLocalPlayer(player))
                        {
                            _predefinedTeleports[$"Player: {player.name}"] = player.transform.position;
                        }
                    }
                }

                ModLogger.Info($"Initialized {_predefinedTeleports.Count} predefined teleport locations");
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error initializing predefined teleports: {ex.Message}");
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

            if (GUI.Button(zoomInRect, "+", UIs._buttonStyle ?? GUI.skin.button))
            {
                _mapZoom = Mathf.Clamp(_mapZoom + 0.1f, 1.0f, _maxZoom);
            }

            // Disable zoom out button if already at default zoom
            GUI.enabled = _mapZoom > 1.0f;
            if (GUI.Button(zoomOutRect, "-", UIs._buttonStyle ?? GUI.skin.button))
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

            if (GUI.Button(resetRect, "Reset View", UIs._buttonStyle ?? GUI.skin.button))
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
                    ModLogger.Error("Map instance not found!");
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
                ModLogger.Error($"Error capturing map: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }

            _isCapturingMap = false;
        }

        public Texture2D CreateReadableTexture(Texture sourceTexture)
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
                ModLogger.Error($"Error creating readable texture: {ex.Message}");
                return null;
            }
        }
        #endregion


        // Teleport all players to the local player's position
        public static void TeleportAllPlayersToMe()
        {
            try
            {
                var localPlayer = GameplayUtils.FindLocalPlayer();
                if (localPlayer == null)
                {
                    Notifier.ShowNotification("Teleport", "Local player not found", NotificationSystem.NotificationType.Error);
                    return;
                }

                Vector3 myPosition = localPlayer.transform.position;

                // Count teleported players
                int teleportCount = 0;

                foreach (var playerInfo in PlayerCache._onlinePlayers)
                {
                    if (playerInfo == null || playerInfo.Player == null || playerInfo.IsLocal)
                        continue;

                    // Teleport remote player to slightly offset position
                    Vector3 offset = new Vector3(
                        UnityEngine.Random.Range(-1.5f, 1.5f),
                        0,
                        UnityEngine.Random.Range(-1.5f, 1.5f)
                    );

                    playerInfo.Player.transform.position = myPosition + offset;
                    teleportCount++;
                }

                if (teleportCount > 0)
                {
                    Notifier.ShowNotification("Teleport", $"Teleported {teleportCount} players to your position", NotificationSystem.NotificationType.Success);
                }
                else
                {
                    Notifier.ShowNotification("Teleport", "No other players to teleport", NotificationSystem.NotificationType.Info);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Error teleporting all players: {ex.Message}");
                Notifier.ShowNotification("Error", "Failed to teleport players", NotificationSystem.NotificationType.Error);
            }
        }
    }
}
