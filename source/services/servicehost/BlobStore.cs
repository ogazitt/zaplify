namespace BuiltSteady.Zaplify.ServiceHost
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    public class BlobStore
    {
        private const string TraceContainerName = "client-traces";

        static CloudStorageAccount storageAccount;
        static CloudBlobClient blobClient;
        static CloudBlobContainer traceContainer;

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
                    storageAccount = CloudStorageAccount.FromConfigurationSetting("DataConnectionString");
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

        public static void WriteTraceFile(string filename, Stream stream)
        {
            try
            {
                bool created = TraceContainer.CreateIfNotExist();
                if (created == true)
                    TraceLog.TraceInfo("BlobStore.AddTraceFile: created container " + TraceContainerName);

                var blob = TraceContainer.GetBlobReference(filename);
                blob.UploadFromStream(stream);
                TraceLog.TraceInfo(String.Format("BlobStore.AddTraceFile: uploaded trace file {0}", filename));
            }
            catch (Exception ex)
            {
                TraceLog.TraceException(String.Format("BlobStore.AddTraceFile: failed to upload trace file {0}", filename), ex);
            }
        }
    }
}
