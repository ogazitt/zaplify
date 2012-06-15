namespace BuiltSteady.Zaplify.ServiceHost
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    public class BlobStore
    {
        public const string TraceContainerName = "client-traces";
        public const string GroceryContainerName = "grocery-data";

        static CloudStorageAccount storageAccount;
        static CloudBlobClient blobClient;
        static CloudBlobContainer traceContainer;
        static CloudBlobContainer groceryContainer;

        private static CloudBlobClient BlobClient
        {
            get
            {
                if (blobClient == null)
                    blobClient = StorageAccount.CreateCloudBlobClient();
                return blobClient;
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
        
        private static CloudBlobContainer TraceContainer
        {
            get
            {
                if (traceContainer == null)
                    traceContainer = BlobClient.GetContainerReference(TraceContainerName);
                return traceContainer;
            }
        }

        private static CloudBlobContainer GroceryContainer
        {
            get
            {
                if (groceryContainer == null)
                    groceryContainer = BlobClient.GetContainerReference(GroceryContainerName);
                return traceContainer;
            }
        }

        public static void WriteTraceFile(string filename, Stream stream)
        {
            try
            {
                bool created = TraceContainer.CreateIfNotExist();
                if (created == true)
                    TraceLog.TraceInfo("Created container " + TraceContainerName);

                var blob = TraceContainer.GetBlobReference(filename);
                blob.UploadFromStream(stream);
                TraceLog.TraceInfo(String.Format("Uploaded trace file {0}", filename));
            }
            catch (Exception ex)
            {
                TraceLog.TraceException(String.Format("Failed to upload trace file {0}", filename), ex);
            }
        }

        public static void WriteBlobData(string containerName, string filename, string data)
        {
            try
            {
                var container = BlobClient.GetContainerReference(containerName);
                bool created = container.CreateIfNotExist();
                if (created == true)
                    TraceLog.TraceInfo("Created container " + containerName);

                var blob = container.GetBlobReference(filename);
                blob.UploadText(data);
                TraceLog.TraceInfo(String.Format("Added file {0} to container {1}", filename, containerName));
            }
            catch (Exception ex)
            {
                TraceLog.TraceException(String.Format("Failed to add file {0} to container {1}", filename, containerName), ex);
            }
        }
    }
}
