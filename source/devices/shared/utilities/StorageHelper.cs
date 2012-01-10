using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using TaskStoreClientEntities;
using System.Text;
using System.Threading;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;

namespace TaskStoreWinPhoneUtilities
{
    public class StorageHelper
    {
        static private Dictionary<string, object> fileLocks = new Dictionary<string, object>()
        {
            { "Constants", new object() },
            { "Lists",     new object() },
            { "ListTypes", new object() },
            { "Tags",      new object() },
            { "TaskStore", new object() },
        };
        
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
        /// Get the Default TaskList ID from isolated storage
        /// </summary>
        /// <returns>TaskList ID if saved, otherwise null</returns>
        public static Guid ReadDefaultTaskListID()
        {
            try
            {
                Guid guid = new Guid((string)AppSettings["DefaultTaskListID"]);
                return guid;
            }
            catch (Exception)
            {
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Write Default TaskList ID to isolated storage
        /// </summary>
        /// <param name="user">TaskList ID to write</param>
        public static void WriteDefaultTaskListID(Guid? defaultTaskListID)
        {
            try
            {
                if (defaultTaskListID == null)
                    AppSettings["DefaultTaskListID"] = null;
                else
                    AppSettings["DefaultTaskListID"] = (string)defaultTaskListID.ToString();
                AppSettings.Save();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Delete the list from isolated storage
        /// </summary>
        public static void DeleteList(TaskList list)
        {
            // construct the list name
            string name = String.Format("{0}-{1}", list.Name, list.ID.ToString());

            // remove the list file using the internal function logic
            InternalWriteFile<TaskList>(null, name);
        }

        /// <summary>
        /// Read the contents of the List XML file from isolated storage
        /// </summary>
        /// <returns>retrieved list</returns>
        public static TaskList ReadList(string name)
        {
            return InternalReadFile<TaskList>(name);
        }

        /// <summary>
        /// Write the List XML to isolated storage
        /// </summary>
        public static void WriteList(TaskList list)
        {
            // construct the list name
            string name = String.Format("{0}-{1}", list.Name, list.ID.ToString());

            // make a copy and do the write on the background thread
            var copy = new TaskList(list);
            ThreadPool.QueueUserWorkItem(delegate { InternalWriteFile<TaskList>(copy, name); });
        }

        /// <summary>
        /// Read the contents of the ListTypes XML file from isolated storage
        /// </summary>
        /// <returns>retrieved list of ListTypes</returns>
        public static ObservableCollection<ListType> ReadListTypes()
        {
            return InternalReadFile<ObservableCollection<ListType>>("ListTypes");
        }

        /// <summary>
        /// Write the ListTypes XML to isolated storage
        /// </summary>
        public static void WriteListTypes(ObservableCollection<ListType> listTypes)
        {
            // if the data passed in is null, remove the corresponding file on the foreground thread
            if (listTypes == null)
            {
                InternalWriteFile<ObservableCollection<ListType>>(null, "ListTypes");
                return;
            }

            // make a copy and do the write on the background thread
            var copy = new ObservableCollection<ListType>();
            foreach (var item in listTypes)
                copy.Add(new ListType(item));
            ThreadPool.QueueUserWorkItem(delegate { InternalWriteFile<ObservableCollection<ListType>>(copy, "ListTypes"); });
        }

        /// <summary>
        /// Read the contents of the Tags XML file from isolated storage
        /// </summary>
        /// <returns>retrieved list of Tags</returns>
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
        /// Read the contents of the TaskStore XML file from isolated storage
        /// </summary>
        /// <returns>retrieved list of TaskLists</returns>
        public static ObservableCollection<TaskList> ReadTaskLists()
        {
            return InternalReadFile<ObservableCollection<TaskList>>("TaskLists");
        }

        /// <summary>
        /// Write the TaskStore XML to isolated storage
        /// </summary>
        public static void WriteTaskLists(ObservableCollection<TaskList> taskLists)
        {
            // if the data passed in is null, remove the corresponding file on the foreground thread
            if (taskLists == null)
            {
                InternalWriteFile<ObservableCollection<TaskList>>(null, "TaskLists");
                return;
            }

            // make a copy and do the write on the background thread
            var copy = new ObservableCollection<TaskList>();
            foreach (var item in taskLists)
                copy.Add(new TaskList(item, false));  // do a shallow copy
            ThreadPool.QueueUserWorkItem(delegate { InternalWriteFile<ObservableCollection<TaskList>>(copy, "TaskLists"); });
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
            // use the app's isolated storage to retrieve the tasks
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
                            DateTime one = DateTime.Now;
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
        /// <typeparam name="T">Type of the items in the list passed in</typeparam>
        /// <param name="obj">List to serialize</param>
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

            // This method is only thread-safe IF the list parameter that is passed in is locked as well.
            // this is because the DCS below will enumerate through the list and if the list is modified while
            // this enumeration is taking place, DCS will throw.
            lock (fileLock)
            {
                // use the app's isolated storage to write the tasks
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
