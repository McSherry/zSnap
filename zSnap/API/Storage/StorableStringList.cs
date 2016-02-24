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
using System.Threading.Tasks;

namespace zSnap.API.Storage
{
    /// <summary>
    /// Implements an array which may be stored by zSnap's Storage API.
    /// </summary>
    public class StorableStringList : IStorableObject, ICollection<string>
    {
        private List<string> _content;
        private string _name;

        /// <summary>
        /// Creates a new storable array.
        /// </summary>
        /// <param name="name">The name to store this array under.</param>
        public StorableStringList(string name) {
            _name = name;
            _content = new List<string>();
        }

        /// <summary>
        /// Adds a string to the list.
        /// </summary>
        /// <param name="item">The item to add to the list.</param>
        public void Add(string item)
        {
            _content.Add(item);
        }
        /// <summary>
        /// Adds a range of strings to the list.
        /// </summary>
        /// <param name="item">The range of strings to add.</param>
        public void AddRange(params string[] item)
        {
            _content.AddRange(item);
        }

        string IStorableObject.Name 
        { 
            get { return _name; } 
        }
        byte[] IStorableObject.GetRepresentation()
        {
            List<byte> bRep = new List<byte>();

            foreach (string str in _content)
            {
                byte[] utf = Encoding.UTF8.GetBytes(str);
                byte[] len = BitConverter.GetBytes(utf.Length);

                bRep.AddRange(len);
                bRep.AddRange(utf);
            }

            return bRep.ToArray();
        }
        void IStorableObject.Parse(byte[] formatBytes)
        {
            if (formatBytes.Length < 4)
            {
                _content = new List<string>();
                return;
            }

            int index = 0;
            while (index < formatBytes.Length)
            {
                int len = BitConverter.ToInt32(formatBytes, index);
                index += 4;

                if (len == 0) continue;

                _content.Add(Encoding.UTF8.GetString(formatBytes, index, len));
                index += len;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _content.GetEnumerator();
        }
        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return _content.GetEnumerator();
        }
        int ICollection<string>.Count
        {
            get { return _content.Count; }
        }
        bool ICollection<string>.IsReadOnly
        {
            get { return false; }
        }
        bool ICollection<string>.Remove(string item)
        {
            return _content.Remove(item);
        }
        void ICollection<string>.CopyTo(string[] array, int index)
        {
            _content.CopyTo(array, index);
        }
        bool ICollection<string>.Contains(string needle)
        {
            return _content.Contains(needle);
        }
        void ICollection<string>.Clear()
        {
            _content.Clear();
        }
    }
}
