using static Modern_Cheat_Menu.Core;
using Modern_Cheat_Menu.Library;

namespace Modern_Cheat_Menu.Commands
{
    public class CommandCore
    {
        public class CommandParameter
        {
            public string Name { get; set; }
            public string Placeholder { get; set; }
            public ParameterType Type { get; set; }
            public string ItemCacheKey { get; set; }
            public string Value { get; set; }
        }

        public enum ParameterType
        {
            Input,
            Dropdown
        }

        public class Command
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public System.Action<string[]> Handler { get; set; }
            public List<CommandParameter> Parameters { get; set; } = new List<CommandParameter>();
        }

        public class CommandCategory
        {
            public string Name { get; set; }
            public List<Command> Commands { get; set; } = new List<Command>();
        }

        // CommandCore Helpers

        public static bool TryParseSingleIntArg(string[] args, out int value, string contextLabel, bool showNotification = true)
        {
            value = 0;

            if (args.Length < 1)
            {
                ModLogger.Error($"{contextLabel} value required.");
                if (showNotification)
                    Notifier.ShowNotification("Error", $"{contextLabel} amount required.", NotificationSystem.NotificationType.Error);
                return false;
            }

            if (!int.TryParse(args[0], out value))
            {
                ModLogger.Error($"{contextLabel}: Invalid number input.");
                if (showNotification)
                    Notifier.ShowNotification("Error", $"Invalid {contextLabel.ToLower()} amount specified.", NotificationSystem.NotificationType.Error);
                return false;
            }

            return true;
        }

        // To list by int
        public static Il2CppSystem.Collections.Generic.List<string> ToCommandList<T>(T value)
        {
            var list = new Il2CppSystem.Collections.Generic.List<string>();
            list.Add(value?.ToString());
            return list;
        }
        public static void ExecuteCommand(CommandCore.Command command)
        {
            try
            {
                if (command.Handler != null)
                {
                    string[] args = command.Parameters
                        .Select(p => p.Value?.Trim() ?? "")
                        .Where(a => !string.IsNullOrEmpty(a))
                        .ToArray();

                    command.Handler.Invoke(args);
                    ModLogger.Info($"Command Executed: {command.Name} completed"); // Log completions. No need to notify unless error... 
                    //Notifier.ShowNotification("Command Executed", $"{command.Name} completed", NotificationSystem.NotificationType.Success); // Uncomment if you want.
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Command failed: Exception: {ex} | CommandName: {command.Name}");
                Notifier.ShowNotification("Command Error (check log)", ex.Message, NotificationSystem.NotificationType.Error);
            }
        }
    }
}
