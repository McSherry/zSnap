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
using System.Drawing;
using System.Security;
using System.Security.Cryptography;
using System.Net;

using zSnap.API;
using zSnap.API.Notification;
using zSnap.API.Storage;
using System.IO;

namespace zSnap.Uploaders.PlainFTP
{
    /// <summary>
    /// An uploader for FTP destinations.
    /// </summary>
    public class PlainFtpUploader : ImageUploader, IConfigurableUploader
    {
        private static Namespace Settings;
        private static SHA512CryptoServiceProvider SHA512;
        internal static SecureString FtpPassword { get; set; }

        internal const string
            SETTING_FTP_HOST    = "FtpHost",
            SETTING_FTP_USER    = "FtpUser",
            SETTING_FTP_ENDPT   = "FtpEndpoint",
            SETTING_FTP_MODE    = "FtpUseActive",
            // SETTING_FTP_NLENV sets the default length. Current method of
            // name generation has a maximum length of 16. If a longer length
            // is required, a switch to something such as SHA-512 would be
            // required, as this would give a maximum length of 128.
            SETTING_FTP_NLEN    = "FtpNameLength"
            ;
        internal const int
            SETTING_FTP_NLENV   = 16,
            SETTING_FTP_NLENMAX = 128,
            SETTING_FTP_NLENMIN = 5
            ;

        private static bool TryFtpGetPassword()
        {
            PlainFtpPasswordWindow pfpw = new PlainFtpPasswordWindow();

            var diagRes = pfpw.ShowDialog();

            return diagRes ?? false;
        }
        private static bool TryFtpAuthenticate(out FtpStatusCode code)
        {
            FtpWebRequest ftpCheckReq = FtpWebRequest.Create(Settings[SETTING_FTP_HOST]) as FtpWebRequest;
            ftpCheckReq.Credentials = new NetworkCredential(Settings[SETTING_FTP_USER], FtpPassword);
            ftpCheckReq.Method = WebRequestMethods.Ftp.ListDirectory;

            FtpWebResponse ftpCheckResp = ftpCheckReq.GetResponseSafe() as FtpWebResponse;

            code = ftpCheckResp.StatusCode;

            ftpCheckResp.Close();
            ftpCheckResp.Dispose();
            return code != FtpStatusCode.NotLoggedIn;
        }

        private FtpWebRequest PrepareFtpRequest(string fname)
        {
            FtpWebRequest uplReq = FtpWebRequest.Create(
                String.Format(
                    "{0}{1}",
                    Settings[SETTING_FTP_HOST], fname
                )
            ) as FtpWebRequest;
            uplReq.Credentials = new NetworkCredential(Settings[SETTING_FTP_USER], FtpPassword);
            uplReq.Method = "STOR";
            uplReq.UseBinary = true;
            uplReq.Timeout = 60000; // 60s timeout
            uplReq.KeepAlive = false;

            bool useActive;
            if (!Settings.RetrieveSafe<bool>(SETTING_FTP_MODE, out useActive))
            {
                Settings[SETTING_FTP_MODE] = false.ToString();
                useActive = false;
            }
            uplReq.UsePassive = !useActive;

            return uplReq;
        }

        static PlainFtpUploader()
        {
            Settings = Storage.GetNamespace(Entry.NAMESPACE);
            SHA512 = new SHA512CryptoServiceProvider();
            FtpPassword = new SecureString();
        }
        public PlainFtpUploader() { }
        public PlainFtpUploader(Image image) : base(image) { }

