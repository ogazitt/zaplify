namespace BuiltSteady.Zaplify.ServiceHost
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Web;
    // avoid loading Azure assemblies unless running in Azure
    using Azure = Microsoft.WindowsAzure;

    public static class HostEnvironment
    {
        public const string AzureStorageAccountConfigKey = "AzureStorageAccount";
        const string UserDataConnectionConfigKey = "UsersConnection";
        const string UserAccountConnectionConfigKey = "UsersConnection";
        const string SuggestionsConnectionConfigKey = "SuggestionsConnection";
        const string DataServicesConnectionConfigKey = "DataServicesConnection";
        const string DataServicesEndpointConfigKey = "DataServicesEndpoint";
        const string AzureDiagnosticsConnectionString = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";

        static bool? isAzure;               // true for either Azure or DevFabric
        static bool? isAzureDevFabric;      // only true in DevFabric
        static string userDataConnection;
        static string userAccountConnection;
        static string suggestionsConnection;
        static string dataServicesConnection;
        static string dataServicesEndpoint;
        static string lexiconFileName;

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
