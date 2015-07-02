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

namespace zSnap.UI
{
    /// <summary>
    /// Interaction logic for ImageButton.xaml
    /// </summary>
    public sealed partial class ImageButton : UserControl
    {
        private static readonly SolidColorBrush Default = Brushify(0);
        private static readonly SolidColorBrush Hover   = Brushify(0xFFEEEEEE);
        private static readonly SolidColorBrush Click   = Brushify(0xFFE8E8E8);
        private static readonly SolidColorBrush Active  = Brushify(0xFF2288CC);

        private bool pIsActive;
        private SolidColorBrush 
            DefaultBackground   = Default,
            HoverBackground     = Hover,
            ClickBackground     = Click,
            ActiveBackground    = Active
            ;

        private static SolidColorBrush Brushify(uint argb)
        {
            return new SolidColorBrush(
                    Color.FromArgb(
                        (byte)(argb >> 24), (byte)(argb >> 16),
                        (byte)(argb >> 8), (byte)(argb)
                    )
                );
        }

        public ImageButton()
        {
            InitializeComponent();
            this.Loaded += LoadedHandler;
        }

        private void LoadedHandler(object sender, EventArgs e)
        {
            this.bGrid.Background = DefaultBackground;

            this.MouseEnter += (s, f) => this.bGrid.Background = HoverBackground;
            this.MouseLeave += (s, f) => this.bGrid.Background = DefaultBackground;

            this.MouseDown += (s, f) =>
            {
                if (this.IsMouseOver)
                    this.bGrid.Background = ClickBackground;
                else this.bGrid.Background = DefaultBackground;
            };
            this.MouseUp += (s, f) => this.bGrid.Background = HoverBackground;
        }

        public ImageSource Source
        {
            get { return this.pImg.Source; }
            set { this.pImg.Source = value; }
        }
        public bool IsActive
        {
            get { return this.pIsActive; }
            set
            {
                this.pIsActive = value;
                if (value)
                {
                    HoverBackground = Active;
                    DefaultBackground = Active;
                    ClickBackground = Active;
                    if (Activated != null) Activated(this, new EventArgs());
                }
                else
                {
                    HoverBackground = Hover;
                    DefaultBackground = Default;
                    ClickBackground = Click;
                    if (Deactivated != null) Deactivated(this, new EventArgs());
                    this.bGrid.Background = DefaultBackground;
                }
            }
        }

        public void ToggleActiveState()
        {
            this.IsActive = !this.IsActive;
        }

        public event EventHandler Activated;
        public event EventHandler Deactivated;
    }
}
