using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
        private Random rand = new Random();

        public HttpPostUploader()
        {

        }

        private string RandomString(int length = 10)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(new char[length].Select(c => c = validChars[rand.Next(0, validChars.Length)]).ToArray());
        }

        public HttpPostUploader(Image image)
        {
            Image = image;
        }

        public override bool Upload(out Uri location)
        {
            using (var client = new HttpClient())
            {
                var form = new MultipartFormDataContent();
                if (Configuration.UseBasicAuth)
                {
                    var credentials =
                        Convert.ToBase64String(
                            Encoding.UTF8.GetBytes(Configuration.Username + ":" + Configuration.Password));
                    client.DefaultRequestHeaders.Add("Authorization", credentials);
                }
                var ms = new MemoryStream();
                Image.Save(ms, ImageFormat.Png);
                form.Add(new StreamContent(ms), "screenshot", RandomString() + ".png");
                var response = client.PostAsync(Configuration.Destination, form).Result;

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
                if (Configuration.UseRegex)
                {
                    var rgx = new Regex(Configuration.Regex);
                    var match = rgx.Match(result);
                    if (match.Success)
                    {
                        location = new Uri(match.Groups["url"].Value);
                    }
                    else
                    {
                        Clipboard.SetText(result);
                        Notifications.Raise("Unable to parse the image URL from the response.\nPress Ctrl+V to paste the full response returned by the server.", NotificationType.Error);
                        location = null;
                        return false;
                    }
                }
                else
                {
                    location = new Uri(result);
                }
            }
            return true;
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
