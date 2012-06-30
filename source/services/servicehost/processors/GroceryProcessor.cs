using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceUtilities.Grocery;
using BuiltSteady.Zaplify.ServiceUtilities.Supermarket;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.ServiceHost
{
    public class GroceryProcessor : ItemProcessor
    {
        public GroceryProcessor(User user, UserStorageContext storage)
        {
            this.user = user;
            this.storage = storage;
        }

        public override bool ProcessCreate(Item item)
        {
            // base method extracts intent
            if (base.ProcessCreate(item))
                return true;

            // assign category to the grocery item
            // create a new category fieldvalue in the item to process
            var categoryFV = item.GetFieldValue(FieldNames.Category, true);

            // check if the category for this item's name has already been saved under the $User/Grocery list
            // in this case, store the category in the incoming item
            var groceryCategoryFV = GetGroceryCategoryFieldValue(item);
            if (groceryCategoryFV != null && groceryCategoryFV.Value != null)
            {
                categoryFV.Value = groceryCategoryFV.Value;
                return true;
            }

            // get the "intent" which in this case is the normalized name 
            var intentName = item.Name.ToLower();
            var intentFV = item.GetFieldValue(ExtendedFieldNames.Intent);
            if (intentFV != null && intentFV.Value != null)
                intentName = intentFV.Value;

            // set up the grocery API endpoint
            GroceryAPI gApi = new GroceryAPI();
            gApi.EndpointBaseUri = string.Format("{0}{1}/", HostEnvironment.DataServicesEndpoint, "Grocery");
            TraceLog.TraceDetail("GroceryAPI Endpoint: " + gApi.EndpointBaseUri);

            // try to find the category from the local Grocery Controller
            try
            {
                var results = gApi.Query(GroceryQueries.GroceryCategory, intentName).ToList();

                // this should only return one result
                if (results.Count > 0)
                {
                    // get the category
                    foreach (var entry in results)
                    {
                        categoryFV.Value = entry[GroceryQueryResult.Category];
                        if (!String.IsNullOrEmpty(entry[GroceryQueryResult.ImageUrl]))
                            item.GetFieldValue(FieldNames.Picture, true).Value = entry[GroceryQueryResult.ImageUrl];
                        // only grab the first category
                        break;
                    }
                    TraceLog.TraceInfo(String.Format("Grocery API assigned {0} category to item {1}", categoryFV.Value, item.Name));
                    return true;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Grocery API or database commit failed", ex);
            }

            // use the Supermarket API to get the grocery category
            SupermarketAPI smApi = new SupermarketAPI();

#if FALSE   // use the synchronous codepath for now
            // execute the call asynchronously so as to not block the response
            smApi.BeginQuery(SupermarketQueries.SearchByProductName, item.Name, new AsyncCallback((iar) =>
            {
                try
                {
                    var results = smApi.EndQuery(iar);

                    // find the item using a new context 
                    var context = Storage.NewUserContext;
                    var groceryItem = context.Items.Single(i => i.ID == item.ID);
                    FieldValue categoryFV = groceryItem.GetFieldValue(FieldNames.Category, true);

                    // get the category
                    foreach (var entry in results)
                    {
                        categoryFV.Value = entry[SupermarketQueryResult.Category];
                        // only grab the first category
                        TraceLog.TraceInfo(String.Format("ProcessCreate: assigned {0} category to item {1}", categoryFV.Value, item.Name));
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    TraceLog.TraceException("ProcessCreate: Supermarket API or database commit failed", ex);
                }
            }), null);
#else
            try
            {
                var results = smApi.Query(SupermarketQueries.SearchByProductName, intentName);

                // get the category and image
                foreach (var entry in results)
                {
                    categoryFV.Value = entry[SupermarketQueryResult.Category];
                    if (!String.IsNullOrEmpty(entry[SupermarketQueryResult.Image]))
                        item.GetFieldValue(FieldNames.Picture, true).Value = entry[SupermarketQueryResult.Image];

                    // write the data to the blob store
                    BlobStore.WriteBlobData(BlobStore.GroceryContainerName, entry[SupermarketQueryResult.Name], entry.ToString());

                    // only grab the first category
                    TraceLog.TraceInfo(String.Format("Supermarket API assigned {0} category to item {1}", categoryFV.Value, item.Name));
                    return true;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Supermarket API or database commit failed", ex);
            }
#endif
            return false;
        }

        public override bool ProcessUpdate(Item oldItem, Item newItem)
        {
            // base handles ItemType changing
            if (base.ProcessUpdate(oldItem, newItem))
                return true;

            if (newItem.Name != oldItem.Name)
            {   // name changed, process like new item
                ProcessCreate(newItem);
                return true;
            }

            // if the user stored a grocery category, overwrite the current category that's saved in the 
            // corresponding item under the $User/Grocery list
            var categoryFV = newItem.GetFieldValue(FieldNames.Category);
            if (categoryFV != null && categoryFV.Value != null)
            {
                // the old category must not exist or the value must have changed
                var oldCategoryFV = oldItem.GetFieldValue(FieldNames.Category);
                if (oldCategoryFV == null || oldCategoryFV.Value != categoryFV.Value)
                {
                    // get the grocery category fieldvalue for the corresponding Item in the $User/Grocery list
                    var groceryCategoryFV = GetGroceryCategoryFieldValue(newItem, true);
                    if (groceryCategoryFV == null)
                        return false;

                    // write the new category in the corresponding item in the user's $User/Grocery list
                    groceryCategoryFV.Value = categoryFV.Value;

                    // null out the picture URL from the new item's fieldvalues.  if the user overrode the category,
                    // we cannot trust that we had the right picture 
                    var picFV = newItem.GetFieldValue(FieldNames.Picture);
                    if (picFV != null && !String.IsNullOrEmpty(picFV.Value))
                        picFV.Value = null;

                    return true;
                }
            }
            return false;
        }

        private FieldValue GetGroceryCategoryFieldValue(Item item, bool create = false)
        {
            // get or create the list for Grocery item types in the UserFolder
            var knownGroceryItems = storage.UserFolder.GetListForItemType(user, SystemItemTypes.Grocery);
            if (knownGroceryItems == null)
                return null;

            Item groceryItem = null;
            FieldValue groceryCategoryFV = null;

            // get the normalized name for the grocery (stored in Intent) or resort to lowercased item name
            var intentFV = item.GetFieldValue(ExtendedFieldNames.Intent);
            var itemName = intentFV != null && intentFV.Value != null ? intentFV.Value : item.Name.ToLower();
            
            if (storage.Items.Any(i => i.Name == itemName && i.ParentID == knownGroceryItems.ID))
            {
                groceryItem = storage.Items.Include("FieldValues").Single(i => i.Name == itemName && i.ParentID == knownGroceryItems.ID);
                groceryCategoryFV = groceryItem.GetFieldValue(FieldNames.Category, true);
            }
            else if (create)
            {
                // create grocery item category item 
                DateTime now = DateTime.UtcNow;
                var groceryCategoryItemID = Guid.NewGuid();
                groceryCategoryFV = new FieldValue()
                {
                    ItemID = groceryCategoryItemID,
                    FieldName = FieldNames.Category,
                    Value = null,
                };
                groceryItem = new Item()
                {
                    ID = groceryCategoryItemID,
                    Name = itemName,
                    FolderID = knownGroceryItems.FolderID,
                    UserID = user.ID,
                    ItemTypeID = SystemItemTypes.NameValue,
                    ParentID = knownGroceryItems.ID,
                    Created = now,
                    LastModified = now,
                    FieldValues = new List<FieldValue>() { groceryCategoryFV }
                };
                storage.Items.Add(groceryItem);
            }

            return groceryCategoryFV;
        }
    }
}
