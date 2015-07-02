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
using System.Windows.Shapes;
using System.Windows.Interop;

namespace zSnap.Uploaders.PlainFTP
{
    /// <summary>
    /// Interaction logic for PlainFtpPasswordWindow.xaml
    /// </summary>
    public partial class PlainFtpPasswordWindow : Window
    {
        private bool pCanClose;

        public PlainFtpPasswordWindow()
        {
            InitializeComponent();
        }

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            IntPtr hWnd = new WindowInteropHelper(this).Handle;

            Interop.SetWindowLong(
                hWnd,
                Interop.GWL_STYLE,
                Interop.WS_CAPTION | Interop.WS_BORDER
            );
        }

        private void ConfirmClickedHandler(object sender, RoutedEventArgs e)
        {
            PlainFtpUploader.FtpPassword = this.PassTBox.SecurePassword;
            pCanClose = true;
            this.DialogResult = true;
        }
        private void CancelClickedHandler(object sender, RoutedEventArgs e)
        {
            this.pCanClose = true;
            this.DialogResult = false;
        }

        private void ClosingHandler(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!this.pCanClose) e.Cancel = true;
        }
    }
}
