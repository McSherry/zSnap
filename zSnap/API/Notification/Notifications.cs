/*
 * Copyright 2014-2016 (c) Liam McSherry <mcsherry.liam@gmail.com>
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at:
 * 
 *      http://www.apache.org/licenses/LICENSE-2.0
 *      
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace zSnap.API.Notification
{
    /// <summary>
    /// Predefined templates for notifications, by type.
    /// </summary>
    public enum NotificationType
    {
        Success,
        Error,
        Miscellaneous
    }

    /// <summary>
    /// A class which allows the raising of notifications through zSnap's notification system.
    /// </summary>
    public static class Notifications
    {
        private static Dictionary<NotificationType, Type> Presets
            = new Dictionary<NotificationType, Type>()
            {
                { NotificationType.Error, typeof(ErrorNotificationPreset) },
                { NotificationType.Success, typeof(SuccessNotificationPreset) },
                { NotificationType.Miscellaneous, typeof(MiscNotificationPreset) }
            };

        /// <summary>
        /// Creates a notification using a predefined preset and message.
        /// </summary>
        /// <param name="message">The message to provide with the notification.</param>
        /// <param name="type">The preset to use.</param>
        public static void Raise(string message, NotificationType type)
        {
            UserControl uc = Activator.CreateInstance(Presets[type], message as object) as UserControl;

            Raise(uc, 3500);
        }
        /// <summary>
        /// Raises a notification with a UserControl as the contents of the notification.
        /// </summary>
        /// <param name="control">The UserControl to use as the contents.</param>
        /// <param name="timeout">The number of milliseconds for the notification to remain visible.</param>
        public static void Raise(UserControl control, int timeout)
        {
            NotificationWindow nw = new NotificationWindow();
            nw.WindowContent.Content = control;

            nw.Show(timeout);
        }
    }
}
