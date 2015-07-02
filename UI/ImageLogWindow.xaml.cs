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

using zSnap.API;
using zSnap.API.Storage;

namespace zSnap.UI
{
    internal class LoggedImage
    {
        public string Uploaded { get; set; }
        public string Service { get; set; }
        public Uri URI { get; set; }
    }

    /// <summary>
    /// Interaction logic for ImageLogWindow.xaml
    /// </summary>
    public partial class ImageLogWindow : Window
    {
        private void PopulateLogList()
        {
            LogList.Items.Clear();
            if (Storage.ObjectExists<StorableStringList>(Uploader.LOGGING_OBJECT))
            {
                List<LoggedImage> images = new List<LoggedImage>();
                foreach (string image in Storage.GetObject<StorableStringList>(Uploader.LOGGING_OBJECT))
                {
                    List<string> logitem = new List<string>();

                    string current = String.Empty;
                    foreach (char c in image)
                    {
                        if (c == ',')
                        {
                            logitem.Add(current);
                            current = String.Empty;
                        }
                        else if (c == '\r' || c == '\n')
                        {
                            continue;
                        }
                        else
                        {
                            current += c;
                        }
                    }
                    logitem.Add(current);

                    switch (logitem[0])
                    {
                        case "0":
                            if (logitem.Count < 4) continue;

                            int timestamp;
                            if (!int.TryParse(logitem[1], out timestamp)) continue;
                            Uri location;
                            try
                            {
                                location = new Uri(logitem[3]);
                            }
                            catch (Exception)
                            {
                                continue;
                            }

                            images.Add(new LoggedImage()
                            {
                                Uploaded = timestamp
                                            .ToDateTime()
                                            .ToLocalTime()
                                            .ToString("MMMM d, yyyy \\a\\t HH:mm:ss"),
                                Service = logitem[2],
                                URI = location
                            });
                            break;
                        default: continue;
                    }
                }

                foreach (var img in images.OrderBy(
                    i => DateTime.ParseExact(
                            i.Uploaded,
                            @"MMMM d, yyyy \a\t HH:mm:ss",
                            System.Globalization.CultureInfo.InvariantCulture
                        ).ToEpochTime()
                ).Reverse())
                {
                    LogList.Items.Add(img);
                }
            }
        }

        public ImageLogWindow()
        {
            InitializeComponent();
            PopulateLogList();
        }

        public void logItemDblClickHandler(object sender, MouseButtonEventArgs e)
        {
            var clickedLogItem = (sender as ListViewItem).Content as LoggedImage;

            System.Diagnostics.Process.Start(clickedLogItem.URI.ToString());
        }

        private void KeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && LogList.SelectedItems.Count > 0)
            {
                foreach (LoggedImage img in LogList.SelectedItems)
                {
                    System.Diagnostics.Process.Start(img.URI.ToString());
                }
            }
        }

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            IntPtr hWnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            Interop.SetWindowLong(hWnd, Interop.GWL_STYLE, Interop.WS_CAPTION);
        }

        private void ClearClickedHandler(object sender, EventArgs e)
        {
            if (
                MessageBox.Show(
                    this,
                    "Clearing will remove the log of uploaded images, and is " +
                    "irreversible.\n\nDo you wish to proceed?",
                    "Confirm Clear",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No
                ) == MessageBoxResult.Yes
            )
            {
                (zSnap.API.Uploader.ImageLog as ICollection<string>).Clear();
                zSnap.API.Storage.Storage.StoreObject(zSnap.API.Uploader.ImageLog);
                PopulateLogList();
            }
        }
    }
}
