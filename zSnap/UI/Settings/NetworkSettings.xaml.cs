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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;

using zSnap.API;
using zSnap.API.Storage;

namespace zSnap.UI.Settings
{
    /// <summary>
    /// Interaction logic for NetworkSettings.xaml
    /// </summary>
    public partial class NetworkSettings : UserControl, ISettingsPage
    {
        private static Namespace pSettings;

        static NetworkSettings()
        {
            pSettings = Storage.GetNamespace(Entry.NAMESPACE);
        }

        public NetworkSettings()
        {
            InitializeComponent();

            #region Configure Selected Hosting Service
            this.HostSelect.SelectionChanged += HandleHostSelection;

            foreach (string service in Uploader.Services)
            {
                this.HostSelect.Items.Add(service);
            }

            if (!pSettings.Keys.Contains(Entry.SETTING_HOST) || !Uploader.Services.Contains(pSettings[Entry.SETTING_HOST]))
            {
                this.HostSelect.SelectedItem = Uploader.Services.ElementAt(0);
            }
            else
            {
                this.HostSelect.SelectedItem = pSettings[Entry.SETTING_HOST];
            }
            #endregion
            #region Configure Updates Checkbox
            bool checkUpdates;
            if (!pSettings.RetrieveSafe<bool>(Entry.SETTING_UPDATES, out checkUpdates))
                checkUpdates = bool.Parse(Entry.DefaultSettings[Entry.SETTING_UPDATES]);

            this.CheckUpdates.IsChecked = checkUpdates;
            #endregion

            this.HostConfigure.Click += HandleHostConfigure;
        }

        private void HandleHostConfigure(object sender, EventArgs e)
        {
            Type uploader = Uploader.Types[this.HostSelect.SelectedItem.ToString()];
            IConfigurableUploader imgUpl = Activator.CreateInstance(uploader) as IConfigurableUploader;
            Type winType = imgUpl.Window.GetType();

            var win = Activator.CreateInstance(winType) as Window;
            win.Owner = SettingsWindow.Current;
            Action focuser = new Action(() => win.Activate());

            Entry.PreventNotifyClick += focuser;
            win.ShowDialog();
            Entry.PreventNotifyClick -= focuser;
        }
        private void HandleHostSelection(object sender, SelectionChangedEventArgs e)
        {
            Type uploader = Uploader.Types[this.HostSelect.SelectedItem.ToString()];
            this.HostConfigure.IsEnabled = typeof(IConfigurableUploader).IsAssignableFrom(uploader);
        }

        void ISettingsPage.Save()
        {
            pSettings[Entry.SETTING_HOST] = this.HostSelect.SelectedItem.ToString();
            pSettings[Entry.SETTING_UPDATES] = this.CheckUpdates.IsChecked.ToString();
        }

        private void UpdateCheck_Click(object sender, RoutedEventArgs e)
        {
            UpdateInfo update;
            Metadata.CheckNewVersion(out update);
            if (update.IsAvailable)
            {
                new UpdateAvailableDisplayWindow(update).ShowDialog();
            }
            else
            {
                API.Notification.Notifications.Raise(
                    "No update currently available.",
                    API.Notification.NotificationType.Miscellaneous
                );
            }
        }
    }
}
