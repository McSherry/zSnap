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

namespace zSnap.UI.Settings
{
    /// <summary>
    /// Interaction logic for AboutSettings.xaml
    /// </summary>
    public partial class AboutSettings : UserControl, ISettingsPage
    {
        public AboutSettings()
        {
            InitializeComponent();

            this.VersionString.Content = String.Format("v{0}", Metadata.FullVersionString);
            this.WebLink.MouseUp += WebClickHandler;
            this.BlogLink.MouseUp += WebClickHandler;
        }

        public void WebClickHandler(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://" + (sender as Label).Content.ToString());
        }

        void ISettingsPage.Save() { }
    }
}
