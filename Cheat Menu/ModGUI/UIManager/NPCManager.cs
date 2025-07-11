using static Modern_Cheat_Menu.Core;
using Modern_Cheat_Menu.Commands;
using Modern_Cheat_Menu.Library;
using UnityEngine;
using Il2CppSystem.Collections.Generic;
using Il2CppEasyButtons;
using Modern_Cheat_Menu.Model;
using Il2CppScheduleOne.AvatarFramework.Emotions;

namespace Modern_Cheat_Menu.ModGUI.UIManager
{
    public class NPCManager
    {
        public static string _selectedId;
        public static string _SearchText;
        public static Vector2 _ScrollPosition = Vector2.zero;

        public static GUIStyle _npcButtonStyle;
        public static GUIStyle _npcSelectedStyle;
        public static string _relationshipInput = "1";

        public static void DrawNPCManager()
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

                float startX = headerRect.x + (headerRect.width - (120f * 3 + 20f * 2)) / 2;

                ManagerHelper.DrawButtonUI(
                    ref startX,
                    headerRect.y + (headerRect.height - 30f) / 2,
                    120f,
                    30f,
                    "Unlock All NPCs",
                    UIs._buttonStyle,
                    () =>
                    {
                        int sCount = 0;

                        foreach (var kvp in ModData._npcDictionary)
                        {
                            string npcName = kvp.Key;
                            string npcID = kvp.Value;

                            try
                            {
                                ModLogger.Info($"TestCommand {sCount}: ID: {npcID} Name: {npcName}");

                                var cmd = new Il2CppScheduleOne.Console.SetUnlocked();
                                cmd.Execute(CommandCore.ToCommandList(npcID));

                                sCount++;
                            }
                            catch (Exception ex)
                            {
                                ModLogger.Warn($"❌ EROEOOERr: {npcID} ({npcName}) - {ex.Message}");
                            }
                        }
                    },
                    20f
                );

                // Search
                _SearchText = ManagerHelper.DrawSearchBarUI(ref _SearchText, headerRect, "Search", "npcSearch");

                if (!ModData._npcCache.TryGetValue("npcs", out var allNPCS)) return;

                var filteredNPCS = allNPCS
                    .Where(npc => string.IsNullOrEmpty(_SearchText) || npc.IndexOf(_SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                float scrollViewY = headerRect.y + headerRect.height + 80f;
                Rect scrollRect = new Rect(20f, scrollViewY, windowWidth - 40f, windowHeight - (scrollViewY - verticalOffset) - 120f);

                ManagerHelper.DrawScrollableGridUI(
                    filteredNPCS,
                    scrollRect,
                    ref _ScrollPosition,
                    100f,
                    25f,
                    5,
                    DrawNPCButton
                );

                if (!string.IsNullOrEmpty(_selectedId))
                {
                    Rect detailsRect = new Rect(20f, windowHeight - 100f + verticalOffset, windowWidth - 40f, 90f);

                    GUI.Box(detailsRect, "", UIs._panelStyle);

                    GUI.Label(
                        new Rect(detailsRect.x + 10f, detailsRect.y + 10f, 200f, 25f),
                        "Selected NPC: " + GetDisplayNameFromId(_selectedId),
                        UIs._labelStyle
                    );

                    float sXUnlock = detailsRect.x + detailsRect.width - 130f;
                    float sYUnlock = detailsRect.y + detailsRect.height - 40f;

                    ManagerHelper.DrawButtonUI(
                        ref sXUnlock,
                        sYUnlock,
                        120f,
                        30f,
                        "Unlock NPC",
                        UIs._buttonStyle,
                        () =>
                        {
                            var cmd = new Il2CppScheduleOne.Console.SetUnlocked();
                            cmd.Execute(CommandCore.ToCommandList(_selectedId));
                            Notifier.ShowSuccess("Set Unlocked", $"Unlocked: ({_selectedId})");
                        },
                        20f);

                    float qualityX = 0f;
                    foreach (var kvp in ModData.RelationshipLevels)
                    {
                        int key = kvp.Key;
                        string label = kvp.Value;

                        Rect qualityRect = new Rect(detailsRect.x + 10f - qualityX, sYUnlock, 80f, 25f);

                        if (GUI.Button(qualityRect, label, UIs._buttonStyle))
                        {
                            _relationshipInput = key.ToString(); // This is used in the command
                        }

                        qualityX -= 90f;
                    }

                    float sXRela = detailsRect.x + detailsRect.width - 260f;
                    ManagerHelper.DrawButtonUI(
                        ref sXRela,
                        sYUnlock + 40,
                        120f,
                        30f,
                        "Set Relationship",
                        UIs._buttonStyle,
                        () =>
                        {
                            var il2cppList = new Il2CppSystem.Collections.Generic.List<string>();
                            il2cppList.Add(_selectedId);
                            il2cppList.Add(_relationshipInput);

                            

                            var cmd = new Il2CppScheduleOne.Console.SetRelationship();
                            cmd.Execute(il2cppList);

                            Notifier.ShowSuccess("Set Relationship", $"npc: ({_selectedId}) set to {_relationshipInput}");
                        },
                        20f);
                }

            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"Error in DrawNPCManager: {ex}");
            }
        }

        public static void DrawNPCButton(string npcName, Rect buttonRect)
        {
            if (string.IsNullOrEmpty(npcName) || !ModData._npcDictionary.ContainsKey(npcName))
                return;

            string npcID = ModData._npcDictionary[npcName];

            bool isSelected = _selectedId == npcID;
            var style = isSelected ? _npcSelectedStyle : _npcButtonStyle;
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
                npcName,
                style,
                () => _selectedId = npcID,
                20f
            );

            if (buttonRect.Contains(Event.current.mousePosition))
                DrawCoreUI.ShowNPCTip(npcName, buttonRect);
        }
        public static string GetDisplayNameFromId(string npcID)
        {
            // Find the display name that corresponds to the item ID
            foreach (var item in ModData._npcDictionary)
                if (item.Value == npcID)
                    return item.Key;

            return npcID; // Fallback to ID if no display name found
        }
    }
}