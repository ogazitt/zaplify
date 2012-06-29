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
        public static string GetListMetadataValue(Folder phoneClient, ClientEntity list, string fieldName)
        {
            if (phoneClient == null)
                return null;

            var listsMetadata = GetListMetadataList(phoneClient);
            var listID = list.ID.ToString();
            string value = null;
            if (phoneClient.Items.Any(i =>
                i.ParentID == listsMetadata.ID &&
                i.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == listID)))
            {
                var metadataItem = phoneClient.Items.Single(i =>
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

        public static void StoreListMetadataValue(Folder phoneClient, ClientEntity list, string fieldName, string value)
        {
            if (phoneClient == null)
                return;

            var listsMetadata = GetListMetadataList(phoneClient);
            var listID = list.ID.ToString();
            if (phoneClient.Items.Any(i =>
                i.ParentID == listsMetadata.ID &&
                i.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == listID)))
            {
                var metadataItem = phoneClient.Items.Single(i =>
                    i.ParentID == listsMetadata.ID &&
                    i.FieldValues.Any(fv => fv.FieldName == FieldNames.EntityRef && fv.Value == listID));
                Item copy = new Item(metadataItem);
                var fieldValue = metadataItem.GetFieldValue(fieldName, true);
                fieldValue.Value = value;

                // queue up a server request
                if (phoneClient.ID != Guid.Empty)
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
                    FolderID = phoneClient.ID,
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
                phoneClient.Items.Add(metadataItem);

                // queue up a server request
                if (phoneClient.ID != Guid.Empty)
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

            // store the phone client folder
            StorageHelper.WritePhoneClient(phoneClient);
        }

        class SelectedCount
        {
            public int Count { get; set; }
            public Item EntityRefItem { get; set; }
        }

        public static List<Item> GetListsOrderedBySelectedCount(Folder phoneClient)
        {
            var metadataList = GetListMetadataList(phoneClient);
            var selectedCountLists = phoneClient.Items.Where(i =>
                i.ParentID == metadataList.ID &&
                i.FieldValues.Any(fv => fv.FieldName == ExtendedFieldNames.SelectedCount)).ToList();

            var orderedLists = new List<SelectedCount>();
            foreach (var l in selectedCountLists)
                orderedLists.Add(new SelectedCount() { EntityRefItem = l, Count = Convert.ToInt32(l.GetFieldValue(ExtendedFieldNames.SelectedCount).Value) });

            // return the ordered lists
            return orderedLists.OrderByDescending(sc => sc.Count).ThenBy(sc => sc.EntityRefItem.Name).Select(sc => sc.EntityRefItem).ToList();
        }

        public static void IncrementListSelectedCount(Folder phoneClient, ClientEntity list)
        {
            // get, increment, and store the selected count for a list
            string countString = GetListMetadataValue(phoneClient, list, ExtendedFieldNames.SelectedCount);
            int count = Convert.ToInt32(countString);
            count++;
            countString = count.ToString();
            StoreListMetadataValue(phoneClient, list, ExtendedFieldNames.SelectedCount, countString);
        }

        public static string GetListSortOrder(Folder phoneClient, ClientEntity list)
        {
            return GetListMetadataValue(phoneClient, list, ExtendedFieldNames.SortBy);
        }

        public static void StoreListSortOrder(Folder phoneClient, ClientEntity list, string listSortOrder)
        {
            StoreListMetadataValue(phoneClient, list, ExtendedFieldNames.SortBy, listSortOrder);
        }

        public static Item GetDefaultList(Folder client, Guid itemType)
        {
            var defaultLists = GetDefaultListsList(client);
            if (defaultLists == null) 
                return null;

            return client.Items.FirstOrDefault(
                    i => i.ParentID == defaultLists.ID &&
                    i.FieldValues.Any(f => f.FieldName == FieldNames.Value && f.Value == itemType.ToString()));
        }

        #region Helpers

        private static Item GetDefaultListsList(Folder client)
        {
            return GetOrCreateList(client, SystemEntities.DefaultLists, SystemItemTypes.NameValue);
        }

        private static Item GetListMetadataList(Folder phoneClient)
        {
            return GetOrCreateList(phoneClient, SystemEntities.ListMetadata, SystemItemTypes.Reference);
        }

        // helper for adding list item to either $Client or $PhoneClient folder
        private static Item GetOrCreateList(Folder folder, string name, Guid itemType)
        {
            if (folder == null)
                return null;

            // get the list item 
            Item listItem = null;
            if (folder.Items.Any(i => i.Name == name && i.ParentID == null))
                listItem = folder.Items.First(i => i.Name == name && i.ParentID == null);
            else
            {
                DateTime now = DateTime.UtcNow;
                listItem = new Item()
                {
                    Name = SystemEntities.ListMetadata,
                    FolderID = folder.ID,
                    IsList = true,
                    ItemTypeID = itemType,
                    Items = new ObservableCollection<Item>(),
                    Created = now,
                    LastModified = now
                };
                folder.Items.Add(listItem);

                // store the client or phone client folder
                if (folder.Name == SystemEntities.Client)
                    StorageHelper.WriteClient(folder);
                else if (folder.Name == SystemEntities.PhoneClient)
                    StorageHelper.WritePhoneClient(folder);

                // queue up a server request
                if (folder.ID != Guid.Empty)
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
