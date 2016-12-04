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
using System.Net;
using System.Net.NetworkInformation;
using System.Drawing;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

using zSnap.API;
using zSnap.API.Notification;
using zSnap.API.Storage;

namespace zSnap.Uploaders.Imgur
{
    /// <summary>
    /// Used to upload images to the hosting service Imgur.
    /// </summary>
    public class ImgurUploader : ImageUploader, IConfigurableUploader
    {
        /// <summary>
        /// A class used to contain a JSON response from Imgur's OAuth2 Token endpoint.
        /// </summary>
        [DataContract]
        internal class OAuthTokenResponse
        {
            /// <summary>
            /// The temporary access token used to make authenticated API calls.
            /// </summary>
            [DataMember(Name = "access_token")]
            public string AccessToken { get; set; }
            /// <summary>
            /// The number of seconds until the access token expires.
            /// </summary>
            [DataMember(Name = "expires_in")]
            public int ExpiresIn { get; set; }
            /// <summary>
            /// The type of token present.
            /// </summary>
            [DataMember(Name = "token_type")]
            public string TokenType { get; set; }
            /// <summary>
            /// The access scope.
            /// </summary>
            [DataMember(Name = "scope")]
            public string Scope { get; set; }
            /// <summary>
            /// The token used to refresh the access token.
            /// </summary>
            [DataMember(Name = "refresh_token")]
            public string RefreshToken { get; set; }
            /// <summary>
            /// The username of the authenticated account.
            /// </summary>
            [DataMember(Name = "account_username")]
            public string AccountName { get; set; }
        }
        /// <summary>
        /// A class encapsulating a response from the image upload endpoint of the Imgur API.
        /// </summary>
        private class UploadResponse
        {
            private readonly HttpWebRequest pRequest;
            private readonly HttpWebResponse pResponse;
            private string XmlString;
            private XPathNavigator Xml;

            private readonly HttpStatusCode pStatus;
            private readonly bool pSuccess;
            private readonly int pApiStatus;
            private readonly DateTime pDateTime;
            private readonly string pId;
            private readonly string pDeleteHash;
            private readonly Uri pLink;

            private bool RetrieveSuccess()
            {
                string statusVal = this.Xml.GetAttribute("success", String.Empty);

                return statusVal == "1";
            }
            private int RetrieveApiStatus()
            {
                string apiStatString = this.Xml.GetAttribute("status", String.Empty);

                int apiStat;
                if (!int.TryParse(apiStatString, out apiStat)) apiStat = 500;

                return apiStat;
            }
            private DateTime RetrieveDateTime()
            {
                var dtNode = this.Xml.SelectSingleNode("/data/datetime");
                string dtString = dtNode.InnerXml;

                int unixTimestamp = int.Parse(dtString);

                return unixTimestamp.ToDateTime();
            }
            private string RetrieveIdentifier()
            {
                var idNode = this.Xml.SelectSingleNode("/data/id");

                return idNode.InnerXml;
            }
            private string RetrieveDelHash()
            {
                var delNode = this.Xml.SelectSingleNode("/data/deletehash");

                return delNode.InnerXml;
            }
            private Uri RetrieveLink()
            {
                var uriNode = this.Xml.SelectSingleNode("/data/link");

                return new Uri(uriNode.InnerXml);
            }

            public UploadResponse(HttpWebRequest request)
            {
                this.pRequest = request;

                try
                {
                    this.pResponse = this.pRequest.GetResponse() as HttpWebResponse;
                }
                catch (WebException wex)
                {
                    if (wex.Response != null) this.pResponse = wex.Response as HttpWebResponse;
                    else
                    {
                        this.pSuccess = false;
                        return;
                    }
                }

                using (StreamReader sr = new StreamReader(this.pResponse.GetResponseStream()))
                {
                    XmlString = sr.ReadToEnd();
                }

                this.pStatus = this.pResponse.StatusCode;

                XmlDocument xmd = new XmlDocument();
                xmd.LoadXml(this.XmlString);

                this.Xml = xmd.SelectSingleNode("/data").CreateNavigator();

                this.pSuccess = RetrieveSuccess();
                this.pApiStatus = RetrieveApiStatus();

                if (this.Success)
                {
                    this.pDateTime = RetrieveDateTime();
                    this.pId = RetrieveIdentifier();
                    this.pDeleteHash = RetrieveDelHash();
                    this.pLink = RetrieveLink();
                }
            }

