/*
 * Copyright 2014-2016 (c) Johan Geluk <johan@jgeluk.net>
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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using zSnap.API;
using zSnap.API.Notification;

namespace zSnap.Uploaders.HttpPost
{
    public class HttpPostUploader : ImageUploader, IConfigurableUploader
    {
        public new Image Image { get; private set; }

        public HttpPostUploader()
        {

        }

        private static string RandomString(int length = 10)
        {
            var rand = new Random();
            const string validChars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(new char[length].Select(c => c = validChars[rand.Next(0, validChars.Length)]).ToArray());
        }

        public HttpPostUploader(Image image)
        {
            Image = image;
        }

        public override bool Upload(out Uri location)
        {
            HttpResponseMessage response;

            string username = null;
            string password = null;
            string filename = RandomString() + ".png";

            if (Configuration.UseBasicAuth)
            {
                username = Configuration.Username;
                password = Configuration.Password;
            }
            switch (Configuration.UploadMethod)
            {
                case "POST":
                    response = UploadFile(Image, filename, Configuration.Destination, username, password);
                    break;
                case "PUT":
                    response = PutFile(Image, filename, Configuration.Destination, username, password);
                    break;
                default:
                    Notifications.Raise($"Invalid upload method: \"{Configuration.UploadMethod}\"", NotificationType.Error);
                    location = null;
                    return false;
            }
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                Notifications.Raise("The remote server returned an error: " + ex.Message, NotificationType.Error);
                location = null;
                return false;
            }
            var result = response.Content.ReadAsStringAsync().Result;

            switch (Configuration.ImageUrlMethod)
            {
                case "FromResponse":
                    if (Configuration.UseRegex)
                    {
                        var rgx = new Regex(Configuration.Regex);
                        var match = rgx.Match(result);
                        if (match.Success)
                        {
                            location = new Uri(match.Groups["url"].Value);
                            return true;
                        }
                        else
                        {
                            Clipboard.SetText(result);
                            Notifications.Raise(
                                "Unable to parse the image URL from the response.\nPress Ctrl+V to paste the full response returned by the server.",
                                NotificationType.Error);
                            location = null;
                            return false;
                        }
                    }
                    else
                    {
                        location = new Uri(result);
                        return true;
                    }
                case "FromRequest":
                    location = response.RequestMessage.RequestUri;
                    return true;
                case "CustomUrl":
                    location = new Uri(Configuration.CustomUrl.Replace("$filename", filename));
                    return true;
                default:
                    location = null;
                    Clipboard.SetText(result);
                    Notifications.Raise(
                        "You must configure an image URL to get the URL on your clipboard. Press Ctrl+V to paste the full response returned by the server.",
                        NotificationType.Error);
                    return false;
            }
        }

        /// <summary>
        /// Uploads an image using an HTTP POST request without using HTTP Basic Authentication.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="filename"></param>
        /// <param name="remoteHost"></param>
        /// <returns></returns>
        public static HttpResponseMessage UploadFile(Image image, string filename, string remoteHost)
        {
            return UploadFile(image, filename, remoteHost, null, null);
        }

        /// <summary>
        /// Uploads an image using an HTTP POST request, using HTTP Basic Authentication.
        /// </summary>
        /// <param name="image">The image file to be uploaded.</param>
        /// <param name="filename">The filename under which the image should be uploaded.</param>
        /// <param name="remoteHost">The URL to send the request to</param>
        /// <param name="username">The username, or null if auth should not be used.</param>
        /// <param name="token">The token or password, or null if auth should not be used.</param>
        /// <returns></returns>
        public static HttpResponseMessage UploadFile(Image image, string filename, string remoteHost, string username, string token)
        {
            bool useAuth;
            if (username == null && token == null)
            {
                useAuth = false;
            }
            else if (username == null || token == null)
            {
                throw new ArgumentException("You must supply a username and a token, or no credentials at all.");
            }
            else
            {
                useAuth = true;
            }

            using (var client = new HttpClient())
            {
                var form = new MultipartFormDataContent();
                if (useAuth)
                {
                    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + token));
                    client.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials);
                }
                var ms = new MemoryStream();
                image.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                form.Add(new StreamContent(ms), "screenshot", filename);
                var response = client.PostAsync(remoteHost, form).Result;
                return response;
            }
        }

        /// <summary>
        /// Uploads an image using an HTTP POST request, using HTTP Basic Authentication.
        /// </summary>
        /// <param name="image">The image file to be uploaded.</param>
        /// <param name="filename">The filename under which the image should be uploaded.</param>
        /// <param name="remoteHost">The URL to send the request to</param>
        /// <param name="username">The username, or null if auth should not be used.</param>
        /// <param name="token">The token or password, or null if auth should not be used.</param>
        /// <returns></returns>
        public static HttpResponseMessage PutFile(Image image, string filename, string remoteHost, string username, string token)
        {
            var useAuth = true;
            if (username == null && token == null)
            {
                useAuth = false;
            }
            else if (username == null || token == null)
            {
                throw new ArgumentException("You must supply a username and a token, or no credentials at all.");
            }

            using (var client = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                if (useAuth)
                {
                    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + token));
                    client.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials);
                }

                var status = client.SendAsync(new HttpRequestMessage(new HttpMethod("PROPFIND"), remoteHost + filename)).Result;
                if (status.StatusCode != HttpStatusCode.NotFound)
                {
                    throw new ArgumentException($"A file named {filename} already exists.");
                }
                var ms = new MemoryStream();
                image.Save(ms, ImageFormat.Png);
                ms.Position = 0;

                var response = client.PutAsync(remoteHost + filename, new StreamContent(ms)).Result;
                return response;
            }
        }

        public override string Name
        {
            get { return "HTTP Basic Auth Image Uploader"; }
        }

        public override string ServiceName
        {
            get { return "Custom HTTP Server"; }
        }

        public Window Window { get { return new HttpConfigWindow(); } }
    }
}
