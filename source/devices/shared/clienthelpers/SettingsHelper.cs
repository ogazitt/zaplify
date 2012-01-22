using System;
using System.Net;

namespace BuiltSteady.Zaplify.Devices.ClientHelpers
{
    public class SettingsHelper
    {

    }

    public class Settings
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ControlAttribute : Attribute
    {
        public ControlAttribute(string controlName)
        {
            ControlName = controlName;
        }

        public string ControlName { get; set; }
    }
}
