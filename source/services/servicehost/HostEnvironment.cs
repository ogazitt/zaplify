namespace BuiltSteady.Zaplify.ServiceHost
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;

    // avoid loading Azure assemblies unless running in Azure
    using Azure = Microsoft.WindowsAzure;

    public static class HostEnvironment
    {
        const string UserDataConnectionConfigKey = "Connection";
        const string UserAccountConnectionConfigKey = "Connection";
        const string AzureDiagnosticsConnectionString = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";

        static bool? isAzure;               // true for either Azure or DevFabric
        static bool? isAzureDevFabric;      // only true in DevFabric
        static string userDataConnection;
        static string userAccountConnection;

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool IsAzureEnvironmentAvailable()
        {
            return Azure.ServiceRuntime.RoleEnvironment.IsAvailable;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool IsAzureDevFabricConfigured()
        {   // inspect diagnostics storage setting to determine if using DevFabric 
            string diagnosticsSetting = ConfigurationSettings.Get(AzureDiagnosticsConnectionString);
            return diagnosticsSetting.StartsWith("UseDevelopmentStorage", StringComparison.OrdinalIgnoreCase);
        }


#region // Azure property helpers
        // should only be invoked from within a codepath that has verified IsAzure is true
        public static string AzureRoleName
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                return Azure.ServiceRuntime.RoleEnvironment.CurrentRoleInstance.Role.Name;
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetLocalResourceRootPath(string resourceName)
        {
            return Azure.ServiceRuntime.RoleEnvironment.GetLocalResource(resourceName).RootPath;
        }

#endregion

    }
}
