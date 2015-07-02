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
using System.Windows.Navigation;
using System.Windows.Shapes;

using zSnap.API;
using zSnap.API.Storage;
using zSnap.API.Notification;

namespace zSnap.Uploaders.Imgur
{
    /// <summary>
    /// Interaction logic for ImgurConfigAuthEnterPin.xaml
    /// </summary>
    public partial class ImgurConfigAuthEnterPin : UserControl
    {
        public ImgurConfigAuthEnterPin()
        {
            InitializeComponent();
        }

        private void PinEnterClickedHandler(object sender, RoutedEventArgs e)
        {
            if (!Interop.InternetCheckConnection())
            {
                Notifications.Raise(
                    "An Internet connection is required for authentication.",
                    NotificationType.Error
                );

                return;
            }

            if (String.IsNullOrEmpty(this.PinBox.Text) || String.IsNullOrWhiteSpace(this.PinBox.Text))
            {
                Notifications.Raise(
                    "You must enter a PIN before confirming.",
                    NotificationType.Error
                );
            }
            else
            {
                ImgurUploader.OAuthTokenResponse response;
                var status = ImgurUploader.OAuthenticator.Tokenise(this.PinBox.Text, out response);

                if (status == System.Net.HttpStatusCode.OK)
                {
                    ImgurUploader.OAuthenticator.Previous = response;
                    ImgurUploader.OAuthenticator.AuthToken = response.AccessToken;
                    ImgurUploader.OAuthenticator.AuthExpiry = DateTime.Now.AddSeconds(response.ExpiresIn);

                    Namespace Settings = Storage.GetNamespace(Entry.NAMESPACE);

                    Settings[ImgurUploader.SETTING_AUTHREFRESH] = response.RefreshToken;

                    ImgurConfigWindow.Instance.AuthContent.Content = new ImgurConfigAuthConfirmPanel();
                }
                else if (status == System.Net.HttpStatusCode.BadRequest)
                {
                    this.PinBox.Clear();
                    Notifications.Raise(
                        "The PIN you entered was invalid or had expired.",
                        NotificationType.Error
                    );
                }
                else
                {
                    Notifications.Raise(
                        "An error occurred whilst attempting to authenticate.",
                        NotificationType.Error
                    );
                }
            }
        }
    }
}