            /// <summary>
            /// The HTTP status code the server responded with.
            /// </summary>
            public HttpStatusCode Status
            {
                get { return this.pStatus; }
            }
            /// <summary>
            /// Whether the Imgur API reported that the request was successful.
            /// </summary>
            public bool Success
            {
                get { return this.pSuccess; }
            }
            /// <summary>
            /// The status code reported in the Imgur API's response.
            /// </summary>
            public int ApiStatus
            {
                get { return pApiStatus; }
            }
            /// <summary>
            /// The time at which the Imgur API responded.
            /// </summary>
            public DateTime DateTime
            {
                get { return this.pDateTime; }
            }
            /// <summary>
            /// The identifier assigned to the uploaded image.
            /// </summary>
            public string Identifier
            {
                get { return this.pId; }
            }
            /// <summary>
            /// The hash to be used for requesting image deletion.
            /// </summary>
            public string DeleteHash
            {
                get { return this.pDeleteHash; }
            }
            /// <summary>
            /// The link to the uploaded image.
            /// </summary>
            public Uri Link
            {
                get { return this.pLink; }
            }

            public static implicit operator UploadResponse(HttpWebRequest hwr)
            {
                return new UploadResponse(hwr);
            }
        }

        internal enum AuthStatus
        {
            Invalid,
            Active
        }
        /// <summary>
        /// Handles authentication with the Imgur API via OAuth.
        /// </summary>
        internal static class OAuthenticator
        {
            /// <summary>
            /// The last OAuthTokenResponse received.
            /// </summary>
            public static OAuthTokenResponse Previous { get; set; }

            /// <summary>
            /// The current authentication token to be used.
            /// </summary>
            public static string AuthToken { get; set; }
            /// <summary>
            /// The time at which the current authentication token expires.
            /// </summary>
            public static DateTime AuthExpiry { get; set; }

            static OAuthenticator()
            {
                // Make sure that HasExpired returns true
                // upon start-up, since we're only storing
                // the refresh token (meaning we need to
                // generate a new authentication token on
                // start-up).
                AuthExpiry = 0.ToDateTime();
            }

            /// <summary>
            /// Whether authentication is enabled.
            /// </summary>
            public static bool Enabled
            {
                get
                {
                    bool didParse;
                    if (!bool.TryParse(Settings[SETTING_ENAUTH], out didParse))
                    {
                        Settings[SETTING_ENAUTH] = DefaultSettings[SETTING_ENAUTH];
                    }
                    
                    return bool.Parse(Settings[SETTING_ENAUTH]);
                }
            }
            /// <summary>
            /// Whether the authentication token expiry time has passed.
            /// </summary>
            public static bool HasExpired
            {
                get { return DateTime.UtcNow > AuthExpiry; }
            }
            /// <summary>
            /// Whether a value for the refresh token is present.
            /// </summary>
            public static bool CanAttemptRefresh
            {
                get { return Settings.Keys.Contains(SETTING_AUTHREFRESH); }
            }

