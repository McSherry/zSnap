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
using System.Net;
using System.Security.Cryptography;
using System.Xml;

namespace zSnap
{
    /// <summary>
    /// A class containing metadata about zSnap.
    /// </summary>
    public static class Metadata
    {
        private const string UPDATE_CHECK_URI = "http://static.zsnap.org/v";

        /// <summary>
        /// The current major version, where X in X.Y.Z-Q is the major version.
        /// </summary>
        public static int MajorVersion
        {
            get { return 1; }
        }
        /// <summary>
        /// The current minor version, where Y in X.Y.Z-Q is the minor version.
        /// </summary>
        public static int MinorVersion
        {
            get { return 2; }
        }
        /// <summary>
        /// The current patch/revision version, where Z in X.Y.Z-Q is the patch/revision version.
        /// </summary>
        public static int PatchVersion
        {
            get { return 0; }
        }
        /// <summary>
        /// The qualifiers appended to the end of the version string, where Q in X.Y.Z-Q is the qualifier.
        /// </summary>
        public static string[] Qualifiers
        {
            get { return new string[0]; }
        }
        /// <summary>
        /// Any additional information about the current build.
        /// </summary>
        public static string BuildInformation
        {
            get { return String.Empty; }
        }
        /// <summary>
        /// The full version string, including all versions and qualifiers.
        /// </summary>
        public static string FullVersionString
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendFormat(
                    "{0}.{1}.{2}", MajorVersion, MinorVersion, PatchVersion.ToString()
                );

                if (Qualifiers.Length > 0)
                {
                    sb.Append("-");
                    foreach (string qualifier in Qualifiers)
                        sb.AppendFormat("{0}.", qualifier);
                    sb.Remove(sb.Length - 1, 1);
                }

                if (BuildInformation != String.Empty)
                    sb.AppendFormat("+{0}", BuildInformation);

                return sb.ToString();
            }
        }

        /// <summary>
        /// Checks whether an update is available for this version.
        /// </summary>
        /// <param name="URIs">A reference parameter used to output the URIs of resources. Archive URI first, installer second, release notes last.</param>
        /// <returns>True if an update is available, false if otherwise or if an error occurs.</returns>
        public static bool CheckNewVersion(out Tuple<Uri, Uri, Uri> URIs)
        {
            URIs = null;

            if (!Interop.InternetCheckConnection()) return false;
            else
            {
                byte[] vsBytes = Encoding.ASCII.GetBytes(FullVersionString);
                byte[] hashBytes;

                using (var crypto = new SHA1CryptoServiceProvider())
                {
                    hashBytes = crypto.ComputeHash(vsBytes);
                }

                string hexits = String.Join(String.Empty, hashBytes.Select(b => b.ToString("x2")));

                try
                {
                    HttpWebRequest webreq = WebRequest.CreateHttp(
                        UPDATE_CHECK_URI + "?hash=" + hexits
                    );
                    HttpWebResponse webres = webreq.GetResponse() as HttpWebResponse;

                    string xml;
                    using (var sr = new System.IO.StreamReader(webres.GetResponseStream()))
                    {
                        xml = sr.ReadToEnd();
                    }

                    XmlDocument xmd = new XmlDocument();
                    xmd.LoadXml(xml);

                    bool success = bool.Parse(xmd["data"].Attributes["success"].Value);

                    if (!success) return false;
                    else
                    {
                        bool upAv = bool.Parse(xmd["data"]["update"].Attributes["available"].Value);

                        if (upAv)
                        {
                            Tuple<Uri, Uri, Uri> uris = new Tuple<Uri, Uri, Uri>
                            (
                                new Uri(xmd["data"]["update"].Attributes["archive"].Value),
                                new Uri(xmd["data"]["update"].Attributes["install"].Value),
                                new Uri(xmd["data"]["update"].Attributes["notes"].Value)
                            );

                            URIs = uris;
                        }

                        return upAv;
                    }
                }
                catch (WebException)
                {
                    return false;
                }
            }
        }
        /// <summary>
        /// Checks whether an update is available for this version.
        /// </summary>
        /// <returns>True if an update is available, false if otherwise or if an error occurs.</returns>
        public static bool CheckNewVersion()
        {
            Tuple<Uri, Uri, Uri> val;
            return CheckNewVersion(out val);
        }
    }
}
