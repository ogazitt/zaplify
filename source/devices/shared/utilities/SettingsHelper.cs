using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace TaskStoreWinPhoneUtilities
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
