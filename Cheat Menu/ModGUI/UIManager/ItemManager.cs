using static Modern_Cheat_Menu.Core;
using Modern_Cheat_Menu.Commands;
using Modern_Cheat_Menu.Library;
using Modern_Cheat_Menu.Model;
using UnityEngine;
using Il2CppEasyButtons;

namespace Modern_Cheat_Menu.ModGUI.UIManager
{
    public class ItemManager
    {
        public static string _selectedId;
        public static string _SearchText;
        public static Vector2 _ScrollPosition = Vector2.zero;
        public static string _quantityInput = "1";
        public static string _slotInput = "1";
        public static int _selectedQualityIndex = 4;

        public static GUIStyle _itemButtonStyle;
        public static GUIStyle _itemSelectedStyle;
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
                float buttonHeight = 35;
                float buttonSpacing = 20f;
                float startX = headerRect.x + (headerRect.width - (buttonWidth * 3 + buttonSpacing * 2)) / 2;

                if (!ModData._itemCache.TryGetValue("qualities", out var qualities)) return;
                ManagerHelper.DrawButtonUI(
                    ref startX,
                    headerRect.y + (headerRect.height - buttonHeight) / 2,
                    buttonWidth,
                    buttonHeight,
                    "Set Quality",
                    UIs._buttonStyle,
                    () => SpawnCommand.SetItemQuality((Il2CppScheduleOne.ItemFramework.EQuality)_selectedQualityIndex),
                    buttonSpacing
                );

                float buttonX = startX;
                float buttonY = headerRect.y + (headerRect.height - buttonHeight) / 2f;

                ManagerHelper.DrawButtonUI(
                    ref buttonX,
                    buttonY,
                    buttonWidth,
                    buttonHeight,
                    "Package Item",
                    UIs._buttonStyle,
                    () => SpawnCommand.PackageProductCommand(PlayerCache._packageType),
                    buttonSpacing
                );

                ManagerHelper.DrawButtonUI(
                    ref buttonX,
                    buttonY,
                    buttonWidth,
                    buttonHeight,
                    PlayerCache._packageType == "baggie" ? "Type: Baggie" : "Type: Jar",
                    UIs._buttonStyle,
                    () =>
                    {
                        PlayerCache._packageType = PlayerCache._packageType == "baggie" ? "jar" : "baggie";
                        Notifier.ShowNotification("Package Type", $"Set to {PlayerCache._packageType}", NotificationSystem.NotificationType.Info);
                    },
                    0f
                );

                // Search
                _SearchText = ManagerHelper.DrawSearchBarUI(ref _SearchText, headerRect, "Search", "itemSearch");

                if (!ModData._itemCache.TryGetValue("items", out var allItems)) return;

                var filteredItems = allItems
                    .Where(item => string.IsNullOrEmpty(_SearchText) ||
                                   item.IndexOf(_SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                float scrollViewY = headerRect.y + headerRect.height + 80f;
                Rect scrollRect = new Rect(20f, scrollViewY, windowWidth - 40f, windowHeight - (scrollViewY - verticalOffset) - 120f);

                ManagerHelper.DrawScrollableGridUI(
                    filteredItems,
                    scrollRect,
                    ref _ScrollPosition,
                    100f,
                    25f,
                    5,
                    DrawItemButton
                );

                // Item Details Panel
                if (!string.IsNullOrEmpty(_selectedId))
                {
                    Rect detailsRect = new Rect(20f, windowHeight - 100f + verticalOffset, windowWidth - 40f, 90f);

                    GUI.Box(detailsRect, "", UIs._panelStyle);

                    // Selected Item Label
                    GUI.Label(
                        new Rect(detailsRect.x + 10f, detailsRect.y + 10f, 200f, 25f),
                        "Selected Item: " + GetDisplayNameFromId(_selectedId),
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
                        quantityField = new CustomTextField(_quantityInput, UIs._inputFieldStyle ?? GUI.skin.textField);
                        ModData._textFields["quantityInput"] = quantityField;
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
                        UIs._labelStyle
                    );

                    Rect slotRect = new Rect(detailsRect.x + 350f, detailsRect.y + 40f, 100f, 25f);
                    if (!ModData._textFields.TryGetValue("slotInput", out var slotField))
                    {
                        slotField = new CustomTextField(_slotInput, UIs._inputFieldStyle ?? GUI.skin.textField);
                        ModData._textFields["slotInput"] = slotField;
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
                            if (GUI.Button(qualityRect, qualities[i], style ?? UIs._buttonStyle))
                            {
                                _selectedQualityIndex = i;
                            }

                            qualityX += 90f;
                        }
                    }

                    float spawnX = detailsRect.x + detailsRect.width - 130f;
                    float spawnY = detailsRect.y + detailsRect.height - 40f;

                    ManagerHelper.DrawButtonUI(
                        ref spawnX,
                        spawnY,
                        120f,
                        30f,
                        "Spawn Item",
                        UIs._buttonStyle,
                        () =>
                        {
                            if (!int.TryParse(_quantityInput, out quantity) || quantity < 1)
                                quantity = 1;

                            SpawnCommand.SpawnItemViaConsole(_selectedId, quantity, (Il2CppScheduleOne.ItemFramework.EQuality)_selectedQualityIndex);
                        },
                        buttonSpacing
                    );
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

            bool isSelected = _selectedId == itemId;
            // Ensure consistent item size with some padding
            var style = isSelected ? _itemSelectedStyle : _itemButtonStyle;
            style = style ?? UIs._buttonStyle;

            // Adjust style to ensure text is centered and doesn't overflow
            style.alignment = TextAnchor.MiddleCenter;
            style.wordWrap = true;

            float buttonRectX = buttonRect.x;
            float buttonRectY = buttonRect.y;

            ManagerHelper.DrawButtonUI(
                ref buttonRectX,
                buttonRectY,
                120f,
                30f,
                itemName,
                style,
                () =>
                {
                    
                    _selectedId = itemId; // Set the selected item ID when clicked

                    // Reset quantity and slot to defaults
                    _quantityInput = "1";
                    _slotInput = "1";

                    _selectedQualityIndex = 4; // Reset quality to default (Heavenly) | (Assuming Heavenly is the 5th option)
                },
                1f
            );

            if (buttonRect.Contains(Event.current.mousePosition))
                DrawCoreUI.ShowItemTooltip(itemName, buttonRect);
        }
        public static string GetDisplayNameFromId(string itemId)
        {
            // Find the display name that corresponds to the item ID
            foreach (var item in ModData._itemDictionary)
            {
                if (item.Value == itemId)
                    return item.Key;
            }
            return itemId; // Fallback to ID if no display name found
        }
    }
}
