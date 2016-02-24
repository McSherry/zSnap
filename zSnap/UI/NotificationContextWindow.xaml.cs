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
using SWF = System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.IO;
using System.Drawing.Imaging;

using zSnap.API;
using zSnap.API.Notification;

namespace zSnap.UI
{
    /// <summary>
    /// Interaction logic for NotificationContextWindow.xaml
    /// </summary>
    public partial class NotificationContextWindow : Window
    {
        internal static IntPtr Handle { get; private set; }

        private Settings.SettingsWindow SettingsWindow;
        private HwndSource hWndSource;

        private const int 
            // Maximum number of attempts to set the clipboard
            CB_ATTEMPTS_MAX = 10,
            // Offset of the displayed window from the taskbar
            DISPLAY_OFFSET  = 8;

        private static int Nearest(int value)
        {
            int neighbour = 36;

            int dif = value % neighbour;

            return dif >= (neighbour / 2) ? (value - dif) + (neighbour / 2) : (value + dif) - (neighbour / 2);
        }

        public NotificationContextWindow()
        {
            InitializeComponent();

            this.ShowActivated = true;
            this.SettingsWindow = null;

            this.Title = String.Empty;

            this.Deactivated += DeactivatedHandler;
            this.Loaded += LoadedHandler;
            this.Closing += ClosingHandler;

            this.Settings.MouseDown += SettingsClickHandler;
            this.File.MouseDown += FolderClickedHandler;
            this.Screenshot.MouseDown += ScreenshotClickedHandler;
            this.Clipboard.MouseDown += ClipboardClickedHandler;
        }

        public new void Show()
        {
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
            this.Topmost = true;

            var cursorLocation = SWF.Cursor.Position;
            var waBounds = SWF.Screen.PrimaryScreen.WorkingArea;
            var scBounds = SWF.Screen.PrimaryScreen.Bounds;
            var cursorNearest32X = Nearest(cursorLocation.X);
            var cursorNearest32Y = Nearest(cursorLocation.Y);

            Interop.Taskbar tb = null;

            try
            {
                tb = new Interop.Taskbar();
            }
            catch (InvalidOperationException)
            {
                this.Left = scBounds.Width - this.Width - DISPLAY_OFFSET;
                this.Top = scBounds.Height - this.Height - DISPLAY_OFFSET;
            }

            if (
                tb.Position == Interop.Taskbar.TaskbarPosition.Bottom ||
                tb.Position == Interop.Taskbar.TaskbarPosition.Top
            )
            {
                // Get the window "near enough" the notification tray icon
                this.Left = (cursorNearest32X - this.Width / 2);
                // If we take the difference between how far left we are and
                // the location of the cursor, we somehow centre it over the
                // notification tray icon.
                //
                // See: http://xkcd.com/323/
                this.Left += cursorLocation.X - this.Left - (this.Width / 2);

                if (this.Left + this.Width > scBounds.Width) 
                    this.Left = scBounds.Width - this.Width - DISPLAY_OFFSET;
            }
            else if (
                tb.Position == Interop.Taskbar.TaskbarPosition.Left ||
                tb.Position == Interop.Taskbar.TaskbarPosition.Right
            )
            {
                this.Top = (cursorNearest32Y - this.Height / 2);
                this.Top += cursorLocation.Y - this.Top - (this.Height / 2);
            }

            if (tb.Position == Interop.Taskbar.TaskbarPosition.Bottom)
            {
                this.Top = scBounds.Height - (tb.Size.Height + DISPLAY_OFFSET + this.Height);
            }
            else if (tb.Position == Interop.Taskbar.TaskbarPosition.Top)
            {
                this.Top = tb.Size.Height + DISPLAY_OFFSET;
            }
            else if (tb.Position == Interop.Taskbar.TaskbarPosition.Right)
            {
                this.Left = scBounds.Width - this.Width - tb.Size.Width - DISPLAY_OFFSET;
            }
            else if (tb.Position == Interop.Taskbar.TaskbarPosition.Left)
            {
                this.Left = tb.Size.Width + DISPLAY_OFFSET;
            }

            base.Show();
            base.Activate();
        }

