/*
 * Copyright 2014-2015 (c) Liam McSherry <mcsherry.liam@gmail.com>
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Interop;

using zSnap.API;
using zSnap.API.Storage;
using zSnap.API.Notification;

namespace zSnap.Uploaders.picBox
{
    /// <summary>
    /// Interaction logic for picBoxConfWindow.xaml
    /// </summary>
    public partial class picBoxConfWindow : Window
    {
        internal static picBoxConfWindow Instance { get; set; }
        private static picBoxConfDisabledPanel DisPanel
            = new picBoxConfDisabledPanel();
        private Namespace Settings;
        private bool pCanClose;

        public picBoxConfWindow()
        {
            InitializeComponent();

            Settings = Storage.GetNamespace(Entry.NAMESPACE);
            Instance = this;
        }

        private void LoadHandler(object sender, RoutedEventArgs e)
        {
            IntPtr hWnd = new WindowInteropHelper(this).Handle;

            Interop.SetWindowLong(
                hWnd,
                Interop.GWL_STYLE,
                Interop.WS_BORDER | Interop.WS_CAPTION
            );

            bool useAuth;
            if (!Settings.RetrieveSafe<bool>(picBoxUploader.SETTING_AUTH, out useAuth))
            {
                useAuth = false;
            }
            this.EnableAuth.IsChecked = useAuth;

            if (!useAuth)
            {
                DisplayCtrl.Content = DisPanel;
            }
            else if (picBoxUploader.PAuthenticator.Verify())
            {
                DisplayCtrl.Content = new picBoxAuthSuccessPanel();
            }
            else
            {
                Notifications.Raise(
                    "picBox authentication failed; reauthentication required.",
                    NotificationType.Error
                );

                Settings.RemoveKey(picBoxUploader.SETTING_TOKEN);
                this.EnableAuth.IsEnabled = false;
            }
        }

        private void ClosingHandler(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !pCanClose;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            pCanClose = true;

            if (!Settings.Keys.Contains(picBoxUploader.SETTING_TOKEN))
            {
                Notifications.Raise(
                    "Authentication not configured; disabled.",
                    NotificationType.Miscellaneous
                );

                EnableAuth.IsChecked = false;
            }

            Settings[picBoxUploader.SETTING_AUTH] = EnableAuth.IsChecked.ToString();

            this.Close();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Settings[picBoxUploader.SETTING_AUTH] = false.ToString();
            Settings.RemoveKey(picBoxUploader.SETTING_TOKEN);
            pCanClose = true;
            this.Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            //if (EnableAuth.IsChecked ?? false)
            //{
            //    if (picBoxUploader.PAuthenticator.Verify())
            //    {
            //        Settings[picBoxUploader.SETTING_AUTH] = EnableAuth.IsChecked.ToString();
            //        pCanClose = true;
            //        this.Close();
            //    }
            //    else
            //    {
            //        Settings[picBoxUploader.SETTING_AUTH] = false.ToString();
            //        Notifications.Raise(
            //            "Authentication not completed; disabled.",
            //            NotificationType.Miscellaneous
            //        );
            //    }
            //}
            //else
            //{
            //    Settings[picBoxUploader.SETTING_AUTH] = EnableAuth.IsChecked.ToString();
            //}
        }

        private void AuthEnabledHandler(object sender, RoutedEventArgs e)
        {
            this.DisplayCtrl.Content = new picBoxConfEnabledPanel();
        }

        private void AuthDisabledHandler(object sender, RoutedEventArgs e)
        {
            this.DisplayCtrl.Content = DisPanel;
        }
    }
}
