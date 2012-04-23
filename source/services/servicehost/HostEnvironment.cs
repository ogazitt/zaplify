namespace BuiltSteady.Zaplify.ServiceHost
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;

    // avoid loading Azure assemblies unless running in Azure
    using Azure = Microsoft.WindowsAzure;
    using System.Globalization;

    public static class HostEnvironment
    {
        const string UserDataConnectionConfigKey = "UsersConnection";
        const string UserAccountConnectionConfigKey = "UsersConnection";
        const string SuggestionsConnectionConfigKey = "SuggestionsConnection";
        const string AzureDiagnosticsConnectionString = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";
        const string UserDatabaseVersionConfigKey = "UserDatabaseVersion";
        const string SuggestionsDatabaseVersionConfigKey = "SuggestionsDatabaseVersion";

        static bool? isAzure;               // true for either Azure or DevFabric
        static bool? isAzureDevFabric;      // only true in DevFabric
        static string userDataConnection;
        static string userAccountConnection;
        static string suggestionsConnection;
        static string userDatabaseVersion;
        static string suggestionsDatabaseVersion;

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

        public static string UserDatabaseVersion
        {
            get
            {
                if (userDatabaseVersion == null)
                {
                    userDatabaseVersion = ConfigurationSettings.Get(UserDatabaseVersionConfigKey);
                }
                return userDatabaseVersion;
            }
        }

        public static string SuggestionsDatabaseVersion
        {
            get
            {
                if (suggestionsDatabaseVersion == null)
                {
                    suggestionsDatabaseVersion = ConfigurationSettings.Get(SuggestionsDatabaseVersionConfigKey);
                }
                return suggestionsDatabaseVersion;
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
