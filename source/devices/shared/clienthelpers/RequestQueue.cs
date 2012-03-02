using System;
using System.Net;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Json;
using System.Linq;
using System.Reflection;

using BuiltSteady.Zaplify.Devices.ClientEntities;

namespace BuiltSteady.Zaplify.Devices.ClientHelpers
{
    public class RequestQueue
    {
        // Lock object for the queue file
        private static object fileLock = new Object();
            
        /// <summary>
        /// Web Request Record
        /// </summary>
        [DataContract(Namespace="")]
        public class RequestRecord
        {
            public enum RequestType { Delete, Insert, Update };

            // don't serialize this - we do custom serialization/deserialization 
            // because the body can be polymorphic
            [IgnoreDataMember]  
            public object Body { get; set; }

            [DataMember]
            public string BodyType { get; set; }

            [DataMember]
            public string BodyTypeName { get; set; }

            [DataMember]
            public Guid ID { get; set; }

            [DataMember]
            public RequestType ReqType { get; set; }

            [DataMember]
            public string SerializedBody { get; set; }

            // deep-copy the passed in newRecord
            public void Copy(RequestRecord record)
            {
                // copy all of the properties
                foreach (PropertyInfo pi in record.GetType().GetProperties())
                {
                    // get the value of the property
                    var val = pi.GetValue(record, null);
                    pi.SetValue(this, val, null);
                }
            }

            // deserialize the SerializedBody into Body
            public void DeserializeBody()
            {
                MemoryStream stream = new MemoryStream();
                StreamWriter writer = new StreamWriter(stream);
                writer.Write(this.SerializedBody);
                writer.Flush();
                Type t = Type.GetType(BodyType);

                try
                {
					stream.Position = 0;
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(t);
                    this.Body = ser.ReadObject(stream);
                }
                catch (Exception ex)
                {
                    stream.Position = 0;
                    string s = new StreamReader(stream).ReadToEnd();
					TraceHelper.AddMessage(String.Format("Exception in deserializing body: {0}; record: {1}", ex.Message, s));
                }
            }

