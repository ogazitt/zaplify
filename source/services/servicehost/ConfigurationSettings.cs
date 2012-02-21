namespace BuiltSteady.Zaplify.ServiceHost
{
    using System.Configuration;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    //using System.Web.Configuration;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public interface IConfigurationSettings
    {
        string Get(string name);
        string GetConnection(string name);
    }

    // abstracts configuration settings for Web vs. Azure
    public static class ConfigurationSettings
    {
        static IConfigurationSettings configSettings = HostEnvironment.IsAzure ? 
            (IConfigurationSettings)(new AzureConfigurationSettings()) : 
            (IConfigurationSettings)(new WebConfigurationSettings());

        public static string Get(string name)
        {
            return configSettings.Get(name);
        }

        public static string GetConnection(string name)
        {
            return configSettings.GetConnection(name);
        }

        public static int GetAsInt(string name)
        {
            return int.Parse(Get(name), CultureInfo.InvariantCulture);
        }

        public static int? GetAsNullableInt(string name)
        {
            string value = Get(name);
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            return int.Parse(value, CultureInfo.InvariantCulture);
        }

    }

    class WebConfigurationSettings : IConfigurationSettings
    {   // retrieve settings from .config file (web.config or app.config)
        public string Get(string name)
        {   
            return ConfigurationManager.AppSettings[name];
        }

        public string GetConnection(string name)
        {   // use app setting to reference connection setting
            string connectionName = ConfigurationManager.AppSettings[name];
            return ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;
        }

    }

    class AzureConfigurationSettings : IConfigurationSettings
    {
        public string Get(string name)
        {
            return GetAzureConfigurationSetting(name);
        }

        public string GetConnection(string name)
        {
            string connectionName = GetAzureConfigurationSetting(name);
            return GetAzureConfigurationSetting(connectionName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static string GetAzureConfigurationSetting(string name)
        {   // delay loading Azure assemblies
            return RoleEnvironment.GetConfigurationSettingValue(name);
        }

    }
}
