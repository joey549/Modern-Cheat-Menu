using Il2CppScheduleOne.PlayerScripts.Health;

namespace Modern_Cheat_Menu.Model
{
    public class OnlinePlayerInfo
    {
        public Il2CppScheduleOne.PlayerScripts.Player Player { get; set; }
        public string Name { get; set; }
        public string SteamID { get; set; }
        public string ServerBindAddress { get; set; }
        public string ClientAddress { get; set; }
        public PlayerHealth Health { get; set; }
        public bool IsLocal { get; set; }
        public bool ExplodeLoop { get; set; }
    }

}
