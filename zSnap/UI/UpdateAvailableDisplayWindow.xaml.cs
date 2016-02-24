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
using System.Diagnostics;

namespace zSnap.UI
{
    /// <summary>
    /// Interaction logic for UpdateAvailableDisplayWindow.xaml
    /// </summary>
    public partial class UpdateAvailableDisplayWindow : Window
    {
        private readonly Tuple<Uri, Uri, Uri> URIs;

        public UpdateAvailableDisplayWindow()
        {
            InitializeComponent();
        }

        public UpdateAvailableDisplayWindow(Tuple<Uri, Uri, Uri> updateLinks)
        {
            InitializeComponent();

            this.URIs = updateLinks;
        }

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            IntPtr hWnd = new WindowInteropHelper(this).Handle;

            Interop.SetWindowLong(
                hWnd,
                Interop.GWL_STYLE,
                Interop.WS_BORDER | Interop.WS_CAPTION
            );
        }

        private void InstallClickedHandler(object sender, RoutedEventArgs e)
        {
            Process.Start(this.URIs.Item2.ToString());
            Entry.Quit();
        }

        private void NotesClickedHandler(object sender, RoutedEventArgs e)
        {
            Process.Start(this.URIs.Item3.ToString());
        }

        private void CancelClickedHandler(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
