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
using System.Windows.Forms;

namespace zSnap.API.Notification
{
    /// <summary>
    /// Interaction logic for NotificationWindow.xaml
    /// </summary>
    public partial class NotificationWindow : Window
    {
        private IntPtr hWnd;
        private HwndSource hWndSrc;

        private void SetWindowStyle()
        {
            Interop.SetWindowLong(hWnd, -16, Interop.WS_THICKFRAME | Interop.WS_BORDER | Interop.WS_DISABLED);
        }
        private IntPtr WndProc(IntPtr hWnd, int uMsg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (uMsg == Interop.WM_NCHITTEST)
            {
                handled = true;
                return new IntPtr(Interop.HTCLIENT);
            }
            else return IntPtr.Zero;
        }

        public NotificationWindow()
        {
            InitializeComponent();

            var waBounds = Screen.PrimaryScreen.WorkingArea;
            var scBounds = Screen.PrimaryScreen.Bounds;

            var top = default(double);
            var left = default(double);

            // Taskbar on top of screen
            if (waBounds.Y > 0)
            {
                top = (scBounds.Height - waBounds.Height) + 8;
                left = waBounds.Width - this.Width - 8;
            }
            // Taskbar on bottom of screen
            else if (
                waBounds.Y == 0 &&
                waBounds.Height != scBounds.Height
            )
            {
                left = waBounds.Width - this.Width - 8;
                top = waBounds.Height - this.Height - 8;
            }
            // Taskbar on left or right
            else if (waBounds.Height == scBounds.Height)
            {
                top = waBounds.Height - this.Height - 8;

                // Taskbar on right
                if (waBounds.X == 0)
                {
                    left = waBounds.Width - this.Width - 8;
                }
                // Taskbar on left
                else
                {
                    left = (scBounds.Width - waBounds.Width) + 8;
                }
            }


            this.Left = left;
            this.Top = top;

            this.Loaded += (s, e) =>
            {
                this.hWnd = new WindowInteropHelper(this).Handle;
                this.hWndSrc = PresentationSource.FromVisual(this) as HwndSource;
                this.hWndSrc.AddHook(WndProc);

                SetWindowStyle();
            };
        }

        [STAThread]
        public void Show(int delay)
        {
            System.Timers.Timer t = new System.Timers.Timer(delay);

            t.Elapsed += (s, e) =>
            {
                t.Stop();
                this.Dispatcher.Invoke(() => 
                {
                    this.Close();
                });
                t.Close();
                t.Dispose();
            };

            base.Show();
            t.Start();
        }
    }
}
