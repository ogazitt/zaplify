namespace BuiltSteady.Zaplify.ServiceHost
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Web;
    // avoid loading Azure assemblies unless running in Azure
    using Azure = Microsoft.WindowsAzure;

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.Shared.Entities;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public static class HostEnvironment
    {
        public const string AzureStorageAccountConfigKey = "AzureStorageAccount";
        public const string MailWorkerTimeoutConfigKey = "MailWorkerTimeout";
        public const string MailWorkerCountConfigKey = "MailWorkerCount";
        public const string SpeechWorkerTimeoutConfigKey = "SpeechWorkerTimeout";
        public const string SpeechWorkerCountConfigKey = "SpeechWorkerCount";
        public const string WorkflowWorkerTimeoutConfigKey = "WorkflowWorkerTimeout";
        public const string WorkflowWorkerCountConfigKey = "WorkflowWorkerCount";
        const string DeploymentNameConfigKey = "DeploymentName";
        const string UserDataConnectionConfigKey = "UsersConnection";
        const string UserAccountConnectionConfigKey = "UsersConnection";
        const string SuggestionsConnectionConfigKey = "SuggestionsConnection";
        const string DataServicesConnectionConfigKey = "DataServicesConnection";
        const string DataServicesEndpointConfigKey = "DataServicesEndpoint";
        const string AzureDiagnosticsConnectionString = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";
        const string AzureLoggingEnabledConfigKey = "AzureLoggingEnabled";
        const string SplunkLoggingEnabledConfigKey = "SplunkLoggingEnabled";
        const string SplunkServerEndpointConfigKey = "SplunkServerEndpoint";
        const string SplunkLocalPortConfigKey = "SplunkLocalPort";
        const string TraceFolderConfigKey = "TraceFolder";
        const string SpeechEndpointConfigKey = "SpeechEndpoint";

        static bool? isAzure;               // true for either Azure or DevFabric
        static bool? isAzureDevFabric;      // only true in DevFabric
        static bool? isAzureLoggingEnabled;
        static bool? isSplunkLoggingEnabled;
        static int?  splunkLocalPort;
        static RoleSize? azureRoleSize;
        static string deploymentName;
        static string userDataConnection;
        static string userAccountConnection;
        static string suggestionsConnection;
        static string dataServicesConnection;
        static string dataServicesEndpoint;
        static string lexiconFileName;
        static string traceDirectoryName;
        static string splunkServerEndpoint;

        public static bool IsAzure
        {   // running in an Azure environment
            get
            {
                if (!isAzure.HasValue)
                {
                    try
                    {
                        isAzure = IsAzureEnvironmentAvailable();
                    }
                    catch (FileNotFoundException)
                    {
                        isAzure = false;
                    }
                    catch (TypeInitializationException)
                    {
                        isAzure = false;
                    }
                }
                return isAzure.Value;
            }
        }

        public static bool IsAzureDevFabric
        {   // running in Azure DevFabric
            get
            {
                if (!isAzureDevFabric.HasValue)
                {
                    isAzureDevFabric = IsAzure && IsAzureDevFabricConfigured();
                }
                return isAzureDevFabric.Value;
            }
        }

        public static bool IsAzureLoggingEnabled
        {
            get
            {
                if (!isAzureLoggingEnabled.HasValue)
                {
                    int? value = ConfigurationSettings.GetAsNullableInt(AzureLoggingEnabledConfigKey);
                    isAzureLoggingEnabled = (value != null && (int)value > 0);
                }
                return isAzureLoggingEnabled.Value;
            }
        }

        public static bool IsSplunkLoggingEnabled
        {
            get
            {
                if (!isSplunkLoggingEnabled.HasValue)
                {
                    int? value = ConfigurationSettings.GetAsNullableInt(SplunkLoggingEnabledConfigKey);
                    isSplunkLoggingEnabled = (value != null && (int)value > 0);
                }
                return isSplunkLoggingEnabled.Value;
            }
        }

        public static int SplunkLocalPort
        {
            get
            {
                if (!splunkLocalPort.HasValue)
                {
                    splunkLocalPort = ConfigurationSettings.GetAsNullableInt(SplunkLocalPortConfigKey) ?? 9237;
                }
                return splunkLocalPort.Value;
            }
        }

        static string versionCheckStatus = null;
        public static bool DataVersionCheck(out string status)
        {
            if (versionCheckStatus == null)
            {
                versionCheckStatus = string.Empty;
                UserStorageContext userStorage = Storage.NewUserContext;
                var dbSchemaVersion = userStorage.Versions.FirstOrDefault(v => v.VersionType == DatabaseVersion.Schema);
                var dbConstantsVersion = userStorage.Versions.FirstOrDefault(v => v.VersionType == DatabaseVersion.Constants);
                if (dbSchemaVersion == null || dbSchemaVersion.VersionString != UserConstants.SchemaVersion)
                {
                    versionCheckStatus += string.Format("UserSchema version mismatch: Code='{0}' Database='{1}' <br/>", 
                        UserConstants.SchemaVersion, 
                        dbSchemaVersion == null ? "<none>" : dbSchemaVersion.VersionString);
                }
                if (dbConstantsVersion == null || dbConstantsVersion.VersionString != UserConstants.ConstantsVersion)
                {
                    versionCheckStatus += string.Format("UserConstants version mismatch: Code='{0}' Database='{1}' <br/>", 
                        UserConstants.ConstantsVersion, 
                        dbConstantsVersion == null ? "<none>" : dbConstantsVersion.VersionString);
                }

                SuggestionsStorageContext workflowStorage = Storage.NewSuggestionsContext;
                dbSchemaVersion = workflowStorage.Versions.FirstOrDefault(v => v.VersionType == DatabaseVersion.Schema);
                dbConstantsVersion = workflowStorage.Versions.FirstOrDefault(v => v.VersionType == DatabaseVersion.Constants);
                if (dbSchemaVersion == null || dbSchemaVersion.VersionString != WorkflowConstants.SchemaVersion)
                {
                    versionCheckStatus += string.Format("WorkflowSchema version mismatch: Code='{0}' Database='{1}' <br/>", 
                        WorkflowConstants.SchemaVersion,
                        dbSchemaVersion == null ? "<none>" : dbSchemaVersion.VersionString);
                }
                if (dbConstantsVersion == null || dbConstantsVersion.VersionString != WorkflowConstants.ConstantsVersion)
                {
                    versionCheckStatus += string.Format("WorkflowConstants version mismatch: Code='{0}' Database='{1}' <br/>", 
                        WorkflowConstants.ConstantsVersion, 
                        dbConstantsVersion == null ? "<none>" : dbConstantsVersion.VersionString);
                }
            }
            status = versionCheckStatus;
            return (versionCheckStatus.Length == 0);
        }

        public static string DeploymentName
        {
            get
            {
                if (deploymentName == null)
                {
                    deploymentName = ConfigurationSettings.Get(DeploymentNameConfigKey);
                }
                return deploymentName;
            }
        }

        public static string UserDataConnection
        {
            get
            {
                if (userDataConnection == null)
                {
                    userDataConnection = ConfigurationSettings.GetConnection(UserDataConnectionConfigKey);
                }
                return userDataConnection;
            }
        }

        public static string UserAccountConnection
        {
            get
            {
                if (userAccountConnection == null)
                {
                    userAccountConnection = ConfigurationSettings.GetConnection(UserAccountConnectionConfigKey);
                }
                return userAccountConnection;
            }
        }

        public static string SuggestionsConnection
        {
            get
            {
                if (suggestionsConnection == null)
                {
                    suggestionsConnection = ConfigurationSettings.GetConnection(SuggestionsConnectionConfigKey);
                }
                return suggestionsConnection;
            }
        }

        public static string DataServicesConnection
        {
            get
            {
                if (dataServicesConnection == null)
                {
                    dataServicesConnection = ConfigurationSettings.GetConnection(DataServicesConnectionConfigKey);
                }
                return dataServicesConnection;
            }
        }

        public static string DataServicesEndpoint
        {
            get
            {
                if (dataServicesEndpoint == null)
                {
                    dataServicesEndpoint = ConfigurationSettings.Get(DataServicesEndpointConfigKey);
                    if (string.IsNullOrEmpty(dataServicesEndpoint))
                    {   // use local hostname if not defined in configuration
                        if (HttpContext.Current != null && HttpContext.Current.Request != null)
                        {
                            Uri requestUri = HttpContext.Current.Request.Url;
                            if (requestUri != null)
                            {
                                dataServicesEndpoint = String.Format("{0}://{1}/", requestUri.Scheme, requestUri.Authority);
                            }
                        }
                    }
                }
                return dataServicesEndpoint;
            }
        }

        public static string SplunkServerEndpoint
        {
            get
            {
                if (splunkServerEndpoint == null)
                {
                    splunkServerEndpoint = ConfigurationSettings.Get(SplunkServerEndpointConfigKey);
                }
                return splunkServerEndpoint;
            }
        }

        public static string LexiconFileName
        {
            get
            {
                if (lexiconFileName == null)
                {
                    if (IsAzure && !IsAzureDevFabric)
                    {
                        // Azure (deployed)
                        if (HttpContext.Current != null)
                        {
                            // web role
                            string driveLetter = HttpContext.Current.Server.MapPath(@"~").Substring(0, 1);
                            lexiconFileName = driveLetter + @":\approot\bin\nlp\lex.dat";
                        }
                        else
                        {
                            // worker role
                            lexiconFileName = @"\approot\nlp\lex.dat";
                        }
                    }
                    else
                    {
                        // local (either dev fabric or cassini)
                        if (HttpContext.Current != null)
                        {
                            // web role - azure dev fabric OR cassini
                            lexiconFileName = HttpContext.Current.Server.MapPath(@"bin\nlp\lex.dat");
                        }
                        else
                        {
                            // azure worker role (dev fabric)
                            lexiconFileName = @"nlp\lex.dat";
                        }
                    }
                }
                return lexiconFileName;
            }
        }

        public static string TraceDirectory
        {
            get
            {
                if (traceDirectoryName == null)
                {
                    traceDirectoryName = GetLocalResourceRootPath(TraceFolderConfigKey);
                }
                return traceDirectoryName;
            }
        }

        public static string SpeechEndpoint
        {
            get
            {
                if (IsAzure)
                {
                    RoleInstanceEndpoint externalEndPoint =
                        RoleEnvironment.CurrentRoleInstance.InstanceEndpoints[SpeechEndpointConfigKey];
                    string endpoint = String.Format(
                        "http://{0}", externalEndPoint.IPEndpoint);
                    return endpoint;
                }
                else
                    return "http://localhost:8080";
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool IsAzureEnvironmentAvailable()
        {
            return Azure.ServiceRuntime.RoleEnvironment.IsAvailable;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool IsAzureDevFabricConfigured()
        {   // inspect deployment id to determine if using DevFabric 
            return AzureDeploymentId.StartsWith("deployment", true, CultureInfo.InvariantCulture);
        }


#region // Azure property helpers
        // should only be invoked from within a codepath that has verified IsAzure is true
        public const string Website = "Website";
        public const string WorkerRole = "WorkerRole";
        public static string AzureRoleName
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                if (IsAzure)
                    return Azure.ServiceRuntime.RoleEnvironment.CurrentRoleInstance.Role.Name;
                else
                    return Website;
            }
        }

        public static string AzureDeploymentId
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                return Azure.ServiceRuntime.RoleEnvironment.DeploymentId;
            }
        }

        public static string AzureRoleId
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                return Azure.ServiceRuntime.RoleEnvironment.CurrentRoleInstance.Id;
            }
        }

        public enum RoleSize
        {
            ExtraSmall,
            Small,
            Medium,
            Large,
            ExtraLarge,
            Unknown
        };

        public static RoleSize AzureRoleSize
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                if (!azureRoleSize.HasValue)
                {
                    switch (Environment.ProcessorCount)
                    {
                        case 1:
                            var totalRamInMB = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1024 / 1024;
                            if (totalRamInMB < 1024)
                                azureRoleSize = RoleSize.ExtraSmall;
                            else
                                azureRoleSize = RoleSize.Small;
                            break;
                        case 2:
                            azureRoleSize = RoleSize.Medium;
                            break;
                        case 4:
                            azureRoleSize = RoleSize.Large;
                            break;
                        case 8:
                            azureRoleSize = RoleSize.ExtraSmall;
                            break;
                        default:
                            azureRoleSize = RoleSize.Unknown;
                            break;
                    }
                }
                return azureRoleSize.Value;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetLocalResourceRootPath(string resourceName)
        {
            return Azure.ServiceRuntime.RoleEnvironment.GetLocalResource(resourceName).RootPath;
        }

#endregion

    }
}
