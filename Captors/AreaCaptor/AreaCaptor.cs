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
using System.Drawing;
using System.IO;

namespace zSnap.Captors.AreaCaptor
{
    /// <summary>
    /// Captures a specific area of the screen.
    /// </summary>
    public class AreaCaptor : zSnap.API.ImageCaptor
    {
        private const int BORDER_WIDTH = 2;

        public override string Name
        {
            get { return "Area"; }
        }

        public override bool Capture(out Image image)
        {
            var acw = new AreaCaptureWindow();
            acw.ShowInTaskbar = true;

            var result = acw.ShowDialog();

            if (result != null && (bool)result)
            {
                // Copies bitmap data from the screen, accounting for the capture window's
                // 2px-wide borders.
                Bitmap scBmp = new Bitmap(
                    (int)acw.Width - (BORDER_WIDTH * 2),
                    (int)acw.Height - (BORDER_WIDTH * 2),
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb
                );
                Graphics scGfx = Graphics.FromImage(scBmp);
                scGfx.CopyFromScreen(
                    new Point(
                        (int)acw.Left + BORDER_WIDTH,
                        (int)acw.Top + BORDER_WIDTH
                    ),
                    new Point(0, 0),
                    new Size(scBmp.Width, scBmp.Height),
                    CopyPixelOperation.SourceCopy
                );

                scGfx.Dispose();

                using (MemoryStream ms = new MemoryStream())
                {
                    scBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                    image = new Bitmap(ms);
                    scBmp.Dispose();
                }

                return true;
            }
            else
            {
                image = null;
                return false;
            }
        }
    }
}
