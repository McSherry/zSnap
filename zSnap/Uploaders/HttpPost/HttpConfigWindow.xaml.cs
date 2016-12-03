/*
 * Copyright 2014-2016 (c) Johan Geluk <johan@jgeluk.net>
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
using System.Windows;
using System.Windows.Controls;

namespace zSnap.Uploaders.HttpPost
{
    /// <summary>
    /// Interaction logic for HttpConfigWindow.xaml
    /// </summary>
    public partial class HttpConfigWindow : Window
    {
        private int lastAllowedFileNameLength;

        public HttpConfigWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            InitializeComponent();
            lastAllowedFileNameLength = Configuration.FilenameLength;
            FilenameLengthTbx.Text = Configuration.FilenameLength.ToString();
            UseAuthCheckBox.IsChecked = Configuration.UseBasicAuth;
            AuthGroupBox.IsEnabled = Configuration.UseBasicAuth;
            UserTBox.Text = Configuration.Username;
            PasswordTBox.Text = Configuration.Password;
            MethodComboBox.SelectedIndex = Configuration.UploadMethod == "POST" ? 0 : 1;
            EndpointTBox.Text = Configuration.Destination;
            UseRegexCheckBox.IsChecked = Configuration.UseRegex;
            RegexTBox.IsEnabled = Configuration.UseRegex;
            RegexTBox.Text = Configuration.Regex;
            UrlTBox.Text = Configuration.CustomUrl;

            switch (Configuration.ImageUrlMethod)
            {
                case "FromResponse":
                    UrlFromReponse.IsChecked = true;
                    break;
                case "FromRequest":
                    UrlFromRequest.IsChecked = true;
                    break;
                case "CustomUrl":
                    CustomUrl.IsChecked = true;
                    break;
            }
            HandleCheckboxes();
        }

        private void HandleCheckboxes()
        {
            var v = UrlFromReponse.IsChecked.GetValueOrDefault();
            UseRegexCheckBox.IsEnabled = v;
            RegexTBox.IsEnabled = v;

            UrlTBox.IsEnabled = CustomUrl.IsChecked.GetValueOrDefault();
        }

        private string GetImageUrlMethod()
        {
            if (UrlFromReponse.IsChecked.GetValueOrDefault())
            {
                return "FromResponse";
            }
            if (UrlFromRequest.IsChecked.GetValueOrDefault())
            {
                return "FromRequest";
            }
            if (CustomUrl.IsChecked.GetValueOrDefault())
            {
                return "CustomUrl";
            }
            return "";
        }

        public void AuthCheckedHandler(object sender, RoutedEventArgs e)
        {
            AuthGroupBox.IsEnabled = true;
        }

        private void AuthUncheckedHandler(object sender, RoutedEventArgs e)
        {
            AuthGroupBox.IsEnabled = false;
        }

        public void RegexCheckedHandler(object sender, RoutedEventArgs e)
        {
            RegexTBox.IsEnabled = true;
        }

        private void RegexUncheckedHandler(object sender, RoutedEventArgs e)
        {
            RegexTBox.IsEnabled = false;
        }

        private void ImageUrlMethodChangedHandler(object sender, RoutedEventArgs e)
        {
            HandleCheckboxes();
        }

        private void SaveClickedHandler(object sender, RoutedEventArgs e)
        {
            Configuration.Username = UserTBox.Text;
            Configuration.Password = PasswordTBox.Text;
            Configuration.UseBasicAuth = UseAuthCheckBox.IsChecked ?? false;
            Configuration.Destination = EndpointTBox.Text;
            Configuration.UploadMethod = ((ComboBoxItem)MethodComboBox.SelectedItem).Name;
            Configuration.UseRegex = UseRegexCheckBox.IsChecked ?? false;
            Configuration.Regex = RegexTBox.Text;
            Configuration.CustomUrl = UrlTBox.Text;
            Configuration.ImageUrlMethod = GetImageUrlMethod();
            Configuration.FilenameLength = lastAllowedFileNameLength;
            Close();
        }

        private void CancelClickedHandler(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ResetClickedHandler(object sender, RoutedEventArgs e)
        {
            UserTBox.Text = PasswordTBox.Text = EndpointTBox.Text = RegexTBox.Text = "";
            UseAuthCheckBox.IsChecked = true;
            UseRegexCheckBox.IsChecked = false;
        }

        private void FileNameLengthTextChangedHandler(object sender, TextChangedEventArgs e)
        {
            int res;
            if (int.TryParse(FilenameLengthTbx.Text, out res))
            {
                lastAllowedFileNameLength = res;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(FilenameLengthTbx.Text))
                {
                    lastAllowedFileNameLength = 0;
                    FilenameLengthTbx.Text = "";
                }
                else
                {
                    FilenameLengthTbx.Text = lastAllowedFileNameLength.ToString();
                }
            }
        }
    }
}
