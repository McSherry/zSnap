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

namespace zSnap.API
{
    /// <summary>
    /// A base class for implementations of screenshot-capture classes.
    /// </summary>
    public abstract class ImageCaptor
    {
        /// <summary>
        /// Parameterless constructor to allow instatiation for information retrieval.
        /// </summary>
        protected ImageCaptor() { }

        /// <summary>
        /// The name given to this form of image capture.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Captures an image using the ImageCaptor.
        /// </summary>
        /// <param name="image">The captured image.</param>
        /// <returns>True if an image was successfully captured.</returns>
        public abstract bool Capture(out Image image);
    }
}
