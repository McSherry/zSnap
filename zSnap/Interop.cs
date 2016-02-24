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
using System.Runtime.InteropServices;
using System.Windows;
using SWC = System.Windows.Controls;
using SWM = System.Windows.Media;
using SD = System.Drawing;
using SN = System.Net;

namespace zSnap
{
    public static class Interop
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct AppBarRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct AppBarData
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public AppBarRect rc;
            public int lParam;
        }

        internal class Taskbar
        {
            private const string CLASSNAME = "Shell_TrayWnd";

            private const uint
                NEW                 = 0x00000000,
                REMOVE              = 0x00000001,
                QUERYPOS            = 0x00000002,
                SETPOS              = 0x00000003,
                GETSTATE            = 0x00000004,
                GETTASKBARPOS       = 0x00000005,
                ACTIVATE            = 0x00000006,
                GETAUTOHIDEBAR      = 0x00000007,
                SETAUTOHIDEBAR      = 0x00000008,
                WINDOWPOSCHANGED    = 0x00000009,
                SETSTATE            = 0x0000000A;

            public enum TaskbarPosition
            {
                Unknown = -1,
                Top     = 1,
                Left    = 0,
                Right   = 2,
                Bottom  = 3
            }

            public SD.Rectangle Bounds { get; private set; }
            public SD.Point Location
            {
                get
                {
                    return this.Bounds.Location;
                }
            }
            public SD.Size Size
            {
                get { return this.Bounds.Size; }
            }
            public TaskbarPosition Position { get; private set; }

            public bool Autohide { get; private set; }

            public Taskbar()
            {
                IntPtr hTaskbar = FindWindow(CLASSNAME, null);

                AppBarData data = new AppBarData()
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(AppBarData)),
                    hWnd = hTaskbar
                };

                IntPtr result = SHAppBarMessage(GETTASKBARPOS, ref data);

                if (result == IntPtr.Zero) throw new InvalidOperationException();

                this.Position = (TaskbarPosition)data.uEdge;
                this.Bounds = SD.Rectangle.FromLTRB(data.rc.Left, data.rc.Top, data.rc.Right, data.rc.Bottom);

                data.cbSize = (uint)Marshal.SizeOf(typeof(AppBarData));
                result = SHAppBarMessage(GETSTATE, ref data);

                int state = result.ToInt32();
                this.Autohide = (state & 1) == 1;
            }
        }

        public const int
            WM_NCHITTEST    = 0x0084,
            HTCLIENT        = 0x0001,
            HTTOPRIGHT      = 0x000E,
            HTBOTTOMLEFT    = 0x0010,
            HTBOTTOMRIGHT   = 0x0011,
            HTCAPTION       = 0x0002,
            HTSYSMENU       = 0x0003,

            WM_SYSCOMMAND   = 0x0112,

            WM_HOTKEY       = 0x0312,
            MOD_ALT         = 0x0001,
            MOD_CONTROL     = 0x0002,
            MOD_SHIFT       = 0x0004,
            MOD_WIN         = 0x0008,
            VK_ONE          = 0x0031,
            VK_TWO          = 0x0032,
            VK_PRTSCRN      = 0x002C,

            WM_MOUSEMOVE    = 0x0200,
            WM_LBUTTONUP    = 0x0202,
            MK_LBUTTON      = 0x0001,

            WM_LBUTTONDOWN  = 0x0201,

            // Thanks, Microsoft.
            // I had to decompile PresentationFramework.dll
            // to find this value, since MSDN doesn't list it.
            // Great developer reference you have there.
            SC_DRAGMOVE     = 0xF012,

            GWL_STYLE       = -16,
            GWL_EXSTYLE     = -20;
        public const uint
            WS_BORDER           = 0x00800000,
            WS_CAPTION          = 0x00C00000,
            WS_CHILD            = 0x40000000,
            WS_CHILDWINDOW      = 0x40000000,
            WS_CLIPCHILDREN     = 0x02000000,
            WS_CLIPSIBLINGS     = 0x04000000,
            WS_DISABLED         = 0x08000000,
            WS_DLGFRAME         = 0x00400000,
            WS_GROUP            = 0x00020000,
            WS_HSCROLL          = 0x00100000,
            WS_ICONIC           = 0x20000000,
            WS_MAXIMIZE         = 0x01000000,
            WS_MAXIMIZEBOX      = 0x00010000,
            WS_MINIMIZE         = 0x20000000,
            WS_MINIMIZEBOX      = 0x00020000,
            WS_OVERLAPPED       = 0x00000000,
            WS_POPUP            = 0x80000000,
            WS_SIZEBOX          = 0x00040000,
            WS_SYSMENU          = 0x00080000,
            WS_TABSTOP          = 0x00010000,
            WS_THICKFRAME       = 0x00040000,
            WS_TILED            = 0x00000000,
            WS_VISIBLE          = 0x10000000,
            WS_VSCROLL          = 0x00200000,

            WS_EX_ACCEPTFILES           = 0x00000010,
            WS_EX_APPWINDOW             = 0x00040000,
            WS_EX_CLIENTEDGE            = 0x00000200,
            WS_EX_COMPOSITED            = 0x02000000,
            WS_EX_CONTEXTHELP           = 0x00000400,
            WS_EX_CONTROLPARENT         = 0x00010000,
            WS_EX_DLGMODALFRAME         = 0x00000001,
            WS_EX_LAYERED               = 0x00080000,
            WS_EX_LAYOUTRTL             = 0x00400000,
            WS_EX_LEFT                  = 0x00000000,
            WS_EX_LEFTSCROLLBAR         = 0x00004000,
            WS_EX_MDICHILD              = 0x00000040,
            WS_EX_NOACTIVE              = 0x08000000,
            WS_EX_NOINHERITLAYOUT       = 0x00100000,
            WS_EX_NOPARENTNOTIFY        = 0x00000004,
            WS_EX_NOREDIRECTIONBITMAP   = 0x00200000,
            WS_EX_RIGHT                 = 0x00001000,
            WS_EX_RIGHTSCROLLBAR        = 0x00000000,
            WS_EX_RTLREADING            = 0x00002000,
            WS_EX_STATICEDGE            = 0x00020000,
            WS_EX_TOOLWINDOW            = 0x00000080,
            WS_EX_TOPMOST               = 0x00000008,
            WS_EX_TRANSPARENT           = 0x00000020,
            WS_EX_WINDOWEDGE            = 0x00000100
            ;
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
        [DllImport("wininet.dll", SetLastError=true)]
        public static extern bool InternetCheckConnection
        (
            string url      = "http://www.example.org", // Should be a URL to check
            uint flags      = 1,                        // FLAG_ICC_FORCE_CONNECTION
            uint reserved   = 0                         // Reserved, must be zero
        );
        [DllImport("user32.dll", SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")]
        public static extern IntPtr GetOpenClipboardWindow();
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        
        [DllImport("user32.dll")]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("shell32.dll")]
        internal static extern IntPtr SHAppBarMessage(uint dwMessage, ref AppBarData pData);

        /// <summary>
        /// Converts a 32-bit UNIX timestamp into a DateTime.
        /// </summary>
        /// <param name="epochTime">The time-stamp to convert.</param>
        /// <returns>A DateTime with the date and time specified by the UNIX timestamp.</returns>
        public static DateTime ToDateTime(this int epochTime)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return epoch.AddSeconds(epochTime);
        }
        /// <summary>
        /// Converts a DateTime to a 64-bit UNIX epoch time-stamp.
        /// </summary>
        /// <param name="time">The DateTime to convert.</param>
        /// <returns>A signed 64-bit UNIX epoch time-stamp.</returns>
        public static long ToEpochTime(this DateTime time)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return Convert.ToInt64((time.ToUniversalTime() - epoch).TotalSeconds);
        }
        /// <summary>
        /// Determines whether a point lies within the bounds of a control.
        /// </summary>
        /// <param name="element">The control to check.</param>
        /// <param name="ancestor">The ancestor of the control.</param>
        /// <param name="location">The point to check.</param>
        /// <returns>True if the point lies within the control.</returns>
        public static bool Within(this FrameworkElement element, SWM.Visual ancestor, Point location)
        {
            Point ctrlPos = element.TransformToAncestor(ancestor).Transform(new Point(0, 0));

            return location.X > ctrlPos.X && location.X < (ctrlPos.X + element.Width) &&
                    location.Y > ctrlPos.Y && location.Y < (ctrlPos.Y + element.Width);
        }
        /// <summary>
        /// Gets the Media Type of a given image.
        /// </summary>
        /// <param name="image">The image to retrieve the media type of.</param>
        /// <returns>The media type of the image.</returns>
        public static string GetMediaType(this SD.Image image)
        {
            var codecs = SD.Imaging.ImageCodecInfo.GetImageEncoders();

            bool contains = codecs.Any(c => c.FormatID == image.RawFormat.Guid);

            if (contains) return codecs.First(c => c.FormatID == image.RawFormat.Guid).MimeType;
            else return "image/x-memory-bmp"; 
            // There isn't a media type for "Memory Bitmap" as it's raw image data. The only callers
            // of this method will know how to handle this (tip: re-encode as PNG).
        }
        /// <summary>
        /// Gets an HttpWebResponse and handles WebExceptions.
        /// </summary>
        /// <param name="request">The request to get the response to.</param>
        /// <returns>The response to the provided request.</returns>
        public static SN.WebResponse GetResponseSafe(this SN.WebRequest request)
        {
            SN.WebResponse response = null;

            try
            {
                response = request.GetResponse();
            }
            catch (SN.WebException wex)
            {
                response = wex.Response;
            }

            return response;
        }
        /// <summary>
        /// Rounds a number to the nearest multiple of the provided number.
        /// </summary>
        /// <param name="orig">The number to round.</param>
        /// <param name="multiple">The multiple to round it to.</param>
        /// <returns>The rounded number.</returns>
        public static int Nearest(this int orig, int multiple)
        {
            if (multiple == 0) return orig;

            int mod = orig % multiple;

            if (mod != 0)
            {
                if (mod >= (multiple / 2)) orig += mod;
                else orig -= mod;
            }

            return orig;
        }

        /// <summary>
        /// Retrieves a value from a namespace safely.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="ns">The namespace to retrieve the value from.</param>
        /// <param name="key">The key to find the associated value for.</param>
        /// <param name="value">The value of the key.</param>
        /// <returns>True if successful.</returns>
        public static bool RetrieveSafe<T>(this API.Storage.Namespace ns, string key, out T value)
        {
            if (!ns.Keys.Contains(key))
            {
                value = default(T);
                return false;
            }
            else
            {
                bool worked = false;
                try
                {
                    value = (T)Convert.ChangeType(ns[key], typeof(T));
                    worked = true;
                }
                // If we can't convert it, it's probably wrong. How or why
                // it's wrong doesn't particularly matter.
                catch (Exception)
                {
                    value = default(T);
                }

                return worked;
            }
        }
    }
}
