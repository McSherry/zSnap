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
using System.Windows.Navigation;
using System.Windows.Shapes;

using zSnap.API;
using zSnap.API.Notification;

namespace zSnap.Uploaders.picBox
{
    /// <summary>
    /// Interaction logic for picBoxEnterPinPanel.xaml
    /// </summary>
    public partial class picBoxEnterPinPanel : UserControl
    {
        public picBoxEnterPinPanel()
        {
            InitializeComponent();
        }

        private void Authenticate_Click(object sender, RoutedEventArgs e)
        {
            Authenticate.IsEnabled = false;

            var code = picBoxUploader.PAuthenticator.Authenticate(PinBox.Text);

            if (code == System.Net.HttpStatusCode.OK)
            {
                picBoxConfWindow.Instance.DisplayCtrl.Content = new picBoxAuthSuccessPanel();
            }
            else if (code == System.Net.HttpStatusCode.BadRequest)
            {
                PinBox.Clear();
                Notifications.Raise(
                    "The entered PIN is invalid.",
                    NotificationType.Error
                );
            }
            else
            {
                Notifications.Raise(
                    String.Format(
                        "{0} ({1}).",
                        "An error occurred when during authentication",
                        Enum.GetName(typeof(System.Net.HttpStatusCode), code)
                    ),
                    NotificationType.Error
                );
            }
        }
    }
}
