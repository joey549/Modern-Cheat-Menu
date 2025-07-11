using static Modern_Cheat_Menu.Core;
using Modern_Cheat_Menu.Library;
using UnityEngine;

namespace Modern_Cheat_Menu.ModGUI.UIManager
{
    public class ManagerHelper
    {
        #region SearchBar Helpers
        public static string DrawSearchBarUI(ref string searchText, Rect parentRect, string labelKey = "Search", string cacheKey = "searchField")
        {
            float searchBarY = parentRect.y + parentRect.height + 20f;
            Rect searchRect = new Rect(20f, searchBarY, parentRect.width - 40f, 30f);

            GUI.Label(new Rect(searchRect.x, searchRect.y - 25f, 100f, 25f), $"{labelKey}:", UIs._labelStyle);

            if (!ModData._textFields.TryGetValue(cacheKey, out var searchField))
            {
                searchField = new CustomTextField(searchText, UIs._searchBoxStyle ?? GUI.skin.textField);
                ModData._textFields[cacheKey] = searchField;
            }

            return searchField.Draw(searchRect);
        }
        public static void DrawScrollableGridUI<T>(List<T> items,Rect parentRect,ref Vector2 scrollPos,float itemSize,float spacing,int minColumns,Action<T, Rect> drawButton)
        {
            int columns = Mathf.Max(minColumns, Mathf.FloorToInt((parentRect.width - spacing) / (itemSize + spacing)));
            int totalRows = Mathf.CeilToInt((float)items.Count / columns);
            float contentHeight = totalRows * (itemSize + spacing) + spacing;

            Rect contentRect = new Rect(0f, 0f, parentRect.width - 20f, contentHeight);
            scrollPos = GUI.BeginScrollView(parentRect, scrollPos, contentRect, false, true);

            float yPos = 0f;
            for (int i = 0; i < items.Count; i += columns)
            {
                for (int j = 0; j < columns; j++)
                {
                    int index = i + j;
                    if (index >= items.Count) break;

                    Rect itemRect = new Rect(
                        j * (itemSize + spacing),
                        yPos,
                        itemSize,
                        itemSize
                    );

                    drawButton(items[index], itemRect);
                }
                yPos += itemSize + spacing;
            }

            GUI.EndScrollView();
        }
        #endregion

        public static Rect DrawButtonUI(ref float x, float y, float width, float height, string label, GUIStyle style, Action onClick, float spacing = 0f)
        {
            Rect rect = new Rect(x, y, width, height);
            if (GUI.Button(rect, label, style))
            {
                onClick?.Invoke();
            }
            x += width + spacing;
            return rect;
        }


    }
}
