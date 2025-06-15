using static Modern_Cheat_Menu.Core;
using Modern_Cheat_Menu.Commands;
using Modern_Cheat_Menu.Library;
using Modern_Cheat_Menu.Model;
using UnityEngine;


namespace Modern_Cheat_Menu.ModGUI
{
    public class DrawCommandUI
    {
        public static void DrawCommandCategory(CommandCore.CommandCategory category)
        {
            try
            {
                // Special handling for Player Exploits category
                if (category.Name == "Player Exploits")
                {
                    DrawPlayerUI.DrawPlayerExploitsUI(category);
                    return;
                }

                // Original implementation for other categories
                float windowWidth = UIs._windowRect.width - 40f;

                // Manual scroll view setup
                UIs._scrollPosition = GUI.BeginScrollView(
                    new Rect(20, 100, windowWidth, UIs._windowRect.height - 150),
                    UIs._scrollPosition,
                    new Rect(0, 0, windowWidth - 20, category.Commands.Count * 100f)
                );

                float yOffset = 0f;
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
                                    DrawCoreUI.ShowDropdownMenu(param);
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
                ModLogger.Error($"Error in DrawCommandCategory: {ex}");
            }
        }

        public static void DrawItemManager()
        {
            try
            {
                float windowWidth = UIs._windowRect.width - 40f;
                float windowHeight = UIs._windowRect.height - 150f;

                // Vertical offset to move everything down
                float verticalOffset = 90f;

                // Add a header panel for the options
                Rect headerRect = new Rect(20f, 20f + verticalOffset, windowWidth - 40f, 50f);
                GUI.Box(headerRect, "", UIs._panelStyle);

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

                if (!ModData._itemCache.TryGetValue("qualities", out var qualities)) return;
                if (GUI.Button(setQualityRect, "Set Quality", UIs._buttonStyle))
                {
                    // Convert selected quality index to enum value
                    var quality = (Il2CppScheduleOne.ItemFramework.EQuality)ModStateS._selectedQualityIndex;
                    SpawnCommand.SetItemQuality(quality);
                }

                // Package Item Button (second in the row)
                Rect packageItemRect = new Rect(
                    setQualityRect.x + buttonWidth + buttonSpacing,
                setQualityRect.y,
                buttonWidth,
                    buttonHeight
                );

                if (GUI.Button(packageItemRect, "Package Item", UIs._buttonStyle))
                {
                    SpawnCommand.PackageProductCommand(PlayerCache._packageType);
                }

                // Package Type Button (third in the row)
                Rect packageTypeRect = new Rect(
                    packageItemRect.x + buttonWidth + buttonSpacing,
                    packageItemRect.y,
                    buttonWidth,
                    buttonHeight
                );

                // Toggle between baggie/jar on click
                string packageTypeText = PlayerCache._packageType == "baggie" ? "Type: Baggie" : "Type: Jar";
                if (GUI.Button(packageTypeRect, packageTypeText, UIs._buttonStyle))
                {
                    PlayerCache._packageType = PlayerCache._packageType == "baggie" ? "jar" : "baggie";
                    Notifier.ShowNotification("Package Type", $"Set to {PlayerCache._packageType}", NotificationSystem.NotificationType.Info);
                }

                // Search Bar - moved down for better spacing
                float searchBarY = headerRect.y + headerRect.height + 20f;
                Rect searchRect = new Rect(20f, searchBarY, windowWidth - 40f, 30f);
                GUI.Label(new Rect(searchRect.x, searchRect.y - 25f, 100f, 25f), "Search:", UIs._labelStyle);

                // Use custom text field for search
                if (!ModData._textFields.TryGetValue("itemSearch", out var searchField))
                {
                    searchField = new CustomTextField(ModStateS._itemSearchText, UIs._searchBoxStyle ?? GUI.skin.textField);
                    ModData._textFields["itemSearch"] = searchField;
                }
                ModStateS._itemSearchText = searchField.Draw(searchRect);

                // Item Grid Scroll View - adjusted position to account for new header
                float scrollViewY = searchRect.y + searchRect.height + 20f;
                Rect scrollViewRect = new Rect(
                    20f,
                    scrollViewY,
                    windowWidth - 40f,
                    windowHeight - (scrollViewY - verticalOffset) - 120f // Adjust height to fit
                );

                // Calculate dynamic content height based on filtered items
                if (!ModData._itemCache.TryGetValue("items", out var allItems)) return;

                var filteredItems = allItems
                .Where(item => string.IsNullOrEmpty(ModStateS._itemSearchText) ||
                                   item.IndexOf(ModStateS._itemSearchText, StringComparison.OrdinalIgnoreCase) >= 0)
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
                ModStateS._itemScrollPosition = GUI.BeginScrollView(
                    scrollViewRect,
                    ModStateS._itemScrollPosition,
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
                if (!string.IsNullOrEmpty(ModStateS._selectedItemId))
                {
                    Rect detailsRect = new Rect(
                        20f,
                        windowHeight - 100f + verticalOffset,
                    windowWidth - 40f,
                        90f
                    );

                    GUI.Box(detailsRect, "", UIs._panelStyle);

                    // Selected Item Label
                    GUI.Label(
                        new Rect(detailsRect.x + 10f, detailsRect.y + 10f, 200f, 25f),
                        "Selected Item: " + GetDisplayNameFromId(ModStateS._selectedItemId),
                        UIs._labelStyle
                    );

                    // Quantity Input
                    GUI.Label(
                        new Rect(detailsRect.x + 10f, detailsRect.y + 40f, 100f, 25f),
                        "Quantity:",
                        UIs._labelStyle
                    );

                    Rect quantityRect = new Rect(detailsRect.x + 120f, detailsRect.y + 40f, 100f, 25f);
                    if (!ModData._textFields.TryGetValue("quantityInput", out var quantityField))
                    {
                        quantityField = new CustomTextField(ModStateS._quantityInput, UIs._inputFieldStyle ?? GUI.skin.textField);
                        ModData._textFields["quantityInput"] = quantityField;
                    }
                    ModStateS._quantityInput = quantityField.Draw(quantityRect);

                    int quantity = 1;
                    if (!int.TryParse(ModStateS._quantityInput, out quantity))
                    {
                        // Invalid input - reset to default
                        quantity = 1;
                        ModStateS._quantityInput = "1";
                    }

                    // Ensure minimum quantity of 1
                    quantity = Math.Max(quantity, 1);

                    // Slot Input
                    GUI.Label(
                        new Rect(detailsRect.x + 250f, detailsRect.y + 40f, 100f, 25f),
                        "Slot:",
                        UIs._labelStyle
                    );

                    Rect slotRect = new Rect(detailsRect.x + 350f, detailsRect.y + 40f, 100f, 25f);
                    if (!ModData._textFields.TryGetValue("slotInput", out var slotField))
                    {
                        slotField = new CustomTextField(ModStateS._slotInput, UIs._inputFieldStyle ?? GUI.skin.textField);
                        ModData._textFields["slotInput"] = slotField;
                    }
                    ModStateS._slotInput = slotField.Draw(slotRect);

                    // Quality Selection
                    if (qualities != null)
                    {
                        float qualityX = detailsRect.x + 10f;
                        for (int i = 0; i < qualities.Count; i++)
                        {
                            Rect qualityRect = new Rect(qualityX, detailsRect.y + 70f, 80f, 25f);

                            var style = i == ModStateS._selectedQualityIndex ? UIs._itemSelectedStyle : UIs._itemButtonStyle;
                            if (GUI.Button(qualityRect, qualities[i], style ?? UIs._buttonStyle))
                            {
                                ModStateS._selectedQualityIndex = i;
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

                    if (GUI.Button(spawnRect, "Spawn Item", UIs._buttonStyle))
                    {
                        // Get the quantity
                        if (!int.TryParse(ModStateS._quantityInput, out quantity) || quantity < 1)
                        {
                            quantity = 1;
                        }

                        // Convert selected quality index to enum value
                        var quality = (Il2CppScheduleOne.ItemFramework.EQuality)ModStateS._selectedQualityIndex;

                        // Use the console command approach
                        SpawnCommand.SpawnItemViaConsole(ModStateS._selectedItemId, quantity, quality);
                    }
                }
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error in DrawItemManager: {ex}");
            }
        }

        public static void DrawItemButton(string itemName, Rect buttonRect)
        {
            if (string.IsNullOrEmpty(itemName) || !ModData._itemDictionary.ContainsKey(itemName))
                return;

            // Get the item ID for this display name
            string itemId = ModData._itemDictionary[itemName];

            bool isSelected = ModStateS._selectedItemId == itemId;
            // Ensure consistent item size with some padding
            var style = isSelected ? UIs._itemSelectedStyle : UIs._itemButtonStyle;
            style = style ?? UIs._buttonStyle;

            // Adjust style to ensure text is centered and doesn't overflow
            style.alignment = TextAnchor.MiddleCenter;
            style.wordWrap = true;

            if (GUI.Button(buttonRect, itemName, style))
            {
                // Set the selected item ID when clicked
                ModStateS._selectedItemId = itemId;

                // Reset quantity and slot to defaults
                ModStateS._quantityInput = "1";
                ModStateS._slotInput = "1";

                // Reset quality to default (Heavenly)
                ModStateS._selectedQualityIndex = 4; // Assuming Heavenly is the 5th option
            }

            if (buttonRect.Contains(Event.current.mousePosition))
            {
                DrawCoreUI.ShowItemTooltip(itemName, buttonRect);
            }
        }
        public static string GetDisplayNameFromId(string itemId)
        {
            // Find the display name that corresponds to the item ID
            foreach (var item in ModData._itemDictionary)
            {
                if (item.Value == itemId)
                {
                    return item.Key;
                }
            }
            return itemId; // Fallback to ID if no display name found
        }
    }
}
