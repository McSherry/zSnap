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
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.XPath;

namespace zSnap
{
    /// <summary>
    /// <para>
    /// Represents the information returned by the update check.
    /// </para>
    /// </summary>
    public struct UpdateInfo
    {
        /// <summary>
        /// <para>
        /// Maps a <see cref="Tuple{T1,T2,T3}"/> to an 
        /// <see cref="UpdateInfo"/>.
        /// </para>
        /// </summary>
        /// <param name="tpl">
        /// The tuple of <see cref="Uri"/>s to map, with the first
        /// item being the archive location, the second being the
        /// installer location, and the third being the notes
        /// location.
        /// </param>
        /// <returns>
        /// <para>
        /// An <see cref="UpdateInfo"/> equivalent to <paramref name="tpl"/>.
        /// </para>
        /// </returns>        
        /// <exception cref="ArgumentNullException">
        /// <para>
        /// Thrown when any parameters are null.
        /// </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para>
        /// Thrown when any parameters do not use the HTTP or HTTPS
        /// URI schemes.
        /// </para>
        /// </exception>
        public static UpdateInfo Map(Tuple<Uri, Uri, Uri> tpl)
        {
            return new UpdateInfo(
                archive:    tpl.Item1,
                installer:  tpl.Item2,
                notes:      tpl.Item3
                );
        }
        /// <summary>
        /// <para>
        /// Maps an <see cref="UpdateInfo"/> to a 
        /// <see cref="Tuple{T1, T2, T3}"/>.
        /// </para>
        /// </summary>
        /// <param name="info">
        /// The <see cref="UpdateInfo"/> to map to a tuple.
        /// </param>
        /// <returns>
        /// A three-tuple of <see cref="Uri"/>s, with the first item
        /// being the archive location, the second the installer location,
        /// and the third the release notes location.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the <see cref="IsAvailable"/> property of
        /// <paramref name="info"/> is false.
        /// </exception>
        public static Tuple<Uri, Uri, Uri> Unmap(UpdateInfo info)
        {
            if (!info.IsAvailable)
            {
                throw new ArgumentException(
                    paramName:  nameof(info),
                    message:    "The provided UpdateInfo did not contain " +
                                "information to map to a Tuple<Uri, Uri, Uri>."
                    );
            }

            return Tuple.Create(
                item1: info.ArchiveLocation,
                item2: info.InstallerLocation,
                item3: info.ReleaseNotesLocation
                );
        }

        /// <summary>
        /// <para>
        /// Creates a new <see cref="UpdateInfo"/> with the provided
        /// information on an update.
        /// </para>
        /// </summary>
        /// <param name="archive">
        /// A URI indicating where to download an archive of the update.
        /// </param>
        /// <param name="installer">
        /// A URI indicating where to download an installer for the update.
        /// </param>
        /// <param name="notes">
        /// A URI indicating where to find release notes for the update.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <para>
        /// Thrown when any parameters are null.
        /// </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para>
        /// Thrown when any parameters do not use the HTTP or HTTPS
        /// URI schemes.
        /// </para>
        /// </exception>
        public UpdateInfo(Uri archive, Uri installer, Uri notes)
        {
            // If there is update info, the caller should actually provide
            // some update info.
            if (archive == null || installer == null || notes == null)
            {
                throw new ArgumentNullException(
                    message:        "All provided URIs must be non-null", 
                    innerException: null
                    );
            }

            // Make sure all URI schemes are http or https. If they're not,
            // we're going to consider that wrong.
            if (archive.Scheme != installer.Scheme &&
                installer.Scheme != notes.Scheme &&
                !(notes.Scheme == "http" || notes.Scheme == "https"))
            {
                throw new ArgumentException(
                    "All provided URIs must use the HTTP or HTTPS scheme."
                    );
            }

            IsAvailable = true;
            ArchiveLocation = archive;
            InstallerLocation = installer;
            ReleaseNotesLocation = notes;
        }

        /// <summary>
        /// <para>
        /// Whether an update is available for this version.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// If false, the properties <see cref="ArchiveLocation"/>,
        /// <see cref="InstallerLocation"/>, and 
        /// <see cref="ReleaseNotesLocation"/> will be null.
        /// </para>
        /// </remarks>
        public bool IsAvailable
        {
            get;
        }

        /// <summary>
        /// <para>
        /// The location from which an archive containing the update 
        /// can be downloaded.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <see cref="IsAvailable"/> is false, this is null.
        /// </para>
        /// </remarks>
        public Uri ArchiveLocation
        {
            get;
        }
        /// <summary>
        /// <para>
        /// The location from which an installer for the update can
        /// be downloaded.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <see cref="IsAvailable"/> is false, this is null.
        /// </para>
        /// </remarks>
        public Uri InstallerLocation
        {
            get;
        }
        /// <summary>
        /// <para>
        /// The location at which release notes for the update are
        /// available.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <see cref="IsAvailable"/> is false, this is null.
        /// </para>
        /// </remarks>
        public Uri ReleaseNotesLocation
        {
            get;
        }
    }

    /// <summary>
    /// <para>
    /// A class providing metadata about zSnap.
    /// </para>
    /// </summary>
    public static class Metadata
    {
        private const string UPDATE_CHECK_URI = "http://static.zsnap.org/v";

