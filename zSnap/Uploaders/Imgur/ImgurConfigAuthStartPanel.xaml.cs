﻿/*
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

namespace zSnap.Uploaders.Imgur
{
    /// <summary>
    /// Interaction logic for ImgurConfigAuthStartPanel.xaml
    /// </summary>
    public partial class ImgurConfigAuthStartPanel : UserControl
    {
        public ImgurConfigAuthStartPanel()
        {
            InitializeComponent();
        }

        private void AuthButtonClicked(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(
                String.Format(
                    "{0}/authorize?client_id={1}&response_type={2}",
                    ImgurUploader.API_OA_BASE, ApiKeys.IMGURv3_CLIENT_ID,
                    ImgurUploader.API_OA_GRANTTYPE
                )
            );

            ImgurConfigWindow.Instance.AuthContent.Content = new ImgurConfigAuthEnterPin();
            ImgurConfigWindow.Instance.Activate();
        }
    }
}
