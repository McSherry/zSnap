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
using System.Windows.Shapes;
using System.Reflection;

namespace zSnap.UI.Settings
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        /// <summary>
        /// The currently-open settings window.
        /// </summary>
        internal static SettingsWindow Current { get; private set; }

        private Dictionary<string, BitmapImage> pBitmapCache;
        private ImageButton pActiveButton = default(ImageButton);
        private void TogglePages(ImageButton button)
        {
            if (pActiveButton == null || pActiveButton.Name == button.Name) return;
            else
            {
                pActiveButton.ToggleActiveState();
                button.ToggleActiveState();
                pActiveButton = button;
            }
        }

        private void ClickedHandler(object sender, EventArgs e)
        {
            ImageButton clicked = sender as ImageButton;

            if (clicked != null)
            {
                TogglePages(clicked);
            }
        }
        private void ActivatedHandler(object sender, EventArgs e)
        {
            ImageButton clicked = sender as ImageButton;

            if (clicked != null)
            {
                Type page = Assembly.GetCallingAssembly().GetTypes().FirstOrDefault(
                    t => t.Name == clicked.Name + "Settings" &&
                        typeof(UserControl).IsAssignableFrom(t) &&
                        typeof(ISettingsPage).IsAssignableFrom(t)
                );

                if (page == null)
                {
                    this.SettingsPage.Content = "No page found for tab!";
                }
                else
                {
                    this.SettingsPage.Content = Activator.CreateInstance(page);
                }

                if (!this.pBitmapCache.ContainsKey(clicked.Name + "Active"))
                {
                    this.pBitmapCache.Add(
                        clicked.Name + "Active",
                        new BitmapImage(
                            new Uri(
                                "pack://application:,,,/zSnap;component/Resources/zSnap_settings_" +
                                clicked.Name.ToLower() +
                                "_w.png"
                            )
                        )
                    );
                }

                clicked.Source = this.pBitmapCache[clicked.Name + "Active"];
            }
        }
        private void DeactivatedHandler(object sender, EventArgs e)
        {
            ImageButton clicked = sender as ImageButton;

            if (clicked != null)
            {
                if (!this.pBitmapCache.ContainsKey(clicked.Name + "Inactive"))
                {
                    this.pBitmapCache.Add(
                        clicked.Name + "Inactive",
                        new BitmapImage(
                            new Uri(
                                "pack://application:,,,/zSnap;component/Resources/zSnap_settings_" +
                                clicked.Name.ToLower() +
                                "_b.png"
                            )
                        )
                    );
                }

                clicked.Source = this.pBitmapCache[clicked.Name + "Inactive"];

                if (this.SettingsPage.Content is ISettingsPage)
                    (this.SettingsPage.Content as ISettingsPage).Save();
            }
        }

        private void LoadedHandler(object sender, EventArgs e)
        {
            this.General.IsActive = true;
            this.pActiveButton = this.General;

            Entry.PreventNotifyDisplay = true;
            Entry.PreventNotifyClick += NotifyClick;
        }
        private void ClosingHandler(object sender, EventArgs e)
        {
            Entry.PreventNotifyClick -= NotifyClick;
            Entry.PreventNotifyDisplay = false;

            if (this.SettingsPage.Content is ISettingsPage)
                (this.SettingsPage.Content as ISettingsPage).Save();
        }
        private void NotifyClick()
        {
            this.Activate();
        }

        public SettingsWindow()
        {
            InitializeComponent();

            this.Loaded += LoadedHandler;
            this.Closing += ClosingHandler;

            this.pBitmapCache = new Dictionary<string, BitmapImage>();

            this.General.MouseDown += ClickedHandler;
            this.General.Activated += ActivatedHandler;
            this.General.Deactivated += DeactivatedHandler;

            this.Network.MouseDown += ClickedHandler;
            this.Network.Activated += ActivatedHandler;
            this.Network.Deactivated += DeactivatedHandler;

            this.About.MouseDown += ClickedHandler;
            this.About.Activated += ActivatedHandler;
            this.About.Deactivated += DeactivatedHandler;

            Current = this;
        }
    }
}
