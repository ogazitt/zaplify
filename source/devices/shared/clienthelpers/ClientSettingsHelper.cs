using System;
using System.Linq;
using System.Net;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Shared.Entities;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace BuiltSteady.Zaplify.Devices.ClientHelpers
{
    public class ClientSettingsHelper
    {
        public static string GetListSortOrder(Folder clientSettings, ClientEntity list)
        {
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

        #region Helpers

        private static Item GetListSortOrders(Folder clientSettings)
        {
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
