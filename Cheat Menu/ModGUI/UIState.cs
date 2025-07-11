using UnityEngine;

namespace Modern_Cheat_Menu.ModGUI
{
    public class UIState
    {
        // UI settings
        public bool _uiVisible = false;
        //public Rect _windowRect = new Rect(20, 20, 900, 650);
        public Rect _windowRect = new Rect( (Screen.width - 600f) / 2f, (Screen.height - 600f) / 2f, 800f, 600f);
        public Vector2 _scrollPosition = Vector2.zero;
        public int _selectedCategoryIndex = 0;
        public float _fadeInProgress = 0f;
        public bool _isInitialized = false;
        public float _uiScale = 1.0f;
        public bool _showSettings = false;
        public bool _isDragging = false;
        public Vector2 _dragOffset;

        public Vector2 _playerScrollPosition = Vector2.zero;
        public Vector2 _settingsScrollPosition = Vector2.zero;

        // Style and texture control
        public bool _stylesInitialized = false;
        public bool _needsTextureRecreation = false;
        public bool _needsStyleRecreation = false;

        // Animation timers
        public float _menuAnimationTime = 0f;
        public Dictionary<string, float> _commandAnimations = new Dictionary<string, float>();
        public Dictionary<string, float> _buttonHoverAnimations = new Dictionary<string, float>();
        public Dictionary<string, float> _toggleAnimations = new Dictionary<string, float>();
        public Dictionary<string, Vector2> _itemGridAnimations = new Dictionary<string, Vector2>();

        public static string _activeDropdownKey = null;
        public static Rect _activeDropdownRect;

        // IMGUI Styling
        public GUISkin _customSkin;
        public Texture2D _backgroundTexture;
        public Texture2D _panelTexture;
        public Texture2D _buttonNormalTexture;
        public Texture2D _buttonHoverTexture;
        public Texture2D _buttonActiveTexture;
        public Texture2D _toggleOnTexture;
        public Texture2D _toggleOffTexture;
        public Texture2D _sliderThumbTexture;
        public Texture2D _sliderTrackTexture;
        public Texture2D _inputFieldTexture;
        public Texture2D _headerTexture;
        public Texture2D _categoryTabTexture;
        public Texture2D _categoryTabActiveTexture;
        public Texture2D _checkmarkTexture;
        public Texture2D _settingsIconTexture;
        public Texture2D _closeIconTexture;
        public Texture2D _glowTexture;
        public GUIStyle _labelStyle;
        public Texture2D _warningTexture;

        public Texture2D _settingsButtonTexture;
        public Texture2D _closeButtonTexture;

        // Styles
        public GUIStyle _windowStyle;
        public GUIStyle _titleStyle;
        public GUIStyle _headerStyle;
        public GUIStyle _subHeaderStyle;
        public GUIStyle _categoryButtonStyle;
        public GUIStyle _categoryButtonActiveStyle;
        public GUIStyle _commandLabelStyle;
        public GUIStyle _buttonStyle;
        public GUIStyle _iconButtonStyle;
        public GUIStyle _toggleButtonStyle;
        public GUIStyle _toggleButtonActiveStyle;
        public GUIStyle _sliderStyle;
        public GUIStyle _sliderThumbStyle;
        public GUIStyle _inputFieldStyle;
        public GUIStyle _searchBoxStyle;
        public GUIStyle _tooltipStyle;
        public GUIStyle _closeButtonStyle;
        public GUIStyle _statusStyle;
        public GUIStyle _panelStyle;
        public GUIStyle _separatorStyle;

        // Colors
        public Color _backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        public Color _panelColor = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        public Color _accentColor = new Color(0.15f, 0.55f, 0.95f, 1f); // Blue accent
        public Color _secondaryAccentColor = new Color(0.15f, 0.85f, 0.55f); // Green accent
        public Color _warningColor = new Color(0.95f, 0.55f, 0.15f); // Orange warning
        public Color _dangerColor = new Color(0.95f, 0.25f, 0.25f); // Red danger
        public Color _textColor = new Color(0.9f, 0.9f, 0.9f);
        public Color _dimTextColor = new Color(0.7f, 0.7f, 0.75f);
        public Color _headerColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);
    }
}
