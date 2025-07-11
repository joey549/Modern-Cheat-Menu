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
                ModLogger.Error($"Error in DrawCommandCategory: {ex}");
            }
        }
    }
}
