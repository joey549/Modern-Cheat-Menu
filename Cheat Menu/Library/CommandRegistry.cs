using Modern_Cheat_Menu.Commands;
using Modern_Cheat_Menu.Features;

namespace Modern_Cheat_Menu.Library
{
    public class CommandRegistry
    {
        public static void RegisterCommands()
        {
            #region Command Data/Cache
            ModData._categories.Clear();

            // Initialize dropdown option lists
            ModData._itemCache["explosion_targets"] = new List<string> {
                "custom",
                "all",
                "random",
                "nukeall"
            };

            ModData._itemCache["vehicle_targets"] = new List<string> {
                "shitbox",
                "veeper",
                "bruiser",
                "dinkler",
                "hounddog",
                "cheetah"
            };

            ModData._itemCache["predefined_tele_targets"] = new List<string>
            {
                "motel",
                "sweatshop",
                "barn",
                "bungalow",
                "warehouse",
                "docks",
                "manor",
                "postoffice",
                "dealership",
                "tacoticklers",
                "laundromat",
                "carwash",
                "pawnshop",
                "hardwarestore"
            };

            ModData._itemCache["property_targets"] = new List<string>
            {
                "storageunit",
                "bungalow",
                "barn",
                "dockswarehouse"
            };

            ModData._itemCache["business_targets"] = new List<string>
            {
                "laundromat",
                "postoffice",
                "carwash"
            };

            ModData._itemCache["employee_targets"] = new List<string>
            {
                "cleaner",
                "botanist",
                "packager",
                "chemist"
            };

            ModLogger.Info($"Added {ModData._itemCache["explosion_targets"].Count} explosion targets to item cache.");
            ModLogger.Info($"Added {ModData._itemCache["vehicle_targets"].Count} vehicles to item cache.");
            ModLogger.Info($"Added {ModData._itemCache["predefined_tele_targets"].Count} predefined teleport locations.");
            ModLogger.Info($"Added {ModData._itemCache["property_targets"].Count} properties to cache.");
            ModLogger.Info($"Added {ModData._itemCache["business_targets"].Count} business's to cache.");
            ModLogger.Info($"Added {ModData._itemCache["employee_targets"].Count} employee's to cache.");

            #endregion

            var onlineCategory = new CommandCore.CommandCategory { Name = "Online" };
            var playerCategory = new CommandCore.CommandCategory { Name = "Self" };
            var exploitsCategory = new CommandCore.CommandCategory { Name = "Exploits" };
            var itemsCategory = new CommandCore.CommandCategory { Name = "Item Manager" };
            var worldCategory = new CommandCore.CommandCategory { Name = "World" };
            var propertyCategory = new CommandCore.CommandCategory { Name = "Property Manager" };
            var teleportCategory = new CommandCore.CommandCategory { Name = "Teleport Manager" };
            var vehicleCategory = new CommandCore.CommandCategory { Name = "Vehicle Manager" };
            var systemCategory = new CommandCore.CommandCategory { Name = "Game" };

            #region Player category
            playerCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Toggle Godmode",
                Description = "Toggles godmode on/off.",
                Handler = PlayerCommand.ToggleGodmode
            });
            playerCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Toggle Unlimited Ammo",
                Description = "Toggles unlimited ammo on/off.",
                Handler = PlayerCommand.ToggleUnlimitedAmmo
            });
            playerCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Toggle Never Wanted",
                Description = "Toggles never wanted on/off.",
                Handler = PlayerCommand.ToggleNeverWanted
            });
            playerCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Give XP",
                Description = "Gives player XP.",
                Handler = PlayerCommand.ChangeXP,
                Parameters = new List<CommandCore.CommandParameter> {
                    new CommandCore.CommandParameter {
                        Name = "Amount",
                        Placeholder = "Amount",
                        Type = CommandCore.ParameterType.Input,
                        Value = "25"
                    }
                }
            });
            playerCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Give Cash",
                Description = "Sends the quantity of cash to the player's cash balance, can take negative numbers.",
                Handler = PlayerCommand.ChangeCash,
                Parameters = new List<CommandCore.CommandParameter> {
                    new CommandCore.CommandParameter {
                        Name = "Amount",
                        Placeholder = "Amount",
                        Type = CommandCore.ParameterType.Input,
                        Value = "1000"
                    }
                }
            });
            playerCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Give Online Balance",
                Description = "Sends the quantity of cash to the player's online balance, can take negative numbers.",
                Handler = PlayerCommand.ChangeBalance,
                Parameters = new List<CommandCore.CommandParameter>
                {
                    new CommandCore.CommandParameter
                    {
                        Name = "Amount",
                        Placeholder = "Amount",
                        Type = CommandCore.ParameterType.Input,
                        Value = "1000"
                    }
                }
            });
            playerCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Toggle Always Visible Crosshair",
                Description = "Forces the crosshair to always remain visible, even when using items that would normally hide it.",
                Handler = CrosshairHandler.ToggleAlwaysVisibleCrosshair
            });
            playerCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Raise Wanted Level",
                Description = "Raises your wanted level.",
                Handler = PlayerCommand.RaiseWantedLevel
            });
            playerCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Lower Wanted Level",
                Description = "Lowers your wanted level.",
                Handler = PlayerCommand.LowerWantedLevel
            });
            playerCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Clear Wanted Level",
                Description = "Clears your wanted level.",
                Handler = PlayerCommand.ClearWantedLevel
            });
            playerCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Set Movement Speed",
                Description = "Sets the player's movement speed.",
                Handler = PlayerCommand.SetPlayerMovementSpeed,
                Parameters = new List<CommandCore.CommandParameter> {
                    new CommandCore.CommandParameter {
                        Name = "Speed",
                        Placeholder = "Speed",
                        Type = CommandCore.ParameterType.Input,
                        Value = "1"
                    }
                }
            });
            playerCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Set Jump Force",
                Description = "Sets the player's jump force.",
                Handler = PlayerCommand.SetJumpForce,
                Parameters = new List<CommandCore.CommandParameter> {
                    new CommandCore.CommandParameter {
                        Name = "Force",
                        Placeholder = "Force",
                        Type = CommandCore.ParameterType.Input,
                        Value = "1"
                    }
                }
            });
            playerCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Set Stamina Reserve",
                Description = "Sets the player's stamina reserve.",
                Handler = PlayerCommand.SetPlayerStaminaReserve,
                Parameters = new List<CommandCore.CommandParameter> {
                    new CommandCore.CommandParameter {
                        Name = "Amount",
                        Placeholder = "Amount",
                        Type = CommandCore.ParameterType.Input,
                        Value = "200"
                    }
                }
            });
            playerCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Clear Inventory",
                Description = "Clears the player's inventory",
                Handler = PlayerCommand.ClearInventory
            });

            playerCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Unlimited Trash Grabber",
                Description = "Trash grabber never fills up.",
                Handler = PlayerCommand.UnlimitedTrashGrabber
            });

            #endregion 

            #region World category

            worldCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Free Camera",
                Description = "Toggles free camera mode",
                Handler = WorldCommand.ToggleFreeCam
            });
            worldCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Set Time",
                Description = "Sets the time of day (24-hour format)",
                Handler = WorldCommand.SetWorldTime,
                Parameters = new List<CommandCore.CommandParameter> {
                    new CommandCore.CommandParameter {
                        Name = "Time",
                        Placeholder = "HHMM (e.g. 1530)",
                        Type = CommandCore.ParameterType.Input,
                        Value = "1200"
                    }
                }
            });
            worldCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Set Time Scale",
                Description = "Sets game time scale (1.0 = normal)",
                Handler = WorldCommand.SetTimeScale,
                Parameters = new List<CommandCore.CommandParameter> {
                    new CommandCore.CommandParameter {
                        Name = "Scale",
                        Placeholder = "Scale",
                        Type = CommandCore.ParameterType.Input,
                        Value = "1.0"
                    }
                }
            });
            worldCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Grow Plants",
                Description = "Instantly grows all weed plants in the world.",
                Handler = WorldCommand.GrowPlants
            });
            worldCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Clear World Trash",
                Description = "Forcefully clears all world trash.",
                Handler = WorldCommand.ClearTrash
            });
            worldCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Law Intensity",
                Description = "Sets the law intensity (maximum 10)",
                Handler = PlayerCommand.SetLawIntensity,
                Parameters = new List<CommandCore.CommandParameter> {
                    new CommandCore.CommandParameter {
                        Name = "Intensity",
                        Placeholder = "6",
                        Type = CommandCore.ParameterType.Input,
                        Value = "6"
                    }
                }
            });

            #endregion

            #region Property Category

            playerCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Set Own Property",
                Description = "Set own a property.",
                Handler = PropertyCommands.ForceOwnProperty,
                Parameters = new List<CommandCore.CommandParameter> {
                    new CommandCore.CommandParameter
                    {
                        Name = "Property",
                        Placeholder = "Select property",
                        Type = CommandCore.ParameterType.Dropdown,
                        ItemCacheKey = "property_targets",
                        Value = "Storageunit"  // Default value
                    }
                }
            });

            propertyCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Set Own Business",
                Description = "Set own a Business.",
                Handler = PropertyCommands.ForceOwnProperty,
                Parameters = new List<CommandCore.CommandParameter> {
                    new CommandCore.CommandParameter
                    {
                        Name = "Business",
                        Placeholder = "Select business",
                        Type = CommandCore.ParameterType.Dropdown,
                        ItemCacheKey = "business_targets",
                        Value = "laundromat"  // Default value
                    }
                }
            });

            propertyCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Add Employee",
                Description = "Adds an employee to a property.",
                Handler = PropertyCommands.AddEmployeeToProperty,
                Parameters = new List<CommandCore.CommandParameter>
                {
                    new CommandCore.CommandParameter {
                        Name = "Property Code",
                        Placeholder = "Select a property",
                        Type = CommandCore.ParameterType.Dropdown,
                        ItemCacheKey = "property_targets",
                        Value = "dockswarehouse"
                    },
                    new CommandCore.CommandParameter {
                        Name = "Employee Code",
                        Placeholder = "Select a employee",
                        Type = CommandCore.ParameterType.Dropdown,
                        ItemCacheKey = "employee_targets",
                        Value = "Botanist"
                    }
                }
            });

            propertyCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Trash Can Settings",
                Description = "Set the radius/width, capacity of all trash cans.",
                Handler = PropertyCommands.SetTrashCanSettings,
                Parameters = new List<CommandCore.CommandParameter> {
                    new CommandCore.CommandParameter {
                        Name = "Radius",
                        Placeholder = "Radius",
                        Type = CommandCore.ParameterType.Input,
                        Value = "8"
                    },
                    new CommandCore.CommandParameter {
                        Name = "Width",
                        Placeholder = "Width",
                        Type = CommandCore.ParameterType.Input,
                        Value = "10"
                    },
                    new CommandCore.CommandParameter {
                        Name = "Capacity",
                        Placeholder = "Capacity",
                        Type = CommandCore.ParameterType.Input,
                        Value = "10"
                    }
                }

            });

            #endregion

            #region System category

            systemCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Save Game",
                Description = "Forces a game save",
                Handler = WorldCommand.ForceGameSave
            });
            systemCategory.Commands.Add(new CommandCore.Command
            {
                Name = "End Tutorial",
                Description = "Forcefully ends the tutorial.",
                Handler = WorldCommand.EndTutorial
            });

            #endregion

            #region Exploits Category
            exploitsCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Create Explosion",
                Description = "Create explosions. Options: 'all' (target all players), 'random' (target random player), or custom location.",
                Handler = ExplosionManager.CreateExplosion,
                Parameters = new List<CommandCore.CommandParameter> {
                    new CommandCore.CommandParameter {
                        Name = "Target",
                        Placeholder = "all/random",
                        Type = CommandCore.ParameterType.Dropdown,
                        ItemCacheKey = "explosion_targets",
                        Value = "custom"
                    },
                    new CommandCore.CommandParameter {
                        Name = "Damage",
                        Placeholder = "Damage",
                        Type = CommandCore.ParameterType.Input,
                        Value = "100"
                    },
                    new CommandCore.CommandParameter {
                        Name = "Radius",
                        Placeholder = "Radius",
                        Type = CommandCore.ParameterType.Input,
                        Value = "10"
                    }
                }
            });

            exploitsCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Kill Player",
                Description = "Kills the specified player by index.",
                Handler = PlayerCommand.KillPlayerCommand,
                Parameters = new List<CommandCore.CommandParameter> {
                    new CommandCore.CommandParameter {
                        Name = "PlayerIndex",
                        Placeholder = "Player Index",
                        Type = CommandCore.ParameterType.Input,
                        Value = "1"
                    }
                }
            });

            exploitsCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Damage Player",
                Description = "Damages the specified player by index.",
                Handler = PlayerCommand.DamagePlayerCommand,
                Parameters = new List<CommandCore.CommandParameter> {
                    new CommandCore.CommandParameter {
                        Name = "PlayerIndex",
                        Placeholder = "Player Index",
                        Type = CommandCore.ParameterType.Input,
                        Value = "1"
                    },
                    new CommandCore.CommandParameter {
                        Name = "Damage",
                        Placeholder = "Damage Amount",
                        Type = CommandCore.ParameterType.Input,
                        Value = "10"
                    }
                }
            });

            exploitsCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Kill All Players",
                Description = "Kills all players except yourself.",
                Handler = PlayerCommand.KillAllPlayersCommand
            });

            #endregion

            #region Vehicle Category

            vehicleCategory.Commands.Add(new CommandCore.Command
            {
                Name = "Spawn Vehicle",
                Description = "Spawns a vehicle of your choosing.",
                Handler = SpawnCommand.SpawnVehicle,
                Parameters = new List<CommandCore.CommandParameter> {
                    new CommandCore.CommandParameter
                    {
                        Name = "Vehicle",
                        Placeholder = "Select vehicle",
                        Type = CommandCore.ParameterType.Dropdown,
                        ItemCacheKey = "vehicle_targets",
                        Value = "Cheetah"  // Default value
                    }
                }
            });

            #endregion

            #region Category Add List
            // Add categories to list
            ModData._categories.Add(onlineCategory);
            ModData._categories.Add(playerCategory);
            ModData._categories.Add(exploitsCategory);
            ModData._categories.Add(itemsCategory);
            ModData._categories.Add(worldCategory);
            ModData._categories.Add(propertyCategory);
            ModData._categories.Add(teleportCategory);
            ModData._categories.Add(vehicleCategory);
            ModData._categories.Add(systemCategory);
            #endregion
        }
    }
}
