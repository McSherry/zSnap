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
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Serialization;

namespace zSnap.API.Storage
{
    /// <summary>
    /// The storage manager, which is used to store application settings.
    /// </summary>
    public static class Storage
    {
#if DEBUG
        private const string SETTINGS_FOLDER = "./";
#else
        private static string SETTINGS_FOLDER 
            = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/zSnap/";
#endif
        private static string SETTINGS_FILE = "zSnap.xml";
        private static FileStream pStorageFileStream;
        internal static List<Namespace> pStore;
        internal static Dictionary<string, byte[]> pRawObjects;

        internal static void Writeback()
        {
            XDocument xd = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("store")
            );

            XElement baseElem = xd.Root;

            foreach (Namespace ns in pStore)
            {
                XElement nsElem = new XElement(ns.Name);
                foreach (string l in ns.Keys)
                {
                    nsElem.Add(new XElement(l, ns[l]));
                }
                baseElem.Add(nsElem);
            }

            foreach (var obj in pRawObjects)
            {
                XElement objElem = new XElement("object");
                objElem.SetAttributeValue("name", obj.Key);
                objElem.Add(new XCData(Convert.ToBase64String(obj.Value)));

                baseElem.Add(objElem);
            }

            pStorageFileStream.SetLength(0);
            xd.Save(pStorageFileStream);
            pStorageFileStream.Flush();
        }
        internal static void Readback()
        {
            if (pStorageFileStream.Length == 0)
            {
                // Nothing to read
                return;
            }

            // Seek ahead of the UTF8 BOM
            pStorageFileStream.Seek(3, SeekOrigin.Begin);
            byte[] xBytes = new byte[pStorageFileStream.Length - 3];
            pStorageFileStream.Read(xBytes, 0, xBytes.Length);
            string xml = Encoding.UTF8.GetString(xBytes);

            XmlDocument xmd = new XmlDocument();
            xmd.LoadXml(xml);

            XmlNode baseNode = xmd["store"];

            foreach (XmlNode ns in baseNode)
            {
                if (ns.Name.ToLower() == "object")
                {
                    string name = ns.Attributes["name"].InnerText;
                    byte[] content = Convert.FromBase64String(ns.InnerText);

                    pRawObjects.Add(name, content);

                    continue;
                }

                Dictionary<string, string> nsDict = new Dictionary<string, string>();

                foreach (XmlNode item in ns)
                    nsDict.Add(item.Name, item.InnerText);

                pStore.Add(new Namespace(ns.Name, nsDict));
            }
        }

        static Storage()
        {
            pStore = new List<Namespace>();
            pRawObjects = new Dictionary<string, byte[]>();

            try
            {
                Directory.CreateDirectory(SETTINGS_FOLDER);
                pStorageFileStream = File.Open(SETTINGS_FOLDER + SETTINGS_FILE, FileMode.OpenOrCreate);
            }
            catch (IOException ioex)
            {
                throw new StorageInaccessibleException("Could not gain access to storage.", ioex);
            }

            Storage.Readback();
        }

        /// <summary>
        /// Retrieves a pre-existing name-space from the store.
        /// </summary>
        /// <param name="ns">The name-space to retrieve.</param>
        /// <returns>The name-space if successful, null if otherwise.</returns>
        public static Namespace GetNamespace(string ns)
        {
            return pStore.FirstOrDefault(n => ns == n.Name);
        }
        /// <summary>
        /// Attempts to create a new name-space in the store.
        /// </summary>
        /// <param name="ns">The name of the name-space to create.</param>
        /// <returns>The created name-space if successful, null if the name-space already exists.</returns>
        public static Namespace CreateNamespace(string ns)
        {
            if (pStore.Where(n => ns == n.Name).Count() != 0) return null;
            else
            {
                pStore.Add(new Namespace(ns));
                Storage.Writeback();
                return pStore.FirstOrDefault(n => n.Name == ns);
            }
        }
        /// <summary>
        /// Creates a new name-space and populates it with the provided values.
        /// </summary>
        /// <param name="ns">The name of the name-space to create.</param>
        /// <param name="values">The values to populate the new name-space with.</param>
        /// <returns>The created name-space.</returns>
        public static Namespace CreateNamespace(string ns, Dictionary<string, string> values)
        {
            Namespace created = CreateNamespace(ns);
            foreach (string key in values.Keys) created[key] = values[key];

            return created;
        }
        /// <summary>
        /// Removes a name-space from the store.
        /// </summary>
        /// <param name="ns">The name of the name-space to remove.</param>
        public static void RemoveNamespace(string ns)
        {
            Namespace nSpace = pStore.FirstOrDefault(n => ns == n.Name);
            if (nSpace != null)
                if (!nSpace.Locked)
                    pStore.Remove(pStore.FirstOrDefault(n => ns == n.Name));
        }

        /// <summary>
        /// Stores the specified object in zSnap's XML storage.
        /// </summary>
        /// <param name="obj">The object to store.</param>
        public static void StoreObject(IStorableObject obj)
        {
            string name = String.Format("{0}##{1}", obj.GetType().FullName, obj.Name);
            byte[] rep = obj.GetRepresentation();

            if (pRawObjects.ContainsKey(name))
            {
                pRawObjects[name] = rep;
            }
            else
            {
                pRawObjects.Add(name, rep);
            }

            Writeback();
        }
        /// <summary>
        /// Retrieves an object from storage using its stored type and name.
        /// </summary>
        /// <typeparam name="T">The type of the stored object.</typeparam>
        /// <param name="name">The name of the stored object.</param>
        /// <returns>The stored object as <typeparamref name="T"/></returns>
        /// <exception cref="System.MissingMethodException">Thrown when there is no acceptable constructor.</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
        public static T GetObject<T>(string name) where T : class, IStorableObject
        {
            if (typeof(T).GetConstructor(new[] { typeof(string) }) == null)
                throw new MissingMethodException(
                    String.Format(
                        "Type {0} does not implement ctor(string).",
                        typeof(T).FullName
                    )
                );

            string fullName = String.Format("{0}##{1}", typeof(T).FullName, name);
            if (!pRawObjects.ContainsKey(fullName))
                throw new KeyNotFoundException("The specified object was not in storage.");

            var raw = new KeyValuePair<string, byte[]>(fullName, pRawObjects[fullName]);

            IStorableObject obj = Activator.CreateInstance(typeof(T), name) as IStorableObject;

            obj.Parse(raw.Value);

            return obj as T;
        }
        /// <summary>
        /// Determines whether an object of given type and name exists within storage.
        /// </summary>
        /// <typeparam name="T">The type of the stored object.</typeparam>
        /// <param name="name">The name of the stored object.</param>
        /// <returns>True if the object exists within storage.</returns>
        public static bool ObjectExists<T>(string name) where T : class, IStorableObject
        {
            return pRawObjects.ContainsKey(String.Format("{0}##{1}", typeof(T).FullName, name));
        }
    }

    /// <summary>
    /// The exception thrown when the storage manager cannot access the storage file.
    /// </summary>
    public class StorageInaccessibleException : Exception
    {
        public StorageInaccessibleException() : base() { }
        public StorageInaccessibleException(string message) : base(message) { }
        public StorageInaccessibleException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