            // serialize the Body into SerializedBody
            public void SerializeBody()
            {
                if (BodyTypeName == null)
                    BodyTypeName = this.Body.GetType().Name;

                if (BodyType == null)
                    BodyType = this.Body.GetType().AssemblyQualifiedName;

                // if the ID is null, try to create it
                // this may fail because for update requests, Body is a List<>, not a ZaplifyEntity
                if (ID == Guid.Empty)
                {
                    try
                    {
                        ClientEntity entity = (ClientEntity)this.Body;
                        ID = entity.ID;
                    }
                    catch (Exception)
                    {
                        // this must be an update... no harm no foul.
                    }
                }

                // serialize the body
                DataContractJsonSerializer ser = new DataContractJsonSerializer(this.Body.GetType());
                MemoryStream memstr = new MemoryStream();
                ser.WriteObject(memstr, this.Body);

                // reset the stream position and read the contents 
                memstr.Position = 0;
                StreamReader reader = new StreamReader(memstr);
                this.SerializedBody = reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Delete the queue
        /// </summary>
        public static void DeleteQueue()
        {
            using (IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication())
            {
                lock (fileLock)
                {
                    file.DeleteFile(@"RequestRecords.xml");
                }
            }
        }

        /// <summary>
        /// Dequeue the first record 
        /// </summary>
        public static RequestRecord DequeueRequestRecord()
        {
            List<RequestRecord> requests = new List<RequestRecord>();
            DataContractJsonSerializer dc = new DataContractJsonSerializer(requests.GetType());

            using (IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication())
            {
                lock (fileLock)
                {
                    using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(@"RequestRecords.xml", FileMode.Open, file))
                    {
                        try
                        {
                            // if the file opens, read the contents 
                            requests = dc.ReadObject(stream) as List<RequestRecord>;
                            if (requests.Count > 0)
                            {
                                RequestRecord record = requests[0];
                                requests.Remove(record);  // remove the first entry
                                stream.SetLength(0);
                                stream.Position = 0;
                                dc.WriteObject(stream, requests);

                                record.DeserializeBody();
                                return record;
                            }
                            else
                                return null;
                        }
                        catch (Exception ex)
                        {
                            stream.Position = 0;
                            string s = new StreamReader(stream).ReadToEnd();
							TraceHelper.AddMessage(String.Format("Exception in deserializing RequestRecord: {0}; record: {1}", ex.Message, s));
                            return null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enqueue a Web Service record into the record queue
        /// </summary>
        public static void EnqueueRequestRecord(RequestRecord newRecord)
        {            
            bool enableQueueOptimization = false;  // turn off the queue optimization (doesn't work with introduction of tags)

            List<RequestRecord> requests = new List<RequestRecord>();
            DataContractJsonSerializer dc = new DataContractJsonSerializer(requests.GetType());

            if (newRecord.SerializedBody == null)
                newRecord.SerializeBody();

            using (IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication())
            {
                lock (fileLock)
                {
                    // if the file opens, read the contents 
                    using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(@"RequestRecords.xml", FileMode.OpenOrCreate, file))
                    {
                        try
                        {
                            // if the file opened, read the record queue
                            requests = dc.ReadObject(stream) as List<RequestRecord>;
                            if (requests == null)
                                requests = new List<RequestRecord>();
                        }
                        catch (Exception ex)
                        {
                            stream.Position = 0;
                            string s = new StreamReader(stream).ReadToEnd();
							TraceHelper.AddMessage(String.Format("Exception in deserializing RequestRQueue: {0}; record: {1}", ex.Message, s));
                        }

                        if (enableQueueOptimization == true)
                        {
                            OptimizeQueue(newRecord, requests);
                        }
                        else
                        {
                            // this is a new record so add the new record at the end
                            requests.Add(newRecord);
                        }

                        // reset the stream and write the new record queue back to the file
                        stream.SetLength(0);
                        stream.Position = 0;
                        dc.WriteObject(stream, requests);
                        stream.Flush();
                    }
                }
            }
        }

        /// <summary>
        /// Get all RequestRecords in the queue
        /// </summary>
        public static List<RequestRecord> GetAllRequestRecords()
        {
            List<RequestRecord> requests = new List<RequestRecord>();
            DataContractJsonSerializer dc = new DataContractJsonSerializer(requests.GetType());

            using (IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication())
            {
                lock (fileLock)
                {
                    // try block because the using block below will throw if the file doesn't exist
                    try
                    {
                        // if the file opens, read the contents 
                        using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(@"RequestRecords.xml", FileMode.Open, file))
                        {
                            try
                            {
                                // if the file opens, read the contents 
                                requests = dc.ReadObject(stream) as List<RequestRecord>;
                                foreach (var req in requests)
                                {
                                    req.DeserializeBody();
                                }
                                return requests;
                            }
                            catch (Exception)
                            {
                                return null;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // could not open the isolated storage file
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Get the first RequestRecord in the queue
        /// </summary>
        public static RequestRecord GetRequestRecord()
        {
            List<RequestRecord> requests = new List<RequestRecord>();
            DataContractJsonSerializer dc = new DataContractJsonSerializer(requests.GetType());

            using (IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication())
            {
                lock (fileLock)
                {
                    // try block because the using block below will throw if the file doesn't exist
                    try
                    {
                        // if the file opens, read the contents 
                        using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(@"RequestRecords.xml", FileMode.Open, file))
                        {
                            try
                            {
                                // if the file opens, read the contents 
                                requests = dc.ReadObject(stream) as List<RequestRecord>;
                                if (requests.Count > 0)
                                {
                                    RequestRecord record = requests[0];
                                    record.DeserializeBody();
                                    return record;
                                }
                                else
                                    return null;
                            }
                            catch (Exception ex)
                            {
                                stream.Position = 0;
                                string s = new StreamReader(stream).ReadToEnd();
								TraceHelper.AddMessage(String.Format("Exception in deserializing RequestRecord: {0}; record: {1}", ex.Message, s));
                                return null;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // could not open the isolated storage file
                        return null;
                    }
                }
            }
        }
		
		/// <summary>
		/// Parse out the type name, request type, ID, and Name of the record 
		/// </summary>
		/// <param name='req'>
		/// Request record.
		/// </param>
		/// <param name='typename'>
		/// Type name output parameter
		/// </param>
		/// <param name='reqtype'>
		/// Request type output parameter
		/// </param>
		/// <param name='id'>
		/// ID of the entity
		/// </param>
		/// <param name='name'>
		/// Name of the entity
		/// </param>
		public static void RetrieveRequestInfo(RequestQueue.RequestRecord req, out string typename, out string reqtype, out string id, out string name)
        {
            typename = req.BodyTypeName;
            reqtype = "";
            id = "";
            name = "";
            switch (req.ReqType)
            {
                case RequestQueue.RequestRecord.RequestType.Delete:
                    reqtype = "Delete";
                    id = ((ClientEntity)req.Body).ID.ToString();
                    name = ((ClientEntity)req.Body).Name;
                    break;
                case RequestQueue.RequestRecord.RequestType.Insert:
                    reqtype = "Insert";
                    id = ((ClientEntity)req.Body).ID.ToString();
                    name = ((ClientEntity)req.Body).Name;
                    break;
                case RequestQueue.RequestRecord.RequestType.Update:
                    reqtype = "Update";
                    switch (req.BodyTypeName)
                    {
                        case "Tag":
                            name = ((List<Tag>)req.Body)[0].Name;
                            id = ((List<Tag>)req.Body)[0].ID.ToString();
                            break;
                        case "Item":
                            name = ((List<Item>)req.Body)[0].Name;
                            id = ((List<Item>)req.Body)[0].ID.ToString();
                            break;
                        case "Folder":
                            name = ((List<Folder>)req.Body)[0].Name;
                            id = ((List<Folder>)req.Body)[0].ID.ToString();
                            break;
                        default:
                            name = "(unrecognized entity)";
                            break;
                    }
                    break;
                default:
                    reqtype = "Unrecognized";
                    break;
            }
        }

        /// <summary>
        /// Helper method to optimize the queue
        /// </summary>
        /// <param name="newRecord"></param>
        /// <param name="requests"></param>
        private static void OptimizeQueue(RequestRecord newRecord, List<RequestRecord> requests)
        {
            // try to find a record for the same entity by the local ID
            try
            {
                var existingRecord = requests.Single(r => r.ID == newRecord.ID);
                existingRecord.DeserializeBody();
                UpdateExistingRecord(requests, existingRecord, newRecord);
            }
            catch (Exception)
            {
                // this is a new record so add the new record at the end
                requests.Add(newRecord);
            }

            // if this is a request to remove a folder, need to also remove all the manipulations to items inside that folder
            if (newRecord.ReqType == RequestRecord.RequestType.Delete &&
                newRecord.BodyTypeName == "Folder")
            {
                // create a folder that holds the item references to delete
                List<RequestRecord> deleteList = new List<RequestRecord>();
                // deserialize the bodies for all the items (need FolderID for all the items)
                foreach (var r in requests)
                {
                    if (r.BodyTypeName == "Item")
                    {
                        r.DeserializeBody();
                        Item t;
                        if (r.ReqType == RequestRecord.RequestType.Update)
                            t = ((List<Item>)r.Body)[0];
                        else
                            t = (Item)r.Body;
                        if (t.FolderID == newRecord.ID)
                            deleteList.Add(r);
                    }
                }
                foreach (var r in deleteList)
                    requests.Remove(r);
            }
        }
		
        /// <summary>
        /// Helper method to update an existing queue record with new information
        /// </summary>
        /// <param name="requests"></param>
        /// <param name="existingRecord"></param>
        /// <param name="newRecord"></param>
        /// <returns>Whether an update was performed</returns>
        private static bool UpdateExistingRecord(List<RequestRecord> requests, RequestRecord existingRecord, RequestRecord newRecord)
        {
            switch (existingRecord.ReqType)
            {
                case RequestRecord.RequestType.Delete:
                    // the existing record in the queue is a delete - this situation can't happen unless there is local corruption (but not necessarily queue corruption)
                    // there can be no further action
                    if (System.Diagnostics.Debugger.IsAttached)
                        throw new Exception("trying to do something to an already deleted entry");
                    return false;
                case RequestRecord.RequestType.Insert:
                    // the existing record in the queue is an insert
                    switch (newRecord.ReqType)
                    {
                        case RequestRecord.RequestType.Delete:
                            // the entity was created and deleted while offline
                            // the existing record needs to be removed 
                            requests.Remove(existingRecord);
                            return true;
                        case RequestRecord.RequestType.Insert:
                            // this doesn't make sense because it violates the Guid uniqueness principle
                            // since the record already exists, we're not going to insert a new one - take no action
                            if (System.Diagnostics.Debugger.IsAttached)
                                throw new Exception("insert after an insert");
                            return false;
                        case RequestRecord.RequestType.Update:
                            // an update on top of an insert
                            // replace the new value in the existing insert record with the "new" value in the update
                            switch (existingRecord.BodyTypeName)
                            {
                                case "Tag":
                                    List<Tag> newRecordTagList = (List<Tag>)newRecord.Body;
                                    existingRecord.Body = newRecordTagList[1];
                                    break;
                                case "Item":
                                    List<Item> newRecordFolder = (List<Item>)newRecord.Body;
                                    existingRecord.Body = newRecordFolder[1];
                                    break;
                                case "Folder":
                                    List<Folder> newRecordFolderList = (List<Folder>)newRecord.Body;
                                    existingRecord.Body = newRecordFolderList[1];
                                    break;
                            }
                            // reserialize the body
                            existingRecord.SerializeBody();
                            return true;
                    }
                    break;
                case RequestRecord.RequestType.Update:
                    // the existing record in the queue is an update
                    switch (newRecord.ReqType)
                    {
                        case RequestRecord.RequestType.Delete:
                            // the entity was updated and now deleted while offline
                            // the existing record needs to be amended to a delete record 
                            existingRecord.Copy(newRecord);
                            return true;
                        case RequestRecord.RequestType.Insert:
                            // this doesn't make sense because it violates the Guid uniqueness principle
                            // since the record already exists, we're not going to insert a new one - take no action
                            if (System.Diagnostics.Debugger.IsAttached)
                                throw new Exception("insert after an update");
                            return false;
                        case RequestRecord.RequestType.Update:
                            // an update on top of an update
                            // replace the new value in the existing record with the "new" new value
                            switch (existingRecord.BodyTypeName)
                            {
                                case "Tag":
                                    List<Tag> existingRecordTagList = (List<Tag>)existingRecord.Body;
                                    List<Tag> newRecordTagList = (List<Tag>)newRecord.Body;
                                    existingRecordTagList[1] = newRecordTagList[1];
                                    break;
                                case "Item":
                                    List<Item> existingRecordFolder = (List<Item>)existingRecord.Body;
                                    List<Item> newRecordFolder = (List<Item>)newRecord.Body;
                                    existingRecordFolder[1] = newRecordFolder[1];
                                    break;
                                case "Folder":
                                    List<Folder> existingRecordFolderList = (List<Folder>)existingRecord.Body;
                                    List<Folder> newRecordFolderList = (List<Folder>)newRecord.Body;
                                    existingRecordFolderList[1] = newRecordFolderList[1];
                                    break;
                            }
                            // reserialize the body
                            existingRecord.SerializeBody();
                            return true;
                    }
                    break;
            }

            // no case was triggered - this is an exceptional situation
            if (System.Diagnostics.Debugger.IsAttached)
                throw new Exception("queue corrupted");

            return false;
        }
    }
}