        private void DeactivatedHandler(object sender, EventArgs e)
        {
            // Location of cursor when the handler fires. We need to find
            // out if the cursor could reasonably be over our notification
            // tray icon. If it might be, let the click handler close the window.
            var cl = SWF.Cursor.Position;

            if (
                cl.X > (this.Left + (this.Width / 2) - 32) &&
                cl.X < (this.Left + (this.Width / 2) + 32) &&
                cl.Y > SWF.Screen.PrimaryScreen.WorkingArea.Height
            )
            {
                return;
            }
            this.Hide();
        }
        private void LoadedHandler(object sender, EventArgs e)
        {
            // We need to wait until the window actually has a handle to do this.
            this.hWndSource = PresentationSource.FromVisual(this) as HwndSource;
            this.hWndSource.AddHook(WndProc);

            Handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;

            Interop.SetWindowLong(Handle, Interop.GWL_STYLE, Interop.WS_THICKFRAME | Interop.WS_BORDER);
            Interop.RegisterHotKey(Handle, Handle.ToInt32(), Interop.MOD_SHIFT, Interop.VK_PRTSCRN);
            Interop.RegisterHotKey(Handle, Handle.ToInt32() + 1, Interop.MOD_CONTROL, Interop.VK_ONE);
        }
        private void ClosingHandler(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void UploadAndClipboardStore(System.Drawing.Image image)
        {
            Uri loc;
            bool success = Uploader.Upload(image, out loc);

            Func<bool> TryClipboard = () =>
            {
                try
                {
                    SWF.Clipboard.SetText(loc.ToString());

                    Notifications.Raise(
                        "The URI of the uploaded image was copied to the clipboard.",
                        NotificationType.Success
                    );

                    return true;
                }
                catch (Exception)
                {

                }

                return false;
            };

            if (success)
            {
                for (int i = 0; i < CB_ATTEMPTS_MAX; i++)
                {
                    if (TryClipboard()) return;
                }

                Notifications.Raise(
                    "Could not copy the URI to the clipboard.",
                    NotificationType.Error
                );
            }

        }
        private void FolderClickedHandler(object sender, EventArgs e)
        {
            SWF.OpenFileDialog selImg = new SWF.OpenFileDialog()
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "Image Files|*.png;*.bmp;*.jpg;*.jpeg;*.tiff;*.tif",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures),
                Multiselect = false,
                SupportMultiDottedExtensions = false,
                ValidateNames = true,
                Title = "Select an Image to Upload",
            };

            Entry.PreventNotifyDisplay = true;
            var result = selImg.ShowDialog();
            Entry.PreventNotifyDisplay = false;

            if (result == SWF.DialogResult.OK)
            {

                FileStream imgFs = null;
                System.Drawing.Image img = null;
                try
                {
                    imgFs = System.IO.File.OpenRead(selImg.FileName);
                    byte[] imgBytes = new byte[imgFs.Length];
                    imgFs.Read(imgBytes, 0, imgBytes.Length);

                    System.Drawing.ImageConverter imgConv = new System.Drawing.ImageConverter();
                    img = imgConv.ConvertFrom(imgBytes) as System.Drawing.Image;

                    UploadAndClipboardStore(img);
                }
                catch (UnauthorizedAccessException)
                {
                    Notifications.Raise(
                        "Could not open the specified file due to inadequate permissions.",
                        NotificationType.Error
                    );
                }
                catch (PathTooLongException)
                {
                    Notifications.Raise(
                        "Could not open the specified file as the path was too long.",
                        NotificationType.Error
                    );
                }
                catch (FileNotFoundException)
                {
                    Notifications.Raise(
                        "The specified file does not exist.",
                        NotificationType.Error
                    );
                }
                catch (DirectoryNotFoundException)
                {
                    Notifications.Raise(
                        "The specified directory does not exist.",
                        NotificationType.Error
                    );
                }
                catch (IOException)
                {
                    Notifications.Raise(
                        "A generic I/O exception occurred when opening the file.",
                        NotificationType.Error
                    );
                }
                finally
                {
                    if (imgFs != null)
                    {
                        imgFs.Close();
                        imgFs.Dispose();
                        img.Dispose();
                    }
                }
            }
        }
        private void ScreenshotClickedHandler(object sender, EventArgs e)
        {
            System.Drawing.Image image;

            if (Uploader.Capture(out image))
                UploadAndClipboardStore(image);
        }
        private void ClipboardClickedHandler(object sender, EventArgs e)
        {
            try
            {
                if (SWF.Clipboard.ContainsImage())
                {
                    var img = SWF.Clipboard.GetImage();

                    // Should fix issue #11
                    this.Hide();

                    UploadAndClipboardStore(img);
                }
                else
                {
                    Notifications.Raise(
                        "Clipboard does not contain image data.",
                        NotificationType.Error
                    );
                }
            }
            catch (Exception)
            {
                Notifications.Raise(
                    "Could not retrieve image from clipboard.",
                    NotificationType.Error
                );
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            IntPtr
                HOTKEY_TAKE_SCREENSHOT  = Handle,
                HOTKEY_UPLOAD_CLIPBOARD = Handle + 1;

            // The window style we're using causes the nice frame without a title bar, but also
            // results in the user being able to resize the window. We don't want this.
            //
            // To prevent the user from resizing the window, we have to make Windows think that
            // the nice border is part of the form/client area of our window, so we handle the
            // message for hit tests and respond saying "this is my client area!" every time.
            if (msg == Interop.WM_NCHITTEST)
            {
                handled = true;
                return new IntPtr(Interop.HTCLIENT);
            }
            else if (msg == Interop.WM_HOTKEY)
            {
                if (wParam == HOTKEY_TAKE_SCREENSHOT)
                {
                    this.ScreenshotClickedHandler(this, new EventArgs());
                    handled = true;
                }
                else if (wParam == HOTKEY_UPLOAD_CLIPBOARD)
                {
                    this.ClipboardClickedHandler(this, new EventArgs());
                    handled = true;
                }
            }
            else
            {
                handled = false;
            }

            return IntPtr.Zero;
        }

        private void SettingsClickHandler(object sender, EventArgs e)
        {
            this.SettingsWindow = new Settings.SettingsWindow();
            this.SettingsWindow.Closed += SettingsClosedHandler;
            this.SettingsWindow.ShowDialog();
        }
        private void SettingsClosedHandler(object sender, EventArgs e)
        {
            this.SettingsWindow = null;
        }
    }
}
