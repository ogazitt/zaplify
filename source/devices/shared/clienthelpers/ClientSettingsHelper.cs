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

    public class ClientSettingsHelper
    {
        public static string GetListSortOrder(Folder clientSettings, ClientEntity list)
        {
            if (clientSettings == null)
                return null;

            var sortOrders = GetListSortOrders(clientSettings);
            var listID = list.ID.ToString();
            string listSortOrder = null;
            if (clientSettings.Items.Any(i =>
                i.ParentID == sortOrders.ID &&
                i.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == listID)))
            {
                var sortOrderItem = clientSettings.Items.Single(i =>
                    i.ParentID == sortOrders.ID &&
                    i.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == listID));
                var sortby = sortOrderItem.FieldValues.Single(fv => fv.FieldName == FieldNames.SortBy);
                listSortOrder = (sortby != null) ? sortby.Value : null;
            }
            return listSortOrder;
        }

        public static void StoreListSortOrder(Folder clientSettings, ClientEntity list, string listSortOrder)
        {
            if (clientSettings == null)
                return;

            var sortOrders = GetListSortOrders(clientSettings);
            var listID = list.ID.ToString();
            if (clientSettings.Items.Any(i =>
                i.ParentID == sortOrders.ID &&
                i.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == listID)))
            {
                var sortOrderItem = clientSettings.Items.Single(i =>
                    i.ParentID == sortOrders.ID &&
                    i.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == listID));
                Item copy = new Item(sortOrderItem);
                var sortby = sortOrderItem.FieldValues.Single(fv => fv.FieldName == FieldNames.SortBy);
                sortby.Value = listSortOrder;

                // queue up a server request
                RequestQueue.EnqueueRequestRecord(new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Update,
                    Body = new List<Item>() { copy, sortOrderItem },
                    BodyTypeName = "Item",
                    ID = sortOrderItem.ID
                });
            }
            else
            {
                Guid id = Guid.NewGuid();
                DateTime now = DateTime.UtcNow;
                var sortOrderItem = new Item()
                {
                    ID = id,
                    Name = list.Name,
                    ItemTypeID = SystemItemTypes.Reference,
                    FolderID = clientSettings.ID,
                    ParentID = sortOrders.ID,
                    Created = now,
                    LastModified = now,
                    FieldValues = new ObservableCollection<FieldValue>()
                    {
                        new FieldValue()
                        {
                            ItemID = id,
                            FieldName = FieldNames.EntityRef,
                            Value = list.ID.ToString(),
                        },
                        new FieldValue()
                        {
                            ItemID = id,
                            FieldName = FieldNames.EntityType,
                            Value = list.GetType().Name,
                        },
                        new FieldValue()
                        {
                            ItemID = id,
                            FieldName = FieldNames.SortBy,
                            Value = listSortOrder
                        }
                    }
                };
                clientSettings.Items.Add(sortOrderItem);

                // queue up a server request
                RequestQueue.EnqueueRequestRecord(new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                    Body = sortOrderItem,
                    ID = sortOrderItem.ID
                });
            }

            // store the client settings
            StorageHelper.WriteClientSettings(clientSettings);
        }

        public static string GetPhoneSetting(Folder clientSettings, string setting)
        {
            if (clientSettings == null || setting == null)
                return null;

            var phoneSettingsItem = GetPhoneSettingsItem(clientSettings);
            var settings = phoneSettingsItem.GetFieldValue(FieldNames.Value);
            if (settings != null && !String.IsNullOrEmpty(settings.Value))
            {
                var jsonSettings = JObject.Parse(settings.Value);
                return (string)jsonSettings[setting];
            }
            else
                return null;
        }

        public static void StorePhoneSetting(Folder clientSettings, string setting, string value)
        {
            if (clientSettings == null)
                return;

            var phoneSettingsItem = GetPhoneSettingsItem(clientSettings);
            var settings = phoneSettingsItem.GetFieldValue(FieldNames.Value, true);
            JObject jsonSettings = null;
            if (settings != null && !String.IsNullOrEmpty(settings.Value))
                jsonSettings = JObject.Parse(settings.Value);
            else
                jsonSettings = new JObject();
            jsonSettings[setting] = value;
            settings.Value = jsonSettings.ToString();

            // store the client settings
            StorageHelper.WriteClientSettings(clientSettings);
        }

        public static Item GetPhoneSettingsItem(Folder clientSettings)
        {
            if (clientSettings == null)
                return null;

            // get the list of phone settings
            Item phoneSettings = null;
            if (clientSettings.Items.Any(i => i.Name == SystemEntities.PhoneSettings))
                phoneSettings = clientSettings.Items.Single(i => i.Name == SystemEntities.PhoneSettings);
            else
            {
                DateTime now = DateTime.UtcNow;
                phoneSettings = new Item()
                {
                    Name = SystemEntities.PhoneSettings,
                    FolderID = clientSettings.ID,
                    ItemTypeID = SystemItemTypes.NameValue,
                    FieldValues = new ObservableCollection<FieldValue>(),
                    Created = now,
                    LastModified = now
                };
                clientSettings.Items.Add(phoneSettings);

                // store the client settings
                StorageHelper.WriteClientSettings(clientSettings);

                // queue up a server request
                RequestQueue.EnqueueRequestRecord(new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                    Body = phoneSettings,
                    ID = phoneSettings.ID
                });
            }

            return phoneSettings;
        }

        public static string GetTheme(Folder clientSettings)
        {
            return GetPhoneSetting(clientSettings, PhoneSettings.Theme);
        }

        public static void StoreTheme(Folder clientSettings, string value)
        {
            StorePhoneSetting(clientSettings, PhoneSettings.Theme, value);
        }

        #region Helpers

        private static Item GetListSortOrders(Folder clientSettings)
        {
            if (clientSettings == null)
                return null;

            // get the list of list sort orders
            Item listSortOrders = null;
            if (clientSettings.Items.Any(i => i.Name == SystemEntities.ListSortOrders))
                listSortOrders = clientSettings.Items.Single(i => i.Name == SystemEntities.ListSortOrders);
            else
            {
                DateTime now = DateTime.UtcNow;
                listSortOrders = new Item()
                {
                    Name = SystemEntities.ListSortOrders,
                    FolderID = clientSettings.ID,
                    IsList = true,
                    ItemTypeID = SystemItemTypes.Reference,
                    Items = new ObservableCollection<Item>(),
                    Created = now,
                    LastModified = now
                };
                clientSettings.Items.Add(listSortOrders);

                // store the client settings
                StorageHelper.WriteClientSettings(clientSettings);

                // queue up a server request
                RequestQueue.EnqueueRequestRecord(new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                    Body = listSortOrders,
                    ID = listSortOrders.ID
                });
            }

            return listSortOrders;
        }

        #endregion Helpers
    }
}
