using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.Devices.ClientHelpers
{
    public class PhoneSetting
    {
        public class NameValuePair
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }
        public string Name { get; set; }
        public string DisplayTemplate { get; set; }
        public List<NameValuePair> Values { get; set; }
    }
    
    public class PhoneTheme
    {
        public string PageBackground { get; set; }
        public string TableBackground { get; set; }
        public string TableSeparatorBackground { get; set; }
    }

    public class PhoneSettings
    {
        public const string Theme = "Theme";
        public const string HomeTab = "Home Tab";

        public static Dictionary<string, PhoneSetting> Settings = new Dictionary<string, PhoneSetting>()
        {
            { 
                Theme, new PhoneSetting()
                {
                    Name = Theme,
                    DisplayTemplate = "LargeRainbowTemplate",
                    Values = new List<PhoneSetting.NameValuePair>()
                    {
#if IOS
                        new PhoneSetting.NameValuePair() { Name = PhoneThemes.Default, Value = new PhoneTheme { PageBackground = "Images/background.png", TableBackground = "White", TableSeparatorBackground = "#ffe0e0e0" } },
                        new PhoneSetting.NameValuePair() { Name = PhoneThemes.Pink, Value = new PhoneTheme { PageBackground = "#ffddff", TableBackground = "#ffddff", TableSeparatorBackground = "#ffb0b0b0" } },
                        new PhoneSetting.NameValuePair() { Name = PhoneThemes.Blue, Value = new PhoneTheme { PageBackground = "#ddddff", TableBackground = "#ddddff", TableSeparatorBackground = "#ffb0b0b0" } },
#else
                        new PhoneSetting.NameValuePair() { Name = PhoneThemes.Black, Value = "Black" },
                        new PhoneSetting.NameValuePair() { Name = PhoneThemes.Cyan, Value = "DarkCyan" },
                        new PhoneSetting.NameValuePair() { Name = PhoneThemes.Magenta, Value = "DarkMagenta" },
                        new PhoneSetting.NameValuePair() { Name = PhoneThemes.Navy, Value = "Navy" },
                        new PhoneSetting.NameValuePair() { Name = PhoneThemes.Olive, Value = "DarkOliveGreen" },
                        new PhoneSetting.NameValuePair() { Name = PhoneThemes.Red, Value = "DarkRed" },
                        new PhoneSetting.NameValuePair() { Name = PhoneThemes.Slate, Value = "DarkSlateBlue" },
#endif
                    }
                }
            },
            { 
                HomeTab, new PhoneSetting()
                {
                    Name = HomeTab,
                    DisplayTemplate = "FullListPickerTemplate",
                    Values = new List<PhoneSetting.NameValuePair>()
                    {
                        new PhoneSetting.NameValuePair() { Name = PhoneTabs.Add, Value = "Add" },
                        new PhoneSetting.NameValuePair() { Name = PhoneTabs.Schedule, Value = "Schedule" },
                        new PhoneSetting.NameValuePair() { Name = PhoneTabs.Folders, Value = "Folders" },
                    }
                }
            },
        };
    }

    public class PhoneThemes
    {
#if IOS
        public const string Default = "Default";
        public const string Pink = "Pink";
        public const string Blue = "Blue";
#else
        public const string Black = "Black";
        public const string Cyan = "Cyan";
        public const string Magenta = "Magenta";
        public const string Navy = "Navy";
        public const string Olive = "Olive";
        public const string Red = "Red";
        public const string Slate = "Slate";
#endif
    }

    public class PhoneTabs
    {
        public const string Add = "Add";
        public const string Schedule = "Schedule";
        public const string Folders = "Folders";
    }

    public class PhoneSettingsHelper
    {
        public static string GetPhoneSetting(Folder phoneClient, string setting)
        {
            if (phoneClient == null || setting == null)
                return null;

            var phoneSettingsItem = GetPhoneSettingsItem(phoneClient);
            var settings = phoneSettingsItem.GetFieldValue(FieldNames.Value);
            if (settings != null && !String.IsNullOrEmpty(settings.Value))
            {
                var jsonSettings = JObject.Parse(settings.Value);
                return (string)jsonSettings[setting];
            }
            else
                return null;
        }

        public static void StorePhoneSetting(Folder phoneClient, string setting, string value)
        {
            if (phoneClient == null)
                return;

            var phoneSettingsItem = GetPhoneSettingsItem(phoneClient);
            var settings = phoneSettingsItem.GetFieldValue(FieldNames.Value, true);
            JObject jsonSettings = null;
            if (settings != null && !String.IsNullOrEmpty(settings.Value))
                jsonSettings = JObject.Parse(settings.Value);
            else
                jsonSettings = new JObject();
            jsonSettings[setting] = value;
            settings.Value = jsonSettings.ToString();

            // store the phone client folder
            StorageHelper.WritePhoneClient(phoneClient);
        }

        public static Item GetPhoneSettingsItem(Folder phoneClient)
        {
            if (phoneClient == null)
                return null;

            // get the list of phone settings
            Item phoneSettings = null;
            if (phoneClient.Items.Any(i => i.Name == SystemEntities.PhoneSettings))
                phoneSettings = phoneClient.Items.Single(i => i.Name == SystemEntities.PhoneSettings);
            else
            {
                DateTime now = DateTime.UtcNow;
                phoneSettings = new Item()
                {
                    Name = SystemEntities.PhoneSettings,
                    FolderID = phoneClient.ID,
                    ItemTypeID = SystemItemTypes.NameValue,
                    FieldValues = new ObservableCollection<FieldValue>(),
                    Created = now,
                    LastModified = now
                };
                phoneClient.Items.Add(phoneSettings);

                // store the phone client folder
                StorageHelper.WritePhoneClient(phoneClient);

                // queue up a server request
                if (phoneClient.ID != Guid.Empty)
                {
                    RequestQueue.EnqueueRequestRecord(RequestQueue.SystemQueue, new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                        Body = phoneSettings,
                        ID = phoneSettings.ID,
                        IsDefaultObject = true
                    });
                }
            }

            return phoneSettings;
        }

        public static string GetHomeTab(Folder phoneClient)
        {
            return GetPhoneSetting(phoneClient, PhoneSettings.HomeTab);
        }

        public static void StoreHomeTab(Folder phoneClient, string value)
        {
            StorePhoneSetting(phoneClient, PhoneSettings.HomeTab, value);
        }

        public static string GetTheme(Folder phoneClient)
        {
            return GetPhoneSetting(phoneClient, PhoneSettings.Theme);
        }

        public static void StoreTheme(Folder phoneClient, string value)
        {
            StorePhoneSetting(phoneClient, PhoneSettings.Theme, value);
        }
    }
}
