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
using System.ComponentModel;
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

namespace zSnap.Uploaders.Imgur
{
    /// <summary>
    /// Interaction logic for ImgurConfigWindow.xaml
    /// </summary>
    public partial class ImgurConfigWindow : Window
    {
        internal static ImgurConfigWindow Instance { get; private set; }

        private IntPtr hWnd;
        private Namespace Settings;

        public ImgurConfigWindow()
        {
            InitializeComponent();

            Instance = this;

            this.Settings = Storage.GetNamespace(Entry.NAMESPACE);
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

            this.Loaded += LoadedHandler;
            this.Closing += ClosingHandler;

            this.EnableAuth.Checked += EnableAuthCheckedHandler;
            this.EnableAuth.Unchecked += (s, e) =>
                this.AuthContent.Content = new ImgurConfigAuthStartDisabledPanel();

            this.AuthContent.Content = new ImgurConfigAuthStartDisabledPanel();
        }

        private void EnableAuthCheckedHandler(object sender, EventArgs e)
        {
            var status = ImgurUploader.OAuthenticator.Verify();

            if (status == ImgurUploader.AuthStatus.Active)
            {
                if (Interop.InternetCheckConnection())
                {
                    this.AuthContent.Content = new ImgurConfigAuthConfirmPanel();
                }
                else
                {
                    this.AuthContent.Content = new ImgurConfigAuthStartDisabledPanel();

                    API.Notification.Notifications.Raise(
                        "An Internet connection is required to authenticate.",
                        API.Notification.NotificationType.Error
                    );
                }
            }
            else
            {
                this.AuthContent.Content = new ImgurConfigAuthStartPanel();
            }
        }

        private void ClosingHandler(object sender, CancelEventArgs e)
        {
            var status = ImgurUploader.OAuthenticator.Verify();

            if (status == ImgurUploader.AuthStatus.Invalid && (bool)this.EnableAuth.IsChecked)
            {
                this.EnableAuth.IsChecked = false;

                API.Notification.Notifications.Raise(
                    "Authentication not configured: switching back to anonymous mode.",
                    API.Notification.NotificationType.Miscellaneous
                );
            }

            Settings[ImgurUploader.SETTING_ENAUTH] = this.EnableAuth.IsChecked.ToString();
        }
        private void LoadedHandler(object sender, EventArgs e)
        {
            this.hWnd = new WindowInteropHelper(this).Handle;

            Interop.SetWindowLong(
                hWnd, Interop.GWL_STYLE,
                Interop.WS_CAPTION | Interop.WS_BORDER
            );

            #region Set up authentication settings
            ImgurUploader.VerifySettingsPresent();

            bool enableAuth;
            if (!bool.TryParse(Settings[ImgurUploader.SETTING_ENAUTH], out enableAuth))
                enableAuth = bool.Parse(ImgurUploader.DefaultSettings[ImgurUploader.SETTING_ENAUTH]);

            this.EnableAuth.IsChecked = enableAuth;
            #endregion
        }

        private void CloseClickedHandler(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ResetButtonClickHandler(object sender, RoutedEventArgs e)
        {
            Settings.RemoveKey(ImgurUploader.SETTING_AUTHREFRESH);
            this.EnableAuth.IsChecked = false;
        }
    }
}
