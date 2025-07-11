using UnityEngine;
using static Modern_Cheat_Menu.Library.NotificationSystem;

namespace Modern_Cheat_Menu.Library
{
    public class ModState
    {
        // Player booleans & shit
        public static bool _staticPlayerGodmodeEnabled = false;
        public bool _playerGodmodeEnabled = false;
        public object _godModeCoroutine = null;
        public bool _playerNeverWantedEnabled = false;
        public object _neverWantedCoroutine = null;
        public static string _localPlayerName = ""; // Store the local player name for checking

        // New weapon cheat settings
        public bool _unlimitedAmmoEnabled = false;
        public object _unlimitedAmmoCoroutine = null;
        public object _perfectAccuracyCoroutine = null;
        public bool _aimbotEnabled = false;
        public object _aimbotCoroutine = null;
        public float _aimbotRange = 50f; // Maximum range to detect enemies
        public bool _autoFireEnabled = false;
        public float _autoFireDelay = 0.5f; // Delay between auto shots
        public bool _perfectAccuracyEnabled = false;
        public bool _noRecoilEnabled = false;
        public object _noRecoilCoroutine = null;
        public bool _oneHitKillEnabled = false;
        public bool _npcsPacifiedEnabled = false;
        public object _pacifyNPCsCoroutine = null;
        public bool _forceCrosshairAlwaysVisible = false;

        // Free camera settings
        public bool _freeCamEnabled = false;
        public Camera _mainCamera;

        // Item manager state
        public string _itemSearchText = "";
        public Vector2 _itemScrollPosition = Vector2.zero;
        public int _itemsPerRow = 5;
        public int _selectedItemIndex = -1;
        public int _selectedQualityIndex = 4; // Default to Heavenly (4)
        public string _selectedItemId = "";
        public string _quantityInput = "1";
        public string _slotInput = "1";
        public float _timeScaleValue = 1.0f;
        public float _timeHours = 12.0f;
        public float _timeMinutes = 0.0f;
        public bool _showTooltip = false;
        public string _currentTooltip = "";
        public Vector2 _tooltipPosition;
        public float _tooltipTimer = 0f;
        public int _tooltipItemId = -1;

        // Settings
        public bool _enableBlur = true;
        public bool _enableAnimations = true;
        public bool _enableGlow = true;
        public bool _darkTheme = true;
        public float _uiOpacity = 0.95f;

        // Notification system
        public Queue<Notification> _notifications = new Queue<Notification>();
        public List<Notification> _activeNotifications = new List<Notification>();
        public float _notificationDisplayTime = 3f;
        public float _notificationFadeTime = 0.5f;

        // Modded Trash Vars
        public float trashGrabberAutoRadius = 0.4f;
        public bool drawTrashGrabberPickup = false;

        public int trashMaxLimit = 2000;
        public int trashEstCache = 0;
        public bool patchSleepTrashLimit = false;


    }
}
