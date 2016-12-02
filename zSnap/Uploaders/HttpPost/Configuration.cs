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
using zSnap.API.Storage;

namespace zSnap.Uploaders.HttpPost
{
    static class Configuration
    {
        private const string NamespaceName = "http-basic-auth-uploader";
        private static Namespace storageNamespace;

        static Configuration()
        {
            storageNamespace = Storage.GetNamespace(NamespaceName) ?? Storage.CreateNamespace(NamespaceName);

        }

        // HTTP basic auth credentials
        public static string Username
        {
            get { return storageNamespace["username"]; }
            set { storageNamespace["username"] = value; }
        }

        public static string Password { get { return storageNamespace["password"]; } set { storageNamespace["password"] = value; } }

        // If true, ignores the credentials and prevents them from being sent.
        public static bool UseBasicAuth
        {
            get
            {
                bool result;
                if (bool.TryParse(storageNamespace["useBasicAuth"], out result))
                {
                    return result;
                }
                else
                {
                    storageNamespace["useBasicAuth"] = "False";
                    return UseBasicAuth;
                }
            }
            set { storageNamespace["useBasicAuth"] = value.ToString(); }
        }


        // The server to which data should be sent
        public static string Destination { get { return storageNamespace["destination"]; } set { storageNamespace["destination"] = value; } }

        // The method that should be used for uploading data
        public static string UploadMethod { get { return storageNamespace["uploadMethod"]; } set { storageNamespace["uploadMethod"] = value; } }


        // if true, attempts to parse the response using a user-supplied regex string
        public static bool UseRegex
        {
            get
            {
                bool result;
                if (bool.TryParse(storageNamespace["useRegex"], out result))
                {
                    return result;
                }
                else
                {
                    storageNamespace["useRegex"] = "False";
                    return UseRegex;
                }
            }
            set { storageNamespace["useRegex"] = value.ToString(); }
        }

        public static string Regex { get { return storageNamespace["regex"]; } set { storageNamespace["regex"] = value; } }

        public static string CustomUrl { get { return storageNamespace["customUrl"]; } set { storageNamespace["customUrl"] = value; } }

        // The method to be used to determine the URL
        public static string ImageUrlMethod { get { return storageNamespace["imageUrl"]; } set { storageNamespace["imageUrl"] = value; } }

    }
}
