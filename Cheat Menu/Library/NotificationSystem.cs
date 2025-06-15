using UnityEngine;
using static Modern_Cheat_Menu.Core;

namespace Modern_Cheat_Menu.Library
{
    public class NotificationSystem
    {
        #region Notification System

        public enum NotificationType
        {
            Info,
            Success,
            Warning,
            Error
        }

        public struct Notification
        {
            public string Title;
            public string Message;
            public NotificationType Type;
            public float Time;
            public float Alpha;
            public float PositionY;
        }

        public void ShowNotification(string title, string message, NotificationType type)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                Time = Time.realtimeSinceStartup,
                Alpha = 0f
            };

            ModStateS._notifications.Enqueue(notification);

            // Limit queue size
            if (ModStateS._notifications.Count > 5)
            {
                ModStateS._notifications.Dequeue();
            }
        }

        public void UpdateNotifications()
        {
            // Process queue
            if (ModStateS._notifications.Count > 0 && ModStateS._activeNotifications.Count < 3)
            {
                ModStateS._activeNotifications.Add(ModStateS._notifications.Dequeue());
            }

            // Update active notifications
            for (int i = ModStateS._activeNotifications.Count - 1; i >= 0; i--)
            {
                var notification = ModStateS._activeNotifications[i];
                float elapsed = Time.realtimeSinceStartup - notification.Time;

                // Fade in
                if (elapsed < 0.3f)
                {
                    notification.Alpha = Mathf.Lerp(0, 1, elapsed / 0.3f);
                }
                // Stay visible
                else if (elapsed < ModStateS._notificationDisplayTime)
                {
                    notification.Alpha = 1f;
                }
                // Fade out
                else if (elapsed < ModStateS._notificationDisplayTime + ModStateS._notificationFadeTime)
                {
                    notification.Alpha = Mathf.Lerp(1, 0, (elapsed - ModStateS._notificationDisplayTime) / ModStateS._notificationFadeTime);
                }
                // Remove
                else
                {
                    ModStateS._activeNotifications.RemoveAt(i);
                    continue;
                }

                // Update position
                float targetY = Screen.height - 100 - (i * 70);
                if (notification.PositionY == 0)
                {
                    // Initial position
                    notification.PositionY = targetY;
                }
                else
                {
                    // Smooth movement
                    notification.PositionY = Mathf.Lerp(notification.PositionY, targetY, Time.deltaTime * 5f);
                }

                ModStateS._activeNotifications[i] = notification;
            }
        }

        public void DrawNotifications()
        {
            if (ModStateS._activeNotifications.Count == 0)
                return;

            GUIStyle notifStyle = UIs._statusStyle ?? GUI.skin.box;
            if (notifStyle == null)
                return;

            foreach (var notification in ModStateS._activeNotifications)
            {
                Rect notifRect = new Rect(
                    Screen.width - 320f,
                    notification.PositionY,
                    300f,
                    60f
                );

                // Background
                GUI.color = new Color(1, 1, 1, notification.Alpha);
                GUI.Box(notifRect, "");

                // Content
                GUILayout.BeginArea(notifRect);

                // Header
                GUILayout.BeginHorizontal();
                GUILayout.Label(notification.Title, GUILayout.Width(280));
                GUILayout.EndHorizontal();

                // Message
                GUILayout.Label(notification.Message);

                GUILayout.EndArea();

                GUI.color = Color.white;
            }
        }

        // Notification Helpers

        public void ShowSuccess(string title, string message)
        {
            Notifier.ShowNotification(title, message, NotificationSystem.NotificationType.Success);
        }

        public void ShowError(string title, string message)
        {
            Notifier.ShowNotification(title, message, NotificationSystem.NotificationType.Error);
        }


        #endregion
    }
}
