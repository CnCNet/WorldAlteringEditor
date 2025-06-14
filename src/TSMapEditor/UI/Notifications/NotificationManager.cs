using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System.Collections.Generic;

namespace TSMapEditor.UI.Notifications
{
    public interface INotificationManager
    {
        void AddNotification(string text);
    }

    internal class NotificationManager : XNAControl, INotificationManager
    {
        public NotificationManager(WindowManager windowManager) : base(windowManager)
        {
            InputEnabled = false;
            Height = WindowManager.WindowHeight;
        }

        private List<Notification> notifications = new List<Notification>();

        public void AddNotification(string text)
        {
            Notification notification = new Notification(WindowManager);
            notification.Text = text;
            notifications.Add(notification);
            AddChild(notification);
        }

        private void RearrangeNotifications()
        {
            int yPos = 0;

            foreach (Notification notification in notifications)
            {
                notification.Y = yPos;
                yPos += notification.Height + Constants.UIVerticalSpacing * 2;
                notification.X = (Width - notification.Width) / 2;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            int i = 0;
            while (i < notifications.Count)
            {
                if (notifications[i].Alpha <= 0)
                {
                    RemoveChild(notifications[i]);
                    notifications.RemoveAt(i);
                    continue;
                }

                i++;
            }

            RearrangeNotifications();
        }
    }
}
