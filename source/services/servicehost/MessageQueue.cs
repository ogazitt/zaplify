namespace BuiltSteady.Zaplify.ServiceHost
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    public class MessageQueue
    {
        const string queueName = "queue";
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
                    storageAccount = CloudStorageAccount.FromConfigurationSetting("DataConnectionString");
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

        public static void DeleteMessage(object message)
        {
            Queue.DeleteMessage((CloudQueueMessage)message);
        }

        public static MQMessage<T> DequeueMessage<T>()
        {
            var msg = Queue.GetMessage();
            if (msg == null)  // GetMessage doesn't block for a message
                return null;

            byte[] bytes = msg.AsBytes;
            var ms = new MemoryStream(bytes);
            DataContractJsonSerializer dcs = new DataContractJsonSerializer(typeof(T));
            T content = (T)dcs.ReadObject(ms);
            MQMessage<T> returnMessage = new MQMessage<T>() { Content = content, MessageRef = msg };
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
        }

        public static void Initialize()
        {
            // create the queue if it doesn't yet exist
            // this call returns false if the queue was already created 
            if (Queue.CreateIfNotExist())
            {
                TraceLog.TraceInfo(String.Format("MessageQueue.Initialize: created queue named '{0}'", queueName));
            }
        }
    }

    public class MQMessage<T>
    {
        public T Content { get; set; }
        public object MessageRef { get; set; }
    }
}
