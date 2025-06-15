using MelonLoader;

namespace Modern_Cheat_Menu.Library
{
    public static class ModLogger
    {
        //Log helper, adds tag, helps when debugging.
        private const string Tag = "[Modern_Cheat_Menu]";

        public static void Info(string msg) => MelonLogger.Msg(Tag +" "+ msg);
        public static void Warn(string msg) => MelonLogger.Warning(Tag + " " + msg);
        public static void Error(string msg) => MelonLogger.Error(Tag + " " + msg);
        public static void Debug(string msg)
        {
#if DEBUG
            MelonLogger.Msg(Tag + " [DEBUG]", msg);
#endif
        }
    }
}