            /// <summary>
            /// Converts a pin into tokens.
            /// </summary>
            /// <param name="pin"></param>
            public static HttpStatusCode Tokenise(string pin, out OAuthTokenResponse response)
            {
                HttpWebRequest tokenRequest = WebRequest.CreateHttp(API_OA_BASE + API_EP_OA_REFRESH);
                tokenRequest.ContentType = API_IMGMEDIA;
                tokenRequest.Method = "POST";

                using (StreamWriter sw = new StreamWriter(tokenRequest.GetRequestStream()))
                {
                    sw.Write(
                        String.Format(
                            "client_id={0}&client_secret={1}&grant_type={2}&pin={3}",
                            ApiKeys.IMGURv3_CLIENT_ID, ApiKeys.IMGURv3_CLIENT_SECRET, API_OA_GRANTTYPE, pin
                        )
                    );
                }

                HttpStatusCode responseCode = default(int);
                HttpWebResponse tokenResponse = null;

                try
                {
                    tokenResponse = tokenRequest.GetResponse() as HttpWebResponse;
                    responseCode = tokenResponse.StatusCode;
                }
                catch (WebException wex)
                {
                    tokenResponse = wex.Response as HttpWebResponse;
                    responseCode = tokenResponse.StatusCode;
                }

                if (responseCode == HttpStatusCode.OK)
                {
                    var ser = new DataContractJsonSerializer(typeof(OAuthTokenResponse));

                    response = ser.ReadObject(tokenResponse.GetResponseStream()) as OAuthTokenResponse;
                }
                else
                {
                    response = null;
                }

                return responseCode;
            }

            /// <summary>
            /// Refreshes the OAuth2 access token using the refresh token.
            /// </summary>
            /// <returns></returns>
            public static bool Refresh()
            {
                HttpWebRequest refreshRequest = WebRequest.CreateHttp(API_OA_BASE + API_EP_OA_REFRESH);
                refreshRequest.Method = "POST";
                refreshRequest.ContentType = API_IMGMEDIA;

                using (StreamWriter sw = new StreamWriter(refreshRequest.GetRequestStream()))
                {
                    sw.Write(
                        String.Format(
                            "client_id={0}&client_secret={1}&grant_type={2}&refresh_token={3}",
                            ApiKeys.IMGURv3_CLIENT_ID, ApiKeys.IMGURv3_CLIENT_SECRET, "refresh_token",
                            Settings[SETTING_AUTHREFRESH]
                        )
                    );
                }

                HttpWebResponse refreshResp = null;
                try
                {
                    refreshResp = refreshRequest.GetResponse() as HttpWebResponse;
                }
                catch (WebException wex)
                {
                    refreshResp = wex.Response as HttpWebResponse;
                }
                catch (Exception)
                {
                    return false;
                }

                if (refreshResp.StatusCode == HttpStatusCode.OK)
                {
                    var ser = new DataContractJsonSerializer(typeof(OAuthTokenResponse));
                    var response = ser.ReadObject(refreshResp.GetResponseStream()) as OAuthTokenResponse;

                    AuthToken = response.AccessToken;
                    AuthExpiry = DateTime.Now.AddSeconds(response.ExpiresIn);

                    Previous = response;

                    return true;
                }
                else return false;
            }
            /// <summary>
            /// Verifies the current authentication status.
            /// </summary>
            /// <returns>Active if authenticated, Invalid if otherwise or if unable to refresh.</returns>
            public static AuthStatus Verify()
            {
                VerifySettingsPresent();

                if (HasExpired)
                {
                    if (CanAttemptRefresh)
                    {
                        if (Refresh()) return AuthStatus.Active;
                        else return AuthStatus.Invalid;
                    }
                    else return AuthStatus.Invalid;
                }
                else return AuthStatus.Active;
            }
        }

        internal static void VerifySettingsPresent()
        {
            Namespace Settings = Storage.GetNamespace(Entry.NAMESPACE);
            foreach (string key in DefaultSettings.Keys)
            {
                if (!Settings.Keys.Contains(key)) Settings[key] = DefaultSettings[key];
            }
        }
        internal static readonly Dictionary<string, string> DefaultSettings
            = new Dictionary<string, string>()
            {
                { SETTING_ENAUTH, false.ToString() }
            };
        internal const string
            SETTING_AUTHREFRESH = "ImgurRefreshToken",
            SETTING_ENAUTH      = "ImgurEnableAuthentication",
            API_IMGMEDIA        = "application/x-www-form-urlencoded",
            API_FORCE200        = "_fake_status=200",
            API_BASE            = "https://api.imgur.com/3",
            API_OA_BASE         = "https://api.imgur.com/oauth2",
            API_EP_UPLOAD       = "/upload.xml",
            API_EP_OA_REFRESH   = "/token",
            API_OA_GRANTTYPE    = "pin";
        // The maximum number of characters that will be escaped at once
        private const int MAX_URI_LENGTH = 32767;
        
