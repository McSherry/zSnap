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
using System.Drawing.Imaging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Net;

using zSnap.API;
using zSnap.API.Storage;
using zSnap.API.Notification;

namespace zSnap.Uploaders.picBox
{
    /// <summary>
    /// A class for uploading images to the image-hosting service picBox.
    /// </summary>
    public class picBoxUploader : ImageUploader, IConfigurableUploader
    {
        private static Namespace Settings;

        /// <summary>
        /// A contract for a successful response from the picBox API.
        /// </summary>
        [DataContract]
        private class AnonSuccessResponse
        {
            /// <summary>
            /// The link to the uploaded image.
            /// </summary>
            [DataMember(Name = "url")]
            public string Link { get; set; }
        }
        /// <summary>
        /// A contract for a token exchange response from the authenticated
        /// picBox API.
        /// </summary>
        [DataContract]
        internal class TokenExSuccessResponse
        {
            /// <summary>
            /// Whether the request was successful (also
            /// indicated by HTTP status code).
            /// </summary>
            [DataMember(Name = "success")]
            public bool Success { get; set; }
            /// <summary>
            /// The authentication token.
            /// </summary>
            [DataMember(Name = "token")]
            public string Token { get; set; }
            /// <summary>
            /// The username of the authenticated user.
            /// </summary>
            [DataMember(Name = "username")]
            public string Username { get; set; }
        }
        /// <summary>
        /// A contract for a valid token response.
        /// </summary>
        [DataContract]
        internal class TokenStatusSuccessResponse
        {            
            /// <summary>
            /// Whether the request was successful (also
            /// indicated by HTTP status code).
            /// </summary>
            [DataMember(Name = "success")]
            public bool Success { get; set; }
            /// <summary>
            /// The current status of the provided token.
            /// </summary>
            [DataMember(Name = "status")]
            public string Status { get; set; }
            /// <summary>
            /// The username of the user the token is associated with.
            /// </summary>
            [DataMember(Name = "username")]
            public string Username { get; set; }
        }

