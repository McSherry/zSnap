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
using System.Reflection;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

using zSnap.API;
using zSnap.API.Notification;
using zSnap.API.Storage;

namespace zSnap.API
{
    /// <summary>
    /// A class to manage the uploading of images to the selected image host.
    /// </summary>
    public static class Uploader
    {
#if DEBUG
        private const string BACKUP_PATH = "./Backup";
#else
        private static string BACKUP_PATH
            = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/zSnap/Backup/";
#endif
        private const string 
            PLUGIN_PATH = "./Plugins"
            ;
        internal const string LOGGING_OBJECT = "zsnap-image-log";
        private static Namespace Settings;
        private static Dictionary<string, Type> Uploaders;
        private static Dictionary<string, Type> ModesOfCapture;
        internal static StorableStringList ImageLog;

        private static void BackupImage(Image image)
        {
            DateTime now = DateTime.Now;
            string fname = String.Format(
                "{0}T{1}Z.png",
                now.ToString("yyyy-MM-dd"),
                now.ToString("hh.mm.ss")
            );
            try
            {
                Directory.CreateDirectory(BACKUP_PATH);

                using (FileStream fs = File.OpenWrite(String.Format("{0}/{1}", BACKUP_PATH, fname)))
                {
                    image.Save(fs, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Notifications.Raise(
                    "Could not save backup: inadequate permissions on directory.",
                    NotificationType.Error
                );
            }
            catch (IOException ioex)
            {
                Notifications.Raise(
                    "An unknown I/O error occurred whilst backing up.\n\n" + ioex.Message,
                    NotificationType.Error
                );
            }
            
        }
        private static void DetermineIfLoad()
        {
            bool res;
            if (bool.TryParse(Settings["LoadExternals"], out res))
            {
                List<Assembly> asms = new List<Assembly>()
                {
                    Assembly.GetEntryAssembly()
                };

                if (res)
                {
                    if (Directory.Exists(PLUGIN_PATH))
                    {
                        try
                        {
                            string[] files = Directory.GetFiles(PLUGIN_PATH, "*.dll");

                            foreach (string file in files)
                            {
                                try
                                {
                                    using (FileStream fs = File.OpenRead(file))
                                    {
                                        byte[] asmBytes = new byte[fs.Length];
                                        fs.Read(asmBytes, 0, asmBytes.Length);

                                        Assembly.Load(asmBytes);
                                    }
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Notifications.Raise(
                                "Inadequate permissions to attempt plugin loading.",
                                NotificationType.Error
                            );
                        }
                        catch (IOException)
                        {
                            Notifications.Raise(
                                "An I/O error occurred whilst attempting to load plugins.",
                                NotificationType.Error
                            );
                        }
                    }
                }

                asms.AddRange(
                    AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.FullName != asms[0].FullName)
                );

                foreach (Assembly asm in asms)
                {
                    IEnumerable<Type> qualifyingTypes = from type in asm.GetTypes()
                                                        where 
                                                            typeof(ImageUploader).IsAssignableFrom(type) &&
                                                            type.Name != typeof(ImageUploader).Name &&
                                                            type.GetConstructor(new Type[0]) != null &&
                                                            type.GetConstructor(new Type[1] { typeof(Image) }) != null
                                                        select type;

                    foreach (Type T in qualifyingTypes)
                    {
                        try
                        {
                            ImageUploader uploader = Activator.CreateInstance(T) as ImageUploader;

                            Uploaders.Add(uploader.ServiceName, T);
                        }
                        catch (Exception)
                        {
                            // We don't care if an uploader throws an exception. If it does, we
                            // just won't load it. Plus, this way, services with duplicate names
                            // don't get loaded.
                            //
                            // We've ensured that our own uploaders are considered first, so
                            // any plugin uploaders won't override ours.
                        }
                    }
                }
            }
            else
            {
                Settings["LoadExternals"] = Entry.DefaultSettings["LoadExternals"];
                DetermineIfLoad();
            }
        }

        static Uploader()
        {
            Settings = Storage.Storage.GetNamespace(Entry.NAMESPACE);
            Uploaders = new Dictionary<string, Type>();
            ModesOfCapture = new Dictionary<string, Type>();

            DetermineIfLoad();

            #region Load Modes of Capture
            Assembly thisAsm = Assembly.GetEntryAssembly();
            var captureTypes = from T in thisAsm.GetTypes()
                               where typeof(ImageCaptor).IsAssignableFrom(T)
                               where T.Name != typeof(ImageCaptor).Name
                               select T;

            foreach (Type T in captureTypes)
            {
                ImageCaptor ic = null;
                try
                {
                    ic = Activator.CreateInstance(T) as ImageCaptor;
                }
                catch (Exception)
                {
                    continue;
                }

                ModesOfCapture.Add(ic.Name, T);
            }
            #endregion

            #region Set uploader settings
            if (
                !Settings.Keys.Contains(Entry.SETTING_HOST) ||
                !Uploaders.Keys.Contains(Settings[Entry.SETTING_HOST])
            ) Settings[Entry.SETTING_HOST] = Uploaders.ElementAt(0).Key;
            #endregion
            #region Set capture settings
            if (
                !Settings.Keys.Contains(Entry.SETTING_CAPTUREMODE) ||
                !ModesOfCapture.Keys.Contains(Settings[Entry.SETTING_CAPTUREMODE])
            ) Settings[Entry.SETTING_CAPTUREMODE] = ModesOfCapture.ElementAt(0).Key;
            #endregion
            #region Load image log
            if (!Storage.Storage.ObjectExists<StorableStringList>(LOGGING_OBJECT))
            {
                ImageLog = new StorableStringList(LOGGING_OBJECT);
                Storage.Storage.StoreObject(ImageLog);
            }
            else
            {
                ImageLog = Storage.Storage.GetObject<StorableStringList>(LOGGING_OBJECT);
            }
            #endregion

            bool doBacks;
            if (!bool.TryParse(Settings[Entry.SETTING_DOBACKUP], out doBacks))
            {
                Settings[Entry.SETTING_DOBACKUP] = Entry.DefaultSettings[Entry.SETTING_DOBACKUP];
            }
        }

        /// <summary>
        /// The names of the services loaded for use.
        /// </summary>
        public static List<string> Services
        {
            get { return Uploaders.Keys.ToList(); }
        }
        /// <summary>
        /// The loaded services with their associated types.
        /// </summary>
        public static Dictionary<string, Type> Types
        {
            get { return new Dictionary<string, Type>(Uploaders); }
        }
        /// <summary>
        /// A dictionary containing all available modes of capture.
        /// </summary>
        public static Dictionary<string, Type> CaptureModes
        {
            get { return new Dictionary<string, Type>(ModesOfCapture); }
        }
        /// <summary>
        /// The name of the currently-selected upload service.
        /// </summary>
        public static string SelectedServiceName
        {
            get
            {
                Type uploaderType = Uploaders[Settings[Entry.SETTING_HOST]];
                var uploader = Activator.CreateInstance(uploaderType) as ImageUploader;

                return uploader.ServiceName;
            }
        }

        /// <summary>
        /// Uploads an image using the currently-selected image uploader.
        /// </summary>
        /// <param name="image">The image to upload.</param>
        /// <param name="location">Where the location of the uploaded image will be stored.</param>
        /// <returns>A URI representing the location of the uploaded image.</returns>
        public static bool Upload(Image image, out Uri location)
        {
            Entry.IconStartLoadView();

            if (bool.Parse(Settings[Entry.SETTING_DOBACKUP]))
                BackupImage(image);

            if (Interop.InternetCheckConnection())
            {
                string serviceName = Settings[Entry.SETTING_HOST];
                Type uploaderType = Uploaders[serviceName];

                try
                {
                    var uploader = Activator.CreateInstance(uploaderType, image) as ImageUploader;

                    bool ret = uploader.Upload(out location);

                    bool doLog;
                    if (!Settings.RetrieveSafe<bool>(Entry.SETTING_DOLOGGING, out doLog))
                    {
                        Settings[Entry.SETTING_DOLOGGING] = Entry.DefaultSettings[Entry.SETTING_DOLOGGING];
                        doLog = bool.Parse(Entry.DefaultSettings[Entry.SETTING_DOLOGGING]);
                    }

                    if (doLog)
                    {
                        /*
                         *  Current format: {format version}, {datetime}, {service}, {uri}
                         *  This format is: version 0
                         *  
                         *      Version 0 Format String: 0,{0},{1},{2}\r\n
                         */
                        ImageLog.Add(
                            String.Format(
                                "0,{0},{1},{2}\r\n",
                                DateTime.Now.ToEpochTime(),
                                SelectedServiceName,
                                location
                            )
                        );
                        Storage.Storage.StoreObject(ImageLog);
                    }

                    Entry.IconStopLoadView();
                    return ret;
                }
                catch (Exception ex)
                {
                    Notifications.Raise(
                        String.Format(
                            "{0}\n\n{1}",
                            "An error occurred:",
                            ex.Message
                        ),
                        NotificationType.Error
                    );

                    Entry.IconStopLoadView();
                    location = null;
                    return false;
                }
            }
            else
            {
                location = null;

                Notifications.Raise(
                    "Cannot upload image:\n\nNo Internet connection.",
                    NotificationType.Error
                );

                Entry.IconStopLoadView();
                return false;
            }
        }
        /// <summary>
        /// Attempts to capture an image using the selected ImageCaptor.
        /// </summary>
        /// <param name="image">The captured image.</param>
        /// <returns>True if an image was captured, false if otherwise.</returns>
        public static bool Capture(out Image image)
        {
            Type T = ModesOfCapture[Settings[Entry.SETTING_CAPTUREMODE]];

            ImageCaptor captor = Activator.CreateInstance(T) as ImageCaptor;

            return captor.Capture(out image);
        }
    }
}
