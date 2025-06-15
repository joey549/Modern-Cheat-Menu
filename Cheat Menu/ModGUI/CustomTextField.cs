using UnityEngine;

namespace Modern_Cheat_Menu.ModGUI
{
    public class CustomTextField //public class CustomTextField
    {
        private string _value;
        private GUIStyle _style;
        private bool _isFocused;
        private int _id;
        private static int _nextId = 1000;
        private float _lastInputTime;
        private const float INPUT_COOLDOWN = 0.1f;

        // Static field to track the currently focused text field
        private static CustomTextField _currentlyFocusedField = null;

        public string Value
        {
            get => _value;
            set => _value = value ?? "";
        }

        public CustomTextField(string initialValue = "", GUIStyle style = null)
        {
            _value = initialValue ?? "";
            _style = style ?? GUI.skin.textField;
            _id = _nextId++;
        }

        public string Draw(Rect position)
        {
            return Draw(position, _value, _style);
        }

        public string Draw(Rect position, string text, GUIStyle style)
        {
            Event current = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Keyboard);

            // Handle focus
            switch (current.type)
            {
                case EventType.MouseDown:
                    if (position.Contains(current.mousePosition))
                    {
                        // Unfocus any previously focused field
                        if (_currentlyFocusedField != null && _currentlyFocusedField != this)
                        {
                            _currentlyFocusedField._isFocused = false;
                        }

                        // Focus this field
                        GUIUtility.keyboardControl = controlID;
                        _isFocused = true;
                        _currentlyFocusedField = this;
                        current.Use();
                    }
                    else if (_isFocused)
                    {
                        // Clicked outside, unfocus this field
                        _isFocused = false;
                        if (_currentlyFocusedField == this)
                        {
                            _currentlyFocusedField = null;
                        }
                    }
                    break;

                case EventType.KeyDown:
                    if (_isFocused && GUIUtility.keyboardControl == controlID)
                    {
                        switch (current.keyCode)
                        {
                            case KeyCode.Backspace:
                                if (_value.Length > 0)
                                {
                                    _value = _value.Substring(0, _value.Length - 1);
                                    current.Use();
                                }
                                break;

                            case KeyCode.Return:
                            case KeyCode.KeypadEnter:
                            case KeyCode.Escape:
                                _isFocused = false;
                                GUIUtility.keyboardControl = 0;
                                _currentlyFocusedField = null;
                                current.Use();
                                break;
                        }
                    }
                    break;

                case EventType.Layout:
                    if (_isFocused && _currentlyFocusedField == this)
                    {
                        HandleTextInput(current);
                    }
                    break;
            }

            // Draw the field background
            GUI.Box(position, "", style);

            // Draw the text with cursor
            string displayText = _value;
            if (_isFocused && (Time.time % 1f) < 0.5f)
            {
                displayText += "|"; // Blinking cursor
            }

            GUI.Label(position, displayText, style);

            return _value;
        }

        private void HandleTextInput(Event current)
        {
            // Prevent rapid duplicate input
            if (current.character != '\0' &&
                !char.IsControl(current.character) &&
                Time.time - _lastInputTime > INPUT_COOLDOWN)
            {
                _value += current.character;
                _lastInputTime = Time.time;
                current.Use();
            }
        }

        public string DrawLayout(GUILayoutOption[] options = null)
        {
            Rect rect = GUILayoutUtility.GetRect(40, 20, options ?? new GUILayoutOption[0]);
            return Draw(rect);
        }

        public static implicit operator string(CustomTextField textField)
        {
            return textField.Value;
        }
    }
}
