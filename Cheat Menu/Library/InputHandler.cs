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
                Event e = Event.current;

                if (e == null || (e.type != EventType.MouseDown && e.type != EventType.MouseDrag && e.type != EventType.MouseUp))
                    return;

                Vector2 mousePos = GUIUtility.GUIToScreenPoint(e.mousePosition);
                Rect dragRect = new Rect(UIs._windowRect.x, UIs._windowRect.y, UIs._windowRect.width, 40);

                switch (e.type)
                {
                    case EventType.MouseDown:
                        if (e.button == 0 && dragRect.Contains(mousePos))
                        {
                            UIs._isDragging = true;
                            UIs._dragOffset = mousePos - new Vector2(UIs._windowRect.x, UIs._windowRect.y);
                            e.Use();
                        }
                        break;

                    case EventType.MouseDrag:
                        if (UIs._isDragging)
                        {
                            UIs._windowRect.position = mousePos - UIs._dragOffset;

                            // Clamp to screen
                            UIs._windowRect.x = Mathf.Clamp(UIs._windowRect.x, 0, Screen.width - UIs._windowRect.width);
                            UIs._windowRect.y = Mathf.Clamp(UIs._windowRect.y, 0, Screen.height - UIs._windowRect.height);

                            e.Use();
                        }
                        break;

                    case EventType.MouseUp:
                        if (UIs._isDragging)
                        {
                            UIs._isDragging = false;
                            e.Use();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[Window Drag Error] {ex}");
            }
        }
    }
}
