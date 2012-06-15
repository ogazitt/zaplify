namespace BuiltSteady.Zaplify.ServiceHost
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    public class MessageQueue
    {
        static string queueName = MakeQueueName();
        static CloudStorageAccount storageAccount;
        static CloudQueueClient queueClient;
        static CloudQueue queue;
        private static CloudQueueClient QueueClient
        {
            get
            {
                if (queueClient == null)
                    queueClient = StorageAccount.CreateCloudQueueClient();
                return queueClient;
            }
        }
        private static CloudStorageAccount StorageAccount
        {
            get
            {
                if (storageAccount == null)
                {
                    // set up config publisher
                    CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
                    {
                        configSetter(Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment.GetConfigurationSettingValue(configName));
                    });
                    // get the connection string from config
                    storageAccount = CloudStorageAccount.FromConfigurationSetting(HostEnvironment.AzureStorageAccountConfigKey);
                }
                return storageAccount;
            }
        }
        private static CloudQueue Queue
        {
            get
            {
                if (queue == null)
                    queue = QueueClient.GetQueueReference(queueName);
                return queue;
            }
        }

        public static bool DeleteMessage(object message)
        {
            var msg = message as CloudQueueMessage;
            if (msg == null)
            {
                TraceLog.TraceError("Wrong message type: " + message.GetType().Name);
                return false;
            }

            try
            {
                TraceLog.TraceInfo("Deleting message ID " + msg.Id);
                Queue.DeleteMessage(msg);
                return true;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("DeleteMessage failed", ex);
                return false;
            }
        }

        public static MQMessage<T> DequeueMessage<T>()
        {
            var msg = Queue.GetMessage(TimeSpan.FromMinutes(1.0d));
            if (msg == null)  // GetMessage doesn't block for a message
                return null;

            TraceLog.TraceDetail(String.Format("Dequeued message ID {0} inserted {1}", msg.Id, msg.InsertionTime.ToString()));
            byte[] bytes = msg.AsBytes;
            var ms = new MemoryStream(bytes);
            DataContractJsonSerializer dcs = new DataContractJsonSerializer(typeof(T));
            T content = (T)dcs.ReadObject(ms);
            MQMessage<T> returnMessage = new MQMessage<T>() { Content = content, MessageRef = msg };
            TraceLog.TraceInfo(String.Format("Dequeued a {0}: {1}", content.GetType().Name, content.ToString()));
            
            return returnMessage;
        }

        public static void EnqueueMessage(object obj)
        {
            DataContractJsonSerializer dcs = new DataContractJsonSerializer(obj.GetType());
            var ms = new MemoryStream();
            dcs.WriteObject(ms, obj);
            ms.Position = 0;
            byte[] bytes = new byte[ms.Length];
            int len = ms.Read(bytes, 0, (int) ms.Length);  // messages can only be 8K so the cast is safe
            if (len < ms.Length)
            {
                var newbytes = new byte[len];
                Array.Copy(bytes, newbytes, len);
                bytes = newbytes;
            }

            var msg = new CloudQueueMessage(bytes);
            Queue.AddMessage(msg);
            TraceLog.TraceInfo(String.Format("Enqueued a {0}: {1}", obj.GetType().Name, obj.ToString()));
        }

        public static void Initialize()
        {
            // create the queue if it doesn't yet exist
            // this call returns false if the queue was already created 
            if (Queue.CreateIfNotExist())
                TraceLog.TraceInfo(String.Format("Created queue named '{0}'", queueName));
            else
                TraceLog.TraceDetail(String.Format("Queue named '{0}' already exists", queueName));
        }

        public static MQMessage<T> PeekMessage<T>()
        {
            var msg = Queue.PeekMessage();
            if (msg == null)  // PeekMessage doesn't block for a message
                return null;

            TraceLog.TraceDetail(String.Format("Found message ID {0} inserted {1}", msg.Id, msg.InsertionTime.ToString()));
            byte[] bytes = msg.AsBytes;
            var ms = new MemoryStream(bytes);
            DataContractJsonSerializer dcs = new DataContractJsonSerializer(typeof(T));
            T content = (T)dcs.ReadObject(ms);
            MQMessage<T> returnMessage = new MQMessage<T>() { Content = content, MessageRef = msg };
            TraceLog.TraceInfo(String.Format("Message content is a {0}: {1}", content.GetType().Name, content.ToString()));

            return returnMessage;
        }

        #region Helpers

        private static string MakeQueueName()
        {
            string deploymentID = HostEnvironment.AzureDeploymentId;
            deploymentID = deploymentID.Replace("(", "");
            deploymentID = deploymentID.Replace(")", "");            
            string queueName = "queue-" + deploymentID;

            // add a machine identifier in case this is a dev fabric, to avoid conflicts
            if (HostEnvironment.IsAzureDevFabric)
                queueName += "-" + System.Environment.MachineName.ToLower();
            return queueName;
        }

        #endregion
    }

    public class MQMessage<T>
    {
        public T Content { get; set; }
        public object MessageRef { get; set; }
    }
}
