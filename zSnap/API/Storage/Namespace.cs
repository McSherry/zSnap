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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace zSnap.API.Storage
{
    /// <summary>
    /// A class representing a name-space within the application's storage.
    /// </summary>
    public class Namespace
    {
        private bool pLocked;
        private Dictionary<string, string> pValues;
        private string pName;

        /// <summary>
        /// Creates a new empty name-space with the specified name.
        /// </summary>
        /// <param name="name">The name to assign this name-space.</param>
        public Namespace(string name)
        {
            this.pName = name;
            this.pValues = new Dictionary<string, string>();
        }
        /// <summary>
        /// Creates a new name-space with the provided contents.
        /// </summary>
        /// <param name="name">The name to assign to this name-space.</param>
        /// <param name="values">The contents to fill the name-space with.</param>
        public Namespace(string name, Dictionary<string, string> values)
            : this(name)
        {
            this.pValues = new Dictionary<string, string>(values);
            Storage.Writeback();
        }

        /// <summary>
        /// The name of this name-space.
        /// </summary>
        public string Name
        {
            get { return this.pName; }
        }
        /// <summary>
        /// A list containing all keys present within this name-space.
        /// </summary>
        public List<string> Keys
        {
            get { return this.pValues.Keys.ToList(); }
        }
        /// <summary>
        /// Whether this name-space is locked and should not be deleted.
        /// </summary>
        internal bool Locked
        {
            get { return this.pLocked; }
        }

        /// <summary>
        /// Retrieves the specified key's value from the name-space.
        /// </summary>
        /// <param name="key">The key to retrieve.</param>
        /// <returns>The value of the key if successful, null if otherwise.</returns>
        public string this[string key]
        {
            get { return this.pValues.ContainsKey(key) ? this.pValues[key] : null; }
            set {
                if (this.pValues.ContainsKey(key)) this.pValues[key] = value;
                else this.pValues.Add(key, value);
                Storage.Writeback();
            }
        }

        /// <summary>
        /// Removes the specified key from the name-space.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        public void RemoveKey(string key)
        {
            if (this.pValues.ContainsKey(key)) this.pValues.Remove(key);
        }

        /// <summary>
        /// Locks the name-space to instruct the storage manager to not delete it.
        /// </summary>
        internal void Lock()
        {
            this.pLocked = true;
        }
        /// <summary>
        /// Unlocks the name-space to inform the storage manager that this name-space can be deleted.
        /// </summary>
        internal void Unlock()
        {
            this.pLocked = false;
        }
    }
}
