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
using System.Threading;

namespace zSnap.Captors.AreaCaptor
{
    /// <summary>
    /// Interaction logic for AreaCaptureWindow.xaml
    /// </summary>
    public partial class AreaCaptureWindow : Window
    {
        private IntPtr hWnd;
        private HwndSource hWndSource;

        public AreaCaptureWindow()
        {
            InitializeComponent();

            //this.MouseDown += (s, e) => this.DragMove();
            this.Cancel.MouseEnter += (s, e) =>
                this.Cancel.Fill = new SolidColorBrush(Color.FromRgb(0xB5, 0x30, 0x30));
            this.Cancel.MouseLeave += (s, e) =>
                this.Cancel.Fill = new SolidColorBrush(Color.FromRgb(0xD4, 0x54, 0x54));
            this.Confirm.MouseEnter += (s, e) =>
                this.Confirm.Fill = new SolidColorBrush(Color.FromRgb(0x3C, 0x73, 0x12));
            this.Confirm.MouseLeave += (s, e) =>
                this.Confirm.Fill = new SolidColorBrush(Color.FromRgb(0x5D, 0x97, 0x30));
            this.Cancel.MouseUp += (s, e) =>
            {
                this.DialogResult = false;
                this.Close();
            };
            this.Confirm.MouseUp += (s, e) =>
            {
                this.DialogResult = true;
                this.Close();
            };
            this.Loaded += LoadedHandler;
        }

        private void LoadedHandler(object sender, EventArgs e)
        {
            this.hWnd = new WindowInteropHelper(this).Handle;

            this.hWndSource = PresentationSource.FromVisual(this) as HwndSource;
            this.hWndSource.AddHook(WndProc);
        }
        private unsafe IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            int lParam32 = lParam.ToInt32();
            // Absolute (screen-relative) co-ordinates
            short ax = *(short*)&lParam32, ay = *(((short*)&lParam32) + 1);
            // Relative (window-relative) co-ordinates
            int rx = ax - (int)this.Left, ry = ay - (int)this.Top;
            Point mousePoint = default(Point);

            // Message received on mouse-over
            if (msg == Interop.WM_NCHITTEST)
            {
                mousePoint = new Point(rx, ry);

                if (BRResizer.Within(this, mousePoint))
                {
                    handled = true;
                    return new IntPtr(Interop.HTBOTTOMRIGHT);
                }
                else if (BLResizer.Within(this, mousePoint))
                {
                    handled = true;
                    return new IntPtr(Interop.HTBOTTOMLEFT);
                }
                else if (TRResizer.Within(this, mousePoint))
                {
                    handled = true;
                    return new IntPtr(Interop.HTTOPRIGHT);
                }

                handled = true;
                return new IntPtr(Interop.HTCLIENT);
            }
            else if (msg == Interop.WM_LBUTTONDOWN)
            {
                // WM_LBUTTONDOWN's co-ords are relative to the window,
                // whereas WM_NCHITTEST's are relative to the screen.
                mousePoint = new Point(ax, ay);

                // Make sure we're in the client area of the form.
                // Our handler above will ensure that we are.
                if (
                    Interop.SendMessage(this.hWnd, Interop.WM_NCHITTEST, wParam, lParam).ToInt32()
                    == Interop.HTCLIENT
                )
                {
                    // If we aren't within our two buttons, allow the window to be dragged and
                    // moved by sending the appropriate message.
                    if (!Cancel.Within(this, mousePoint) && !Confirm.Within(this, mousePoint))
                    {
                        Interop.SendMessage(
                            this.hWnd,
                            Interop.WM_SYSCOMMAND,
                            (IntPtr)Interop.SC_DRAGMOVE,
                            IntPtr.Zero
                        );
                    }
                }
            }

            return IntPtr.Zero;
        }
    }
}