        public override bool Upload(out Uri location)
        {
            DateTime now = DateTime.Now;
            string fname = String.Join(
                "",
                SHA512.ComputeHash(Encoding.ASCII.GetBytes(now.Ticks.ToString())).Select(b => b.ToString("x"))
            );

            ushort fnameLeng;
            if (!Settings.RetrieveSafe<ushort>(SETTING_FTP_NLEN, out fnameLeng))
            {
                fnameLeng = SETTING_FTP_NLENV;
            }

            fname = fname.Substring(0, fnameLeng) + ".png";

            Uri ftpUri = null;
            if (!Settings.Keys.Contains(SETTING_FTP_HOST) || !Settings.Keys.Contains(SETTING_FTP_USER))
            {
                Notifications.Raise(
                    "FTP must be configured before use.",
                    NotificationType.Error
                );

                location = null;
                return false;
            }
            else try
            {
                ftpUri = new Uri(Settings[SETTING_FTP_HOST]);
            }
            catch (UriFormatException)
            {
                Notifications.Raise(
                    "The URI of the FTP server is invalid.",
                    NotificationType.Error
                );

                location = null;
                return false;
            }

            FtpStatusCode code;
            if (!TryFtpAuthenticate(out code))
            {
                if (code == FtpStatusCode.NotLoggedIn)
                {
                    bool contLoop = true;

                    while (contLoop && code == FtpStatusCode.NotLoggedIn)
                    {
                        if (!TryFtpGetPassword())
                        {
                            Notifications.Raise(
                                "The upload was cancelled.",
                                NotificationType.Miscellaneous
                            );

                            location = null;
                            return false;
                        }
                        else
                        {
                            contLoop = !TryFtpAuthenticate(out code);

                            if (contLoop) Notifications.Raise(
                                "The password you entered was not accepted by the server.",
                                NotificationType.Error
                            );
                        }
                    }
                }
            }

            var request = PrepareFtpRequest(fname);

            FtpWebResponse response = null;
            try
            {
                using (Stream rSt = request.GetRequestStream())
                {
                    base.Image.Save(rSt, System.Drawing.Imaging.ImageFormat.Png);
                }

                response = request.GetResponseSafe() as FtpWebResponse;
            }
            catch (WebException wex)
            {
                response = wex.Response as FtpWebResponse;
            }

            if (
                response.StatusCode == FtpStatusCode.CommandOK ||
                response.StatusCode == FtpStatusCode.FileActionOK ||
                response.StatusCode == FtpStatusCode.ClosingData
                )
            {
                UriBuilder endpoint = null;

                try
                {
                    endpoint = new UriBuilder(Settings[SETTING_FTP_ENDPT]);

                    if (endpoint.Path.Last() == '/') endpoint.Path += fname;
                    else endpoint.Path += String.Format("/{0}", fname);
                }
                catch (UriFormatException)
                {
                    Notifications.Raise(
                        String.Format(
                            "The endpoint URI was invalid.\n\nFile uploaded as '{0}'.",
                            fname
                        ),
                        NotificationType.Error
                    );

                    location = null;
                    return false;
                }

                location = endpoint.Uri;
                return true;
            }
            else if (response.StatusCode == FtpStatusCode.ActionNotTakenFilenameNotAllowed)
            {
                Notifications.Raise(
                    "The FTP server refused the upload. Check user write permissions.",
                    NotificationType.Error
                );
            }
            else if (response.StatusCode == FtpStatusCode.ActionNotTakenInsufficientSpace)
            {
                Notifications.Raise(
                    "The FTP server reported insufficient space to complete the upload.",
                    NotificationType.Error
                );
            }
            else if (response.StatusCode == FtpStatusCode.Undefined)
            {
                Notifications.Raise(
                    "Unable to communicate with FTP server.",
                    NotificationType.Error
                );
            }
            else
            {
                Notifications.Raise(
                    String.Format(
                        "The FTP server returned the following response: {0}",
                        response.StatusDescription
                    ),
                    NotificationType.Error
                );
            }

            response.Close();
            response.Dispose();
            location = null;
            return false;
        }

        public override string Name
        {
            get { return "zSnap Plain FTP Uploader"; }
        }
        public override string ServiceName
        {
            get { return "Plain FTP"; }
        }

        System.Windows.Window IConfigurableUploader.Window
        {
            get { return new PlainFtpConfigWindow(); }
        }
    }
}
