using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.Devices.ClientHelpers
{
    public class ListMetadataHelper
    {
        public static string GetListMetadataValue(Folder clientSettings, ClientEntity list, string fieldName)
        {
            if (clientSettings == null)
                return null;

            var listsMetadata = GetListMetadataList(clientSettings);
            var listID = list.ID.ToString();
            string value = null;
            if (clientSettings.Items.Any(i =>
                i.ParentID == listsMetadata.ID &&
                i.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == listID)))
            {
                var metadataItem = clientSettings.Items.Single(i =>
                    i.ParentID == listsMetadata.ID &&
                    i.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == listID));
                if (metadataItem.FieldValues.Any(fv => fv.FieldName == fieldName))
                {
                    var fieldValue = metadataItem.FieldValues.Single(fv => fv.FieldName == fieldName);
                    value = (fieldValue != null) ? fieldValue.Value : null;
                }
            }
            return value;
        }

        public static void StoreListMetadataValue(Folder clientSettings, ClientEntity list, string fieldName, string value)
        {
            if (clientSettings == null)
                return;

            var listsMetadata = GetListMetadataList(clientSettings);
            var listID = list.ID.ToString();
            if (clientSettings.Items.Any(i =>
                i.ParentID == listsMetadata.ID &&
                i.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == listID)))
            {
                var metadataItem = clientSettings.Items.Single(i =>
                    i.ParentID == listsMetadata.ID &&
                    i.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == listID));
                Item copy = new Item(metadataItem);
                var fieldValue = metadataItem.GetFieldValue(fieldName, true);
                fieldValue.Value = value;

                // queue up a server request
                if (clientSettings.ID != Guid.Empty)
                {
                    RequestQueue.EnqueueRequestRecord(RequestQueue.SystemQueue, new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Update,
                        Body = new List<Item>() { copy, metadataItem },
                        BodyTypeName = "Item",
                        ID = metadataItem.ID,
                        IsDefaultObject = true
                    });
                }
            }
            else
            {
                Guid id = Guid.NewGuid();
                DateTime now = DateTime.UtcNow;
                var metadataItem = new Item()
                {
                    ID = id,
                    Name = list.Name,
                    ItemTypeID = SystemItemTypes.Reference,
                    FolderID = clientSettings.ID,
                    ParentID = listsMetadata.ID,
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
                            FieldName = fieldName,
                            Value = value
                        }
                    }
                };
                clientSettings.Items.Add(metadataItem);

                // queue up a server request
                if (clientSettings.ID != Guid.Empty)
                {
                    RequestQueue.EnqueueRequestRecord(RequestQueue.SystemQueue, new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                        Body = metadataItem,
                        ID = metadataItem.ID,
                        IsDefaultObject = true
                    });
                }
            }

            // store the client settings
            StorageHelper.WriteClientSettings(clientSettings);
        }

        class SelectedCount
        {
            public int Count { get; set; }
            public Item EntityRefItem { get; set; }
        }

        public static List<Item> GetListsOrderedBySelectedCount(Folder clientSettings)
        {
            var metadataList = GetListMetadataList(clientSettings);
            var selectedCountLists = clientSettings.Items.Where(i =>
                i.ParentID == metadataList.ID &&
                i.FieldValues.Any(fv => fv.FieldName == FieldNames.SelectedCount)).ToList();

            var orderedLists = new List<SelectedCount>();
            foreach (var l in selectedCountLists)
                orderedLists.Add(new SelectedCount() { EntityRefItem = l, Count = Convert.ToInt32(l.GetFieldValue(FieldNames.SelectedCount).Value) });

            // return the ordered lists
            return orderedLists.OrderByDescending(sc => sc.Count).ThenBy(sc => sc.EntityRefItem.Name).Select(sc => sc.EntityRefItem).ToList();
        }

        public static void IncrementListSelectedCount(Folder clientSettings, ClientEntity list)
        {
            // get, increment, and store the selected count for a list
            string countString = GetListMetadataValue(clientSettings, list, FieldNames.SelectedCount);
            int count = Convert.ToInt32(countString);
            count++;
            countString = count.ToString();
            StoreListMetadataValue(clientSettings, list, FieldNames.SelectedCount, countString);
        }

        public static string GetListSortOrder(Folder clientSettings, ClientEntity list)
        {
            return GetListMetadataValue(clientSettings, list, FieldNames.SortBy);
        }

        public static void StoreListSortOrder(Folder clientSettings, ClientEntity list, string listSortOrder)
        {
            StoreListMetadataValue(clientSettings, list, FieldNames.SortBy, listSortOrder);
        }

        public static Item GetDefaultList(Folder clientSettings, Guid itemType)
        {
            var defaultLists = GetDefaultListsList(clientSettings);
            if (defaultLists == null) 
                return null;

            return clientSettings.Items.FirstOrDefault(
                    i => i.ParentID == defaultLists.ID &&
                    i.FieldValues.Any(f => f.FieldName == FieldNames.Value && f.Value == itemType.ToString()));
        }

        #region Helpers

        private static Item GetDefaultListsList(Folder clientSettings)
        {
            return GetClientSettingsList(clientSettings, SystemEntities.DefaultLists, SystemItemTypes.NameValue);
        }

        private static Item GetListMetadataList(Folder clientSettings)
        {
            return GetClientSettingsList(clientSettings, SystemEntities.ListMetadata, SystemItemTypes.Reference);
        }

        private static Item GetClientSettingsList(Folder clientSettings, string name, Guid itemType)
        {
            if (clientSettings == null)
                return null;

            // get the list item 
            Item listItem = null;
            if (clientSettings.Items.Any(i => i.Name == name && i.ParentID == null))
                listItem = clientSettings.Items.Single(i => i.Name == name && i.ParentID == null);
            else
            {
                DateTime now = DateTime.UtcNow;
                listItem = new Item()
                {
                    Name = SystemEntities.ListMetadata,
                    FolderID = clientSettings.ID,
                    IsList = true,
                    ItemTypeID = itemType,
                    Items = new ObservableCollection<Item>(),
                    Created = now,
                    LastModified = now
                };
                clientSettings.Items.Add(listItem);

                // store the client settings
                StorageHelper.WriteClientSettings(clientSettings);

                // queue up a server request
                if (clientSettings.ID != Guid.Empty)
                {
                    RequestQueue.EnqueueRequestRecord(RequestQueue.SystemQueue, new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                        Body = listItem,
                        ID = listItem.ID,
                        IsDefaultObject = true
                    });
                }
            }

            return listItem;
        }

        #endregion Helpers
    }
}