        private static Lazy<string> _verString = new Lazy<string>(delegate
        {
            // The very basics of a Semantic Version number.
            var sb = new StringBuilder(
                $"{MajorVersion}.{MinorVersion}.{PatchVersion}"
                );

            if (Qualifiers.Any())
            {
                // Qualifiers are separated from the version string by a 
                // hyphen.
                sb.Append('-');

                // Qualifiers are separated from each other by periods.
                foreach (var q in Qualifiers)
                    sb.AppendFormat("{0}.", q);

                // Remove the additional period that the above loop will add
                // at the end.
                sb.Remove(sb.Length - 1, 1);
            }

            // If we've got build information, append it. Build information is
            // separated from qualifiers/the version number by a plus sign.
            if (!String.IsNullOrWhiteSpace(BuildInformation))
                sb.AppendFormat("+{0}", BuildInformation);

            return sb.ToString();
        });
        private static Lazy<string> _verHash = new Lazy<string>(delegate
        {
            var verStringBytes = Encoding.ASCII.GetBytes(FullVersionString);
            var sha1 = SHA1.Create();

            return sha1.ComputeHash(verStringBytes)
                       .Aggregate(new StringBuilder(),
                                  (sb, b) => sb.AppendFormat("{0:x2}", b))
                       .ToString();

        });

        // Set major/minor/patch to 1.99.0 to test update checking.

        /// <summary>
        /// <para>
        /// The current major version.
        /// </para>
        /// </summary>
        public static int MajorVersion => 1;
        /// <summary>
        /// <para>
        /// The current minor version.
        /// </para>
        /// </summary>
        public static int MinorVersion => 2;
        /// <summary>
        /// <para>
        /// The current patch version.
        /// </para>
        /// </summary>
        public static int PatchVersion => 0;

        /// <summary>
        /// <para>
        /// The qualifiers appended to the version string to indicate a
        /// pre-release version.
        /// </para>
        /// </summary>
        public static string[] Qualifiers => new string[0];
        /// <summary>
        /// <para>
        /// The build information appended to the version string.
        /// </para>
        /// </summary>
        public static string BuildInformation => String.Empty;

        /// <summary>
        /// <para>
        /// The current full version string, including all numbers, qualifiers,
        /// and build information.
        /// </para>
        /// </summary>
        public static string FullVersionString => _verString.Value;
        /// <summary>
        /// <para>
        /// A unique hash representing the current version.
        /// </para>
        /// </summary>
        public static string VersionHash => _verHash.Value;

        /// <summary>
        /// <para>
        /// Checks whether an update is available for this version, and
        /// provides access information if one is available.
        /// </para>
        /// </summary>
        /// <param name="info">
        /// <para>
        /// The variable to which information about any available update
        /// is to be assigned.
        /// </para>
        /// </param>
        public static void CheckNewVersion(out UpdateInfo info)
        {
            // If there's no internet connection, we obviously can't check 
            // for an update using the internet.
            if (!Interop.InternetCheckConnection())
            {
                info = new UpdateInfo();
                return;
            }

            var uri = String.Join(
                String.Empty, UPDATE_CHECK_URI, "?hash=", VersionHash);
            var req = WebRequest.CreateHttp(uri);

            // Get the response, catching any exceptions if the request fails.
            var res = req.GetResponseSafe() as HttpWebResponse;

            // If the request fails, indicate that no update is available.
            if (res.StatusCode != HttpStatusCode.OK)
            {
                info = new UpdateInfo();
                return;
            }

            // The response we get should be some XML.
            var xpd = new XPathDocument(res.GetResponseStream());
            var update = xpd.CreateNavigator().SelectSingleNode("/data/update");

            // Is there an update available? If not, we return.
            bool isAvail;
            if (!bool.TryParse(update.GetAttribute("available", ""), out isAvail) ||
                !isAvail)
            {
                info = new UpdateInfo();
                return;
            }

            // Try and parse the URIs 
            Uri archive, installer, notes;
            if (!Uri.TryCreate(update.GetAttribute("archive", ""),
                               UriKind.Absolute,
                               out archive) ||
                !Uri.TryCreate(update.GetAttribute("install", ""),
                               UriKind.Absolute,
                               out installer) ||
                !Uri.TryCreate(update.GetAttribute("notes", ""),
                               UriKind.Absolute,
                               out notes))
            {
                info = new UpdateInfo();
                return;
            }

            // Return the retrieved URIs
            info = new UpdateInfo(archive, installer, notes);
        }

        /// <summary>
        /// <para>
        /// Checks whether an update is available for this version.
        /// </para>
        /// </summary>
        /// <param name="URIs">
        /// <para>
        /// A reference parameter used to output the URIs of update resources.
        /// </para>
        /// </param>
        /// <returns>
        /// <para>
        /// True if an update is available, false if not or if an error occurs.
        /// </para>
        /// </returns>
        [Obsolete(message: "Use Metadata.CheckNewVersion(out UpdateInfo) instead.",
                  error:   true)]
        public static bool CheckNewVersion(out Tuple<Uri, Uri, Uri> URIs)
        {
            UpdateInfo update;
            CheckNewVersion(out update);

            if (update.IsAvailable)
            {
                URIs = UpdateInfo.Unmap(update);
            }
            else
            {
                URIs = null;
            }

            return update.IsAvailable;
        }
        /// <summary>
        /// <para>
        /// Checks whether an update is available for this version.
        /// </para>
        /// </summary>
        /// <returns>
        /// <para>
        /// True if an update is available, false if not or if an error occurs.
        /// </para>
        /// </returns>
        [Obsolete(message: "Use Metadata.CheckNewVersion(out UpdateInfo) instead.",
                  error:   true)]
        public static bool CheckNewVersion()
        {
            Tuple<Uri, Uri, Uri> val;
            return CheckNewVersion(out val);
        }
    }
}
