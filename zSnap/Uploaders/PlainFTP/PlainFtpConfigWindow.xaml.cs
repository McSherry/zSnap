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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Interop;

using zSnap.API.Storage;
using zSnap.API.Notification;

namespace zSnap.Uploaders.PlainFTP
{
    /// <summary>
    /// Interaction logic for PlainFtpConfigWindow.xaml
    /// </summary>
    public partial class PlainFtpConfigWindow : Window
    {
        private Namespace Settings;
        private bool pCanClose;

        public PlainFtpConfigWindow()
        {
            InitializeComponent();

            this.Settings = Storage.GetNamespace(Entry.NAMESPACE);
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

            this.FLengthLabel.Content = String
                .Format(
                    "Filename Length ({0} - {1})", 
                    PlainFtpUploader.SETTING_FTP_NLENMIN,
                    PlainFtpUploader.SETTING_FTP_NLENMAX
                );
        }

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            IntPtr hWnd = new WindowInteropHelper(this).Handle;

            Interop.SetWindowLong(
                hWnd,
                Interop.GWL_STYLE,
                Interop.WS_CAPTION | Interop.WS_BORDER
            );

            #region Fill input boxes
            string ftpUri, epUri, ftpUser;
            bool useActive;
            ushort flen;
            if (!Settings.RetrieveSafe<string>(PlainFtpUploader.SETTING_FTP_HOST, out ftpUri))
                ftpUri = null;
            if (!Settings.RetrieveSafe<string>(PlainFtpUploader.SETTING_FTP_ENDPT, out epUri))
                epUri = null;
            if (!Settings.RetrieveSafe<string>(PlainFtpUploader.SETTING_FTP_USER, out ftpUser))
                ftpUser = null;
            if (!Settings.RetrieveSafe<bool>(PlainFtpUploader.SETTING_FTP_MODE, out useActive))
                useActive = false;
            if (!Settings.RetrieveSafe<ushort>(PlainFtpUploader.SETTING_FTP_NLEN, out flen))
                flen = PlainFtpUploader.SETTING_FTP_NLENV;

            if (ftpUri != null) this.HostTBox.Text = ftpUri;
            if (epUri != null) this.EndpointTBox.Text = epUri;
            if (ftpUser != null) this.UserTBox.Text = ftpUser;
            this.UseActive.IsChecked = useActive;
            this.FLengthTBox.Text = flen.ToString();
            #endregion
        }

        private void CancelClickedHandler(object sender, RoutedEventArgs e)
        {
            this.pCanClose = true;
            this.Close();
        }

        private void ClosingHandler(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!this.pCanClose) e.Cancel = true;
        }

        private void SaveClickedHandler(object sender, RoutedEventArgs e)
        {
            #region Ensure non-null/whitespace
            if (
                String.IsNullOrEmpty(this.HostTBox.Text) || String.IsNullOrWhiteSpace(this.HostTBox.Text) ||
                String.IsNullOrEmpty(this.UserTBox.Text) || String.IsNullOrWhiteSpace(this.UserTBox.Text) ||
                String.IsNullOrEmpty(this.EndpointTBox.Text) || String.IsNullOrWhiteSpace(this.EndpointTBox.Text)
                )
                Notifications.Raise(
                    "All fields must be filled.",
                    NotificationType.Error
                );
            #endregion
            #region Verify FTP URI
            UriBuilder ftpUriBuilder = null;

            try
            {
                ftpUriBuilder = new UriBuilder(this.HostTBox.Text);
            }
            catch (UriFormatException)
            {
                Notifications.Raise(
                    "The FTP server URI is invalid.",
                    NotificationType.Error
                );

                return;
            }

            if (ftpUriBuilder.Scheme == "http")
            {
                ftpUriBuilder.Scheme = "ftp";
            }
            else if (ftpUriBuilder.Scheme != "ftp")
            {
                Notifications.Raise(
                    "You must use the 'ftp://' URI scheme.",
                    NotificationType.Error
                );

                return;
            }

            if (ftpUriBuilder.Port < 1024) ftpUriBuilder.Port = 21;
            ftpUriBuilder.Path = String.Empty;

            Uri ftpUri = ftpUriBuilder.Uri;
            #endregion
            #region Verify file-name length
            ushort flen;
            if (!ushort.TryParse(FLengthTBox.Text, out flen))
            {
                Notifications.Raise(
                    "Filename length must be a positive integer.",
                    NotificationType.Error
                );

                return;
            }
            if (
                flen > PlainFtpUploader.SETTING_FTP_NLENMAX ||
                flen < PlainFtpUploader.SETTING_FTP_NLENMIN
            )
            {
                Notifications.Raise(
                    String.Format(
                        "Filename length must be between {0} and {1} characters.",
                        PlainFtpUploader.SETTING_FTP_NLENMIN,
                        PlainFtpUploader.SETTING_FTP_NLENMAX
                    ),
                    NotificationType.Error
                );

                return;
            }
            #endregion

            Uri epUri = null;
            try
            {
                epUri = new UriBuilder(this.EndpointTBox.Text).Uri;
            }
            catch (UriFormatException)
            {
                Notifications.Raise(
                    "The provided endpoint URI is invalid.",
                    NotificationType.Error
                );

                return;
            }

            this.Settings[PlainFtpUploader.SETTING_FTP_HOST] = ftpUri.ToString();
            this.Settings[PlainFtpUploader.SETTING_FTP_USER] = this.UserTBox.Text;
            this.Settings[PlainFtpUploader.SETTING_FTP_ENDPT] = epUri.ToString();
            this.Settings[PlainFtpUploader.SETTING_FTP_MODE] = UseActive.IsChecked.ToString();
            this.Settings[PlainFtpUploader.SETTING_FTP_NLEN] = flen.ToString();

            pCanClose = true;
            this.Close();
        }

        private void ResetClickedHandler(object sender, RoutedEventArgs e)
        {
            Settings.RemoveKey(PlainFtpUploader.SETTING_FTP_USER);
            Settings.RemoveKey(PlainFtpUploader.SETTING_FTP_HOST);
            Settings.RemoveKey(PlainFtpUploader.SETTING_FTP_ENDPT);
            Settings.RemoveKey(PlainFtpUploader.SETTING_FTP_NLEN);

            HostTBox.Clear();
            UserTBox.Clear();
            EndpointTBox.Clear();
            FLengthTBox.Clear();

            pCanClose = true;
            this.Close();
        }

        private void FLengthTBox_TextInput(object sender, TextCompositionEventArgs e)
        {
            if (
                !new [] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }
                .Contains(e.Text)
            )
                e.Handled = true;
        }
    }
}