        private static Namespace Settings;
        private HttpWebRequest Request;

        static ImgurUploader()
        {
            Settings = Storage.GetNamespace(Entry.NAMESPACE);
        }

        public ImgurUploader(Image image) : base(image)
        {
            VerifySettingsPresent();
        }
        public ImgurUploader()
        {

        }

        public override string Name
        {
            get { return "zSnap Imgur Uploader"; }
        }
        public override string ServiceName
        {
            get { return "Imgur"; }
        }

        public override bool Upload(out Uri location)
        {            
            this.Request = WebRequest.CreateHttp(String.Format("{0}{1}", API_BASE, API_EP_UPLOAD));
            this.Request.Method = "POST";
            this.Request.ContentType = API_IMGMEDIA;

            if (OAuthenticator.Enabled)
            {
                OAuthenticator.Verify();

                this.Request.Headers["Authorization"] = String.Format("Bearer {0}", OAuthenticator.AuthToken);
            }
            else
            {
                this.Request.Headers["Authorization"] = String.Format("Client-ID {0}", ApiKeys.IMGURv3_CLIENT_ID);
            }
            

            #region Perform actual upload
            byte[] imageBytes = new ImageConverter().ConvertTo(base.Image, typeof(byte[])) as byte[];
            string imageBase64 = Convert.ToBase64String(imageBytes);

            // We need to process the URI in blocks of MAX_URI_LENGTH,
            // or we'll receive an exception preventing us from uploading
            // larger files.
            StringBuilder escapedUri = new StringBuilder();
            if (imageBase64.Length > MAX_URI_LENGTH)
            {
                bool continueLoop = true;
                int offset = 0;
                while (continueLoop)
                {
                    // This condition will be met by every block
                    // until the very last block.
                    if (offset + MAX_URI_LENGTH < imageBase64.Length)
                    {
                        escapedUri.Append(Uri.EscapeDataString(imageBase64.Substring(offset, MAX_URI_LENGTH)));
                        offset += MAX_URI_LENGTH;
                    }
                    else
                    {
                        int length = imageBase64.Length % MAX_URI_LENGTH;
                        escapedUri.Append(Uri.EscapeDataString(imageBase64.Substring(offset, length)));
                        continueLoop = false;
                    }
                }
            }
            else escapedUri.Append(Uri.EscapeDataString(imageBase64));

            using (StreamWriter sw = new StreamWriter(this.Request.GetRequestStream()))
            {
                sw.Write(
                    String.Format(
                        "image={0}",
                        escapedUri.ToString()
                    )
                );
            }

            UploadResponse resp = this.Request;

            if (resp.ApiStatus != default(int))
            {
                if (resp.ApiStatus == 200)
                {
                    location = resp.Link;
                    return true;
                }
                else if (resp.ApiStatus == 400)
                {
                    Notifications.Raise(
                        "The image could not be uploaded as it was invalid, corrupt, " +
                        "or in an unsupported format.",
                        NotificationType.Error
                    );
                }
                else if (resp.ApiStatus == 429)
                {
                    Notifications.Raise(
                        "The image could not be uploaded because you have exhausted your Imgur credit allocation.",
                        NotificationType.Error
                    );
                }
                else if (resp.ApiStatus == 500)
                {
                    Notifications.Raise(
                        "The image could not be uploaded due to an issue with Imgur's service.",
                        NotificationType.Error
                    );
                }
                else
                {
                    Notifications.Raise(
                        String.Format(
                            "The image could not be uploaded (status code {0})",
                            resp.ApiStatus
                        ),
                        NotificationType.Error
                    );
                }
            }
            else
            {
                Notifications.Raise(
                    "The image could not be uploaded due to an unknown error.",
                    NotificationType.Error
                );
            }

            location = null;
            return false;

            #endregion
        }

        System.Windows.Window IConfigurableUploader.Window
        {
            get { return new Imgur.ImgurConfigWindow(); }
        }
    }
}
