using static Modern_Cheat_Menu.Core;
using UnityEngine;

namespace Modern_Cheat_Menu.Library
{
    public class InputHandler
    {
        public static void HandleWindowDragging()
        {
            try
            {
                // Make the drag area just the header section
                Rect dragRect = new Rect(0, 0, UIs._windowRect.width, 40);

                // Check for mousedown event inside the drag area
                if (Event.current.type == EventType.MouseDown &&
                    Event.current.button == 0 &&
                    dragRect.Contains(Event.current.mousePosition))
                {
                    UIs._isDragging = true;
                    UIs._dragOffset = new Vector2(
                        Event.current.mousePosition.x - UIs._windowRect.x,
                        Event.current.mousePosition.y - UIs._windowRect.y
                    );
                    Event.current.Use(); // Prevent this event from being processed further
                }
                // Handle dragging movement
                else if (UIs._isDragging && Event.current.type == EventType.MouseDrag)
                {
                    // Update window position based on mouse movement
                    UIs._windowRect.x = Event.current.mousePosition.x - UIs._dragOffset.x;
                    UIs._windowRect.y = Event.current.mousePosition.y - UIs._dragOffset.y;

                    // Keep window fully on screen with some padding
                    UIs._windowRect.x = Mathf.Clamp(UIs._windowRect.x, 0, Screen.width - UIs._windowRect.width);
                    UIs._windowRect.y = Mathf.Clamp(UIs._windowRect.y, 0, Screen.height - UIs._windowRect.height);

                    Event.current.Use(); // Prevent this event from being processed further
                }
                // Handle end of dragging
                else if (Event.current.type == EventType.MouseUp && UIs._isDragging)
                {
                    UIs._isDragging = false;
                    Event.current.Use(); // Prevent this event from being processed further
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Window dragging error: {ex.Message}");
            }
        }
    }
}
