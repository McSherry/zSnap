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
using System.Drawing;

namespace zSnap.API
{
    /// <summary>
    /// A base class for implementations of image uploaders.
    /// </summary>
    public abstract class ImageUploader
    {
        private readonly Image pImage;

        /// <summary>
        /// The image supplied for uploading.
        /// </summary>
        protected Image Image
        {
            get
            {
                return this.pImage;
            }
        }

        /// <summary>
        /// The constructor used to set the protected property 'Image'.
        /// </summary>
        /// <param name="image">The image to be uploaded.</param>
        protected ImageUploader(Image image)
        {
            this.pImage = image;
        }
        /// <summary>
        /// A constructor which does not set any properties. Use to retrieve
        /// information, such as Name/SerivceName.
        /// </summary>
        protected ImageUploader() { }

        /// <summary>
        /// The name given to this ImageUploader.
        /// </summary>
        public abstract string Name { get; }
        /// <summary>
        /// The name of the service images are uploaded to.
        /// </summary>
        public abstract string ServiceName { get; }

        /// <summary>
        /// Uploads the image and returns a URI indicating its location.
        /// </summary>
        /// <param name="location">The Uri that will store the location of the uploaded image upon success.</param>
        /// <returns>True if the upload was successful.</returns>
        public abstract bool Upload(out Uri location);
        // Uploaders are responsible for their own error reporting to
        // the user.
    }
}