        /// <summary>
        /// Class for interacting with the picBox authenticated API.
        /// </summary>
        internal static class PAuthenticator
        {
            /// <summary>
            /// The previous successful response.
            /// </summary>
            public static TokenExSuccessResponse PreviousExchange { get; private set; }
            /// <summary>
            /// The previous successful status response.
            /// </summary>
            public static TokenStatusSuccessResponse PreviousStatus { get; private set; }
            /// <summary>
            /// Used to swap a PIN for an authentication token.
            /// </summary>
            /// <param name="pin">The PIN to swap.</param>
            /// <returns>True upon success.</returns>
            public static HttpStatusCode Authenticate(string pin)
            {
                using (HttpClient hcl = new HttpClient())
                {
                    var task = hcl.GetAsync(
                        String.Format(
                            "{0}?client_id={1}&client_secret={2}&pin={3}",
                            API_PA_TOKEN, Keys.PICBOX_CLIENT_ID, Keys.PICBOX_CLIENT_SECRET,
                            pin
                        )
                    );
                    task.Wait();

                    using (HttpResponseMessage hrm = task.Result)
                    {
                        if (hrm.StatusCode == HttpStatusCode.OK)
                        {
                            var ser = new DataContractJsonSerializer(typeof(TokenExSuccessResponse));

                            using (MemoryStream ms = new MemoryStream())
                            {
                                var getRes = hrm.Content.CopyToAsync(ms);
                                getRes.Wait();

                                ms.Seek(0, SeekOrigin.Begin);

                                PreviousExchange = ser.ReadObject(ms) as TokenExSuccessResponse;
                            }

                            Settings[SETTING_TOKEN] = PreviousExchange.Token;
                            return hrm.StatusCode;
                        }
                        else return hrm.StatusCode;
                    }
                }
            }
            /// <summary>
            /// Checks to see whether authentication is currently possible.
            /// </summary>
            /// <returns>True if possible, false if otherwise.</returns>
            public static bool Verify()
            {
                string token;
                if (Settings.RetrieveSafe<string>(SETTING_TOKEN, out token))
                {
                    using (HttpClient hcl = new HttpClient())
                    {
                        var task = hcl.GetAsync(
                            String.Format("{0}?token={1}", API_PA_STATUS, token)
                        );
                        task.Wait();

                        using (HttpResponseMessage hrm = task.Result)
                        {
                            var ser = new DataContractJsonSerializer(typeof(TokenStatusSuccessResponse));

                            
                            var getRes = hrm.Content.ReadAsStreamAsync();
                            getRes.Wait();
                            Stream content = getRes.Result;
                            PreviousStatus = ser.ReadObject(content) as TokenStatusSuccessResponse;
                            content.Close();
                            content.Dispose();

                            return hrm.StatusCode == System.Net.HttpStatusCode.OK;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        internal const string
            API_BASE        = "https://picbox.us/api/push",
            API_EP_IMAGE    = "?type=image",
            API_MEDIA_TYPE  = "multipart/form-data",
            API_NAME        = "zsnap_image",

            API_PA_TOKEN    = "https://picbox.us/api/pauth_req_token",
            API_PA_STATUS   = "https://picbox.us/api/pauth_token_status",
            API_PA_PIN      = "https://picbox.us/pauth",

            SETTING_TOKEN   = "pbToken",
            SETTING_AUTH    = "pbUseAuth"
            ;

        public picBoxUploader() { }
        public picBoxUploader(Image image) : base(image) { }
        static picBoxUploader()
        {
            Settings = Storage.GetNamespace(Entry.NAMESPACE);

            bool useAuth;
            if (!Settings.RetrieveSafe<bool>(SETTING_AUTH, out useAuth))
                Settings[SETTING_AUTH] = false.ToString();
        }

        public override string Name
        {
            get { return "zSnap picBox Uploader"; }
        }

        public override string ServiceName
        {
            get { return "picBox"; }
        }

        public override bool Upload(out Uri location)
        {
            HttpResponseMessage hrm = null;
            using (HttpClient hcl = new HttpClient())
            {
                byte[] imageBytes = new ImageConverter().ConvertTo(base.Image, typeof(byte[])) as byte[];

                bool useAuth;
                if (Settings.RetrieveSafe<bool>(SETTING_AUTH, out useAuth))
                {
                    useAuth = PAuthenticator.Verify();
                }
                else
                {
                    Settings[SETTING_AUTH] = false.ToString();
                    useAuth = false;
                }

                using (var icon = new ByteArrayContent(imageBytes))
                {
                    string media_type = base.Image.GetMediaType();
                    icon.Headers.ContentType = MediaTypeHeaderValue.Parse(media_type);

                    using (var rcon = new MultipartFormDataContent())
                    {
                        rcon.Add(icon, "file", "filename");

                        string reqStr =
                            useAuth
                            ? String.Format(
                                "{0}{1}&token={2}",
                                API_BASE, API_EP_IMAGE,
                                Settings[SETTING_TOKEN]
                                )
                            : API_BASE + API_EP_IMAGE
                            ;

                        var task = hcl.PostAsync(reqStr, rcon);
                        task.Wait();
                        hrm = task.Result;
                    }
                }
            }

            if (hrm.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var ser = new DataContractJsonSerializer(typeof(AnonSuccessResponse));

                using (MemoryStream ms = new MemoryStream())
                {
                    var respTask = hrm.Content.ReadAsStringAsync();
                    respTask.Wait();

                    byte[] respBytes = Encoding.UTF8.GetBytes(respTask.Result);
                    ms.Write(respBytes, 0, respBytes.Length);
                    ms.Seek(0, SeekOrigin.Begin);

                    var response = ser.ReadObject(ms) as AnonSuccessResponse;

                    hrm.Dispose();
                    location = new Uri(response.Link);
                    return true;
                }
            }
            else if (hrm.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                Notifications.Raise(
                    "The image could not be uploaded as it was invalid or corrupt.",
                    NotificationType.Error
                );

                hrm.Dispose();
                location = null;
                return false;
            }
            else if (hrm.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                Notifications.Raise(
                    "The image could be not uploaded due to an issue with the picBox servers.",
                    NotificationType.Error
                );

                hrm.Dispose();
                location = null;
                return false;
            }
            else
            {
                Notifications.Raise(
                    "The image could not be uploaded as it was invalid or corrupt.",
                    NotificationType.Error
                );

                hrm.Dispose();
                location = null;
                return false;
            }
        }

        System.Windows.Window IConfigurableUploader.Window
        {
            get { return new picBoxConfWindow(); }
        }
    }
}