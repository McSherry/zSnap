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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

using zSnap.API;
using zSnap.API.Storage;

namespace zSnap.UI.Settings
{
    /// <summary>
    /// Interaction logic for GeneralSettings.xaml
    /// </summary>
    public partial class GeneralSettings : UserControl, ISettingsPage
    {
        private Namespace pSettings;

        private void QuitHandler(object sender, EventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Are you sure?\n\nThis will close zSnap completely, and you'll need to\n restart zSnap before you can use it again.",
                "Quit zSnap",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No
            );

            if (result == MessageBoxResult.Yes)
            {
                (this as ISettingsPage).Save();
                Entry.Quit();
            }
        }
        private void LoadedHandler(object sender, EventArgs e)
        {
            #region Configure backups
            bool doBackup;
            if (!bool.TryParse(pSettings[Entry.SETTING_DOBACKUP], out doBackup))
            {
                pSettings[Entry.SETTING_DOBACKUP] = Entry.DefaultSettings[Entry.SETTING_DOBACKUP];
                doBackup = bool.Parse(pSettings[Entry.SETTING_DOBACKUP]);
            }
            this.DoBackup.IsChecked = doBackup;
            #endregion
            #region Configure capture modes
            foreach (string key in Uploader.CaptureModes.Keys)
            {
                this.CaptureMode.Items.Add(key);
            }

            if (
                !this.pSettings.Keys.Contains(Entry.SETTING_CAPTUREMODE) ||
                !Uploader.CaptureModes.Keys.Contains(pSettings[Entry.SETTING_CAPTUREMODE])
            )
            {
                pSettings[Entry.SETTING_CAPTUREMODE] = Uploader.CaptureModes.Keys.ElementAt(0);
            }

            this.CaptureMode.SelectedItem = pSettings[Entry.SETTING_CAPTUREMODE];
            #endregion
            #region Configure logging
            bool doLog;
            if (!pSettings.RetrieveSafe<bool>(Entry.SETTING_DOLOGGING, out doLog))
            {
                doLog = bool.Parse(Entry.DefaultSettings[Entry.SETTING_DOLOGGING]);
            }
            this.DoLogging.IsChecked = doLog;
            #endregion
        }

        public GeneralSettings()
        {
            InitializeComponent();

            this.QuitButton.Click += QuitHandler;
            this.Loaded += LoadedHandler;

            this.pSettings = Storage.GetNamespace(Entry.NAMESPACE);
        }

        void ISettingsPage.Save()
        {
            pSettings[Entry.SETTING_DOBACKUP] = this.DoBackup.IsChecked.ToString();
            pSettings[Entry.SETTING_CAPTUREMODE] = this.CaptureMode.SelectedItem.ToString();
            pSettings[Entry.SETTING_DOLOGGING] = this.DoLogging.IsChecked.ToString();
        }

        private void ViewLogButtonClick(object sender, RoutedEventArgs e)
        {
            var logwindow = new UI.ImageLogWindow()
            {
                Owner = SettingsWindow.Current
            };

            Action focus = () => logwindow.Activate();
            Entry.PreventNotifyClick += focus;
            logwindow.ShowDialog();
            Entry.PreventNotifyClick -= focus;
        }
    }
}
