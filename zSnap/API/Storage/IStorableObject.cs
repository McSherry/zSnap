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

namespace zSnap.API.Storage
{
    /// <summary>
    /// An contract for objects that will be stored in zSnap's storage.
    /// </summary>
    public interface IStorableObject
    {
        /*
         *      There is no restriction on the type of representation
         *      used by IStorableObjects, as they will be stored in
         *      Base64 by the Storage API.
         *      
         *      This means that any format may be used, including binary,
         *      plain-text, XML, JSON, and so on and so forth.
         */

        /// <summary>
        /// The name of the object instance.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Converts a stored representation into an IStorableObject.
        /// </summary>
        /// <param name="xmlString">The stored representation of the object.</param>
        /// <returns>An IStorableObject equivalent to the passed representation.</returns>
        void Parse(byte[] formatBytes);

        /// <summary>
        /// Retrieves the representation that will be stored by the
        /// Storage API.
        /// </summary>
        /// <returns>The bytes representing the object.</returns>
        byte[] GetRepresentation();
    }
}
