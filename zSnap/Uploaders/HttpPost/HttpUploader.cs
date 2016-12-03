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
        public new Image Image { get; }

        public override string Name => "HTTP Basic Auth Image Uploader";

        public override string ServiceName => "Custom HTTP Server";

        public Window Window { get; } = new HttpConfigWindow();

        private const int MAX_FILENAME_GENERATION_ATTEMPTS = 10;

        private static string RandomString(int length = 10)
        {
            var rand = new Random();
            const string validChars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(new char[length].Select(c => c = validChars[rand.Next(0, validChars.Length)]).ToArray());
        }

        //
        public HttpPostUploader() { }

        public HttpPostUploader(Image image)
        {
            Image = image;
        }

        public override bool Upload(out Uri location)
        {
            HttpResponseMessage response;

            string username = null;
            string password = null;

            if (Configuration.UseBasicAuth)
            {
                username = Configuration.Username;
                password = Configuration.Password;
            }

            // TLSv1.1 and TLSv1.2 are disabled by default, which causes problems with newer
            // web servers that don't support TLSv1.0 or older, so let's enable them.
            var previousSecurityProtocol = ServicePointManager.SecurityProtocol;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            var attempt = 0;
            var filename = "";
            do
            {
                // Ensure we don't end up in an endless loop if IsFileUnique() always returns false.
                if (++attempt > MAX_FILENAME_GENERATION_ATTEMPTS)
                {
                    Notifications.Raise($"Unable to generate a unique filename.\nA file named '{filename}' already exists.", NotificationType.Error);
                    location = null;
                    return false;
                }
                filename = RandomString(Configuration.FilenameLength) + ".png";
            } while (!IsFileUnique(filename));

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
            // We're done uploading; reset the SecurityProtocol to its previous settings.
            ServicePointManager.SecurityProtocol = previousSecurityProtocol;

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
            return GetFilename(response, filename, out location);
        }

        /// <summary>
        /// Determines whether a filename is unique, to prevent it from being overwritten
        /// when an image is uploaded under that filename.
        /// </summary>
        /// <param name="filename">The filename to check.</param>
        /// <returns>True if no file with the specified name exists,
        /// or False if a file with that name does exist.</returns>
        private bool IsFileUnique(string filename)
        {
            Uri location;
            switch (Configuration.ImageUrlMethod)
            {
                // If ImageUrlMethod is not defined, the user will be warned about this later,
                // so no action needs to be taken.
                default:
                    return true;
                // If the filename is returned in the response, we can assume that the server will
                // generate it, and therefore, that there's no risk of accidentally overwriting
                // an existing file.
                case "FromResponse":
                    return true;
                case "FromRequest":
                    location = new Uri(Configuration.Destination + filename);
                    break;
                case "CustomUrl":
                    location = GetCustomUrl(filename);
                    break;

            }
            using (var client = new HttpClient())
            {
                var response = client.SendAsync(new HttpRequestMessage(HttpMethod.Head, location)).Result;
                return !response.IsSuccessStatusCode;
            }
        }

        private Uri GetCustomUrl(string filename)
        {
            return new Uri(Configuration.CustomUrl.Replace("$filename", filename));
        }

        private bool GetFilename(HttpResponseMessage response, string uploadedFileName, out Uri location)
        {
            var responseText = response.Content.ReadAsStringAsync().Result;
            switch (Configuration.ImageUrlMethod)
            {
                case "FromResponse":
                    if (Configuration.UseRegex)
                    {
                        var rgx = new Regex(Configuration.Regex);
                        var match = rgx.Match(responseText);
                        if (match.Success)
                        {
                            location = new Uri(match.Groups["url"].Value);
                            return true;
                        }
                        else
                        {
                            Clipboard.SetText(responseText);
                            Notifications.Raise(
                                "Unable to parse the image URL from the response.\nPress Ctrl+V to paste the full response returned by the server.",
                                NotificationType.Error);
                            location = null;
                            return false;
                        }
                    }
                    else
                    {
                        location = new Uri(responseText);
                        return true;
                    }
                case "FromRequest":
                    location = response.RequestMessage.RequestUri;
                    return true;
                case "CustomUrl":
                    location = GetCustomUrl(uploadedFileName);
                    return true;
                default:
                    location = null;
                    Clipboard.SetText(responseText);
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
        /// <returns>The HTTP response generated by the server</returns>
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
        /// <returns>The HTTP response generated by the server</returns>
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
                if (useAuth)
                {
                    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + token));
                    client.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials);
                }

                var ms = new MemoryStream();
                image.Save(ms, ImageFormat.Png);
                ms.Position = 0;

                var response = client.PutAsync(remoteHost + filename, new StreamContent(ms)).Result;
                return response;
            }
        }
    }
}
