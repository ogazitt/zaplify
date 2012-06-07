using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization.Json;
using System.Threading;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.Devices.ClientHelpers
{
    public class StorageHelper
    {
        static private Dictionary<string, object> fileLocks = new Dictionary<string, object>()
        {
            { "Constants", new object() },
            { "Folders",   new object() },
            { "ItemTypes", new object() },
            { "Tags",      new object() },
            { "Zaplify",   new object() },
        };

        const string CrashReportFileName = "trace.txt";

        // alias for Application Settings
        static private IsolatedStorageSettings AppSettings = IsolatedStorageSettings.ApplicationSettings;

        /// <summary>
        /// Read the contents of the Constants XML file from isolated storage
        /// </summary>
        /// <returns>retrieved constants</returns>
        public static Constants ReadConstants()
        {
            return InternalReadFile<Constants>("Constants");
        }

        /// <summary>
        /// Write the Constants XML to isolated storage
        /// </summary>
        public static void WriteConstants(Constants constants)
        {
            // if the data passed in is null, remove the corresponding file on the foreground thread
            if (constants == null)
            {
                InternalWriteFile<Constants>(null, "Constants");
                return;
            }

            // make a copy and do the write on the background thread
            var copy = new Constants(constants);
            ThreadPool.QueueUserWorkItem(delegate { InternalWriteFile<Constants>(copy, "Constants"); });
        }

        /// <summary>
        /// Read the contents of the ClientSettings Folder XML file from isolated storage
        /// </summary>
        /// <returns>retrieved folder</returns>
        public static Folder ReadClientSettings()
        {
            return InternalReadFile<Folder>(SystemEntities.ClientSettings);
        }

        /// <summary>
        /// Write the ClientSettings Folder XML to isolated storage
        /// </summary>
        public static void WriteClientSettings(Folder folder)
        {
            // save all the system entity ID's
            WriteSystemEntityID(SystemEntities.ClientSettings, folder.ID);
            foreach (var item in folder.Items)
            {
                if (item == null)
                    continue;
                if (item.Name != SystemEntities.DefaultLists &&
                    item.Name != SystemEntities.ListMetadata &&
                    item.Name != SystemEntities.PhoneSettings)
                    continue;
                WriteSystemEntityID(item.Name, item.ID);
            }

            // make a copy and do the write on the background thread
            var copy = new Folder(folder);
            ThreadPool.QueueUserWorkItem(delegate { InternalWriteFile<Folder>(copy, SystemEntities.ClientSettings); });
        }

        /// <summary>
        /// Get the Default Folder ID from isolated storage
        /// </summary>
        /// <returns>Folder ID if saved, otherwise null</returns>
        public static Guid ReadDefaultFolderID()
        {
            try
            {
                Guid guid = new Guid((string)AppSettings["DefaultFolderID"]);
                return guid;
            }
            catch (Exception)
            {
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Write Default Folder ID to isolated storage
        /// </summary>
        /// <param name="user">Folder ID to write</param>
        public static void WriteDefaultFolderID(Guid? defaultFolderID)
        {
            try
            {
                if (defaultFolderID == null)
                    AppSettings["DefaultFolderID"] = null;
                else
                    AppSettings["DefaultFolderID"] = (string)defaultFolderID.ToString();
                AppSettings.Save();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Delete the folder from isolated storage
        /// </summary>
        public static void DeleteFolder(Folder folder)
        {
            // construct the folder name
            string name = String.Format("{0}-{1}", folder.Name, folder.ID.ToString());

            // remove the folder file using the internal function logic
            InternalWriteFile<Folder>(null, name);
        }

        /// <summary>
        /// Read the contents of the Folder XML file from isolated storage
        /// </summary>
        /// <returns>retrieved folder</returns>
        public static Folder ReadFolder(string name)
        {
            return InternalReadFile<Folder>(name);
        }

        /// <summary>
        /// Write the Folder XML to isolated storage
        /// </summary>
        public static void WriteFolder(Folder folder)
        {
            // construct the folder name
            string name = String.Format("{0}-{1}", folder.Name, folder.ID.ToString());

            // make a copy and do the write on the background thread
            var copy = new Folder(folder);
            ThreadPool.QueueUserWorkItem(delegate { InternalWriteFile<Folder>(copy, name); });
        }

        /// <summary>
        /// Read the contents of the ItemTypes XML file from isolated storage
        /// </summary>
        /// <returns>retrieved folder of ItemTypes</returns>
        public static ObservableCollection<ItemType> ReadItemTypes()
        {
            return InternalReadFile<ObservableCollection<ItemType>>("ItemTypes");
        }

        /// <summary>
        /// Write the ItemTypes XML to isolated storage
        /// </summary>
        public static void WriteItemTypes(ObservableCollection<ItemType> itemTypes)
        {
            // if the data passed in is null, remove the corresponding file on the foreground thread
            if (itemTypes == null)
            {
                InternalWriteFile<ObservableCollection<ItemType>>(null, "ItemTypes");
                return;
            }

            // make a copy and do the write on the background thread
            var copy = new ObservableCollection<ItemType>();
            foreach (var item in itemTypes)
                copy.Add(new ItemType(item));
            ThreadPool.QueueUserWorkItem(delegate { InternalWriteFile<ObservableCollection<ItemType>>(copy, "ItemTypes"); });
        }

        /// <summary>
        /// Get the ID for the System Entity from isolated storage
        /// </summary>
        /// <returns>Folder ID if saved, otherwise null</returns>
        public static Guid ReadSystemEntityID(string systemEntityName)
        {
            try
            {
                Guid guid = new Guid((string)AppSettings[systemEntityName]);
                return guid;
            }
            catch (Exception ex)
            {
                TraceHelper.AddMessage(String.Format("ReadSystemEntityID: could not find entity ID for {0}; ex: {1}", systemEntityName, ex.Message)); 
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Write Default Folder ID to isolated storage
        /// </summary>
        /// <param name="user">Folder ID to write</param>
        public static void WriteSystemEntityID(string systemEntityName, Guid systemEntityID)
        {
            try
            {
                if (systemEntityID == Guid.Empty)
                    AppSettings[systemEntityName] = null;
                else
                    AppSettings[systemEntityName] = (string)systemEntityID.ToString();
                AppSettings.Save();
            }
            catch (Exception ex)
            {
                TraceHelper.AddMessage(String.Format("WriteSystemEntityID: could not write entity ID for {0}; ex: {1}", systemEntityName, ex.Message)); 
            }
        }

        /// <summary>
        /// Read the contents of the Tags XML file from isolated storage
        /// </summary>
        /// <returns>retrieved folder of Tags</returns>
        public static ObservableCollection<Tag> ReadTags()
        {
            return InternalReadFile<ObservableCollection<Tag>>("Tags");
        }

        /// <summary>
        /// Write the Tags XML to isolated storage
        /// </summary>
        public static void WriteTags(ObservableCollection<Tag> tags)
        {
            // if the data passed in is null, remove the corresponding file on the foreground thread
            if (tags == null)
            {
                InternalWriteFile<ObservableCollection<Tag>>(null, "Tags");
                return;
            }

            // make a copy and do the write on the background thread
            var copy = new ObservableCollection<Tag>();
            foreach (var item in tags)
                copy.Add(new Tag(item));
            ThreadPool.QueueUserWorkItem(delegate { InternalWriteFile<ObservableCollection<Tag>>(copy, "Tags"); });
        }

        /// <summary>
        /// Read the contents of the Zaplify XML file from isolated storage
        /// </summary>
        /// <returns>retrieved folder of Folders</returns>
        public static ObservableCollection<Folder> ReadFolders()
        {
            return InternalReadFile<ObservableCollection<Folder>>("Folders");
        }

        /// <summary>
        /// Write the Zaplify XML to isolated storage
        /// </summary>
        public static void WriteFolders(ObservableCollection<Folder> folders)
        {
            // if the data passed in is null, remove the corresponding file on the foreground thread
            if (folders == null)
            {
                InternalWriteFile<ObservableCollection<Folder>>(null, "Folders");
                return;
            }

            // make a copy and do the write on the background thread
            var copy = new ObservableCollection<Folder>();
            foreach (var item in folders)
                copy.Add(new Folder(item, false));  // do a shallow copy
            ThreadPool.QueueUserWorkItem(delegate { InternalWriteFile<ObservableCollection<Folder>>(copy, "Folders"); });
        }

        /// <summary>
        /// Get the User from isolated storage
        /// </summary>
        /// <returns>User structure if saved, otherwise null</returns>
        public static User ReadUserCredentials()
        {
            // trace reading data
            TraceHelper.AddMessage("Read User Credentials");

            try
            {
                User user = new User()
                {
                    Name = (string)AppSettings["Username"],
                    Password = (string)AppSettings["Password"],
                    Email = (string)AppSettings["Email"],
                    Synced = (bool)AppSettings["Synced"]
                };
                if (user.Name == null || user.Name == "")
                    return null;

                // trace reading data
                TraceHelper.AddMessage("Finished Read User Credentials");

                return user;
            }
            catch (Exception ex)
            {
                // trace reading data
                TraceHelper.AddMessage(String.Format("Exception Read User Credentials: ", ex.Message));

                return null;
            }
        }

        /// <summary>
        /// Write User credentials to isolated storage
        /// </summary>
        /// <param name="user">User credentials to write</param>
        public static void WriteUserCredentials(User user)
        {
            // trace writing data
            TraceHelper.AddMessage("Write User Credentials");

            try
            {
                if (user == null)
                {
                    AppSettings["Username"] = null;
                    AppSettings["Password"] = null;
                    AppSettings["Email"] = null;
                    AppSettings["Synced"] = null;
                    AppSettings.Save();
                }
                else
                {
                    AppSettings["Username"] = user.Name;
                    AppSettings["Password"] = user.Password;
                    AppSettings["Email"] = user.Email;
                    AppSettings["Synced"] = user.Synced;
                    AppSettings.Save();
                }
                
                // trace writing data
                TraceHelper.AddMessage("Finished Write User Credentials");
            }
            catch (Exception ex)
            {
                // trace writing data
                TraceHelper.AddMessage(String.Format("Exception Write User Credentials: {0}", ex.Message));
            }
        }
  
        /// <summary>
        /// Deletes the crash report from isolated storage
        /// </summary>
        public static void DeleteCrashReport()
        {
            try
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    store.DeleteFile(CrashReportFileName);
                }
            }
            catch (Exception)
            {
            }
        }
        
        /// <summary>
        /// Read the crash report from isolated storage
        /// </summary>
        /// <returns>string containing crash report</returns>
        public static string ReadCrashReport()
        {
            try
            {
                string contents = null;
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (store.FileExists(CrashReportFileName))
                    {
                        using (TextReader reader = new StreamReader(store.OpenFile(CrashReportFileName, FileMode.Open, FileAccess.Read, FileShare.None)))
                        {
                            contents = reader.ReadToEnd();
                        }
                    }
                }
                return contents;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Write crash report to isolated storage
        /// </summary>
        /// <param name="text">string containing crash report</param>
        public static void WriteCrashReport(string text)
        {
            try
            {
                DeleteCrashReport();
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (TextWriter output = new StreamWriter(store.CreateFile(CrashReportFileName)))
                    {
                        output.Write(text);
                        output.Flush();
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        #region Helpers

        /// <summary>
        /// Generic ReadFile method
        /// </summary>
        /// <typeparam name="T">Type of the returned items</typeparam>
        /// <param name="elementName">Name of the element (as well as the prefix of the filename)</param>
        /// <returns>ObservableCollection of the type passed in</returns>
        private static T InternalReadFile<T>(string elementName)
        {
            // trace reading data
            TraceHelper.AddMessage(String.Format("Reading {0}", elementName));

            T type;
            // use the app's isolated storage to retrieve the items
            using (IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication())
            {
                // use a DCS to de/serialize the xml file
                DataContractJsonSerializer dc = new DataContractJsonSerializer(typeof(T));
                IsolatedStorageFileStream stream = null;

                // try to open the file
                try
                {
                    using (stream = new IsolatedStorageFileStream(elementName + ".json", FileMode.Open, file))
                    {
                        // if the file opens, read the contents and replace the generated data
                        try
                        {
                            type = (T)dc.ReadObject(stream);
                        }
                        catch (Exception ex)
                        {
                            stream.Position = 0;
                            string s = new StreamReader(stream).ReadToEnd();

                            // trace exception
                            TraceHelper.AddMessage(String.Format("Exception Reading {0}: {1}; {2}", elementName, ex.Message, s));
           
                            return default(T);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // trace exception
                    TraceHelper.AddMessage(String.Format("Exception Reading {0}: {1}", elementName, ex.Message));

                    return default(T);
                }

                return type;
            }
        }

        /// <summary>
        /// Generic WriteFile method
        /// </summary>
        /// <typeparam name="T">Type of the items in the folder passed in</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <param name="elementName">Name of the element (as well as the prefix of the filename)</param>
        private static void InternalWriteFile<T>(T obj, string elementName)
        {
            // trace writing data
            TraceHelper.AddMessage(String.Format("Writing {0}", elementName));

            // obtain the object to lock (or create one if it doesn't exist)
            object fileLock;
            if (fileLocks.TryGetValue(elementName, out fileLock) == false)
            {
                fileLock = new Object();
                fileLocks[elementName] = fileLock; 
            }

            // This method is only thread-safe IF the folder parameter that is passed in is locked as well.
            // this is because the DCS below will enumerate through the folder and if the folder is modified while
            // this enumeration is taking place, DCS will throw.
            lock (fileLock)
            {
                // use the app's isolated storage to write the items
                using (IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    try
                    {
                        if (obj == null)
                        {
                            file.DeleteFile(elementName + ".json");
                            return;
                        }

                        DataContractJsonSerializer dc = new DataContractJsonSerializer(obj.GetType());
                        using (IsolatedStorageFileStream stream = file.CreateFile(elementName + ".json"))
                        {
                            dc.WriteObject(stream, obj);
                        }

                        // trace writing data
                        TraceHelper.AddMessage(String.Format("Finished Writing {0}", elementName));
                    }
                    catch (Exception ex)
                    {
                        // trace exception
                        TraceHelper.AddMessage(String.Format("Exception Writing {0}: {1}", elementName, ex.Message));
                    }
                }
            }
        }

        #endregion
    }
}
