using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.ServiceUtilities.Grocery;
using BuiltSteady.Zaplify.ServiceUtilities.Supermarket;

namespace BuiltSteady.Zaplify.ServiceHost
{
    public abstract class ItemProcessor
    {
        protected UserStorageContext userContext;
        protected User currentUser;

        /// <summary>
        /// Factory method to create a new item processor based on the item type
        /// </summary>
        /// <param name="userContext">User storage context</param>
        /// <param name="currentUser">Current user</param>
        /// <param name="itemTypeID">Guid representing the item type ID</param>
        /// <returns>A new ItemProcessor subtype</returns>
        public static ItemProcessor Create(UserStorageContext userContext, User currentUser, Guid itemTypeID)
        {
            if (itemTypeID == SystemItemTypes.ShoppingItem)
                return new ShoppingItemProcessor(userContext, currentUser);
            if (itemTypeID == SystemItemTypes.Task)
                return new TaskProcessor(userContext, currentUser);
            return null;
        }

        /// <summary>
        /// Process a new item that is being created.  The default 
        /// implementation extracts the intent in an itemtype-specific way
        /// and attaches the intent to the item's fieldvalues.
        /// </summary>
        /// <param name="item">Item to create</param>
        /// <returns>true if already processed, false if subclass needs to process</returns>
        public virtual bool ProcessCreate(Item item)
        {
            var intent = ExtractIntent(item);
            if (intent != null)
                CreateIntentFieldValue(item, intent);
            return false;
        }

        /// <summary>
        /// Process an item that is being deleted.  The default implementation does nothing.
        /// </summary>
        /// <param name="item">Item to create</param>
        /// <returns>true if already processed, false if subclass needs to process</returns>
        public virtual bool ProcessDelete(Item item)
        {
            return false;
        }

        /// <summary>
        /// Process an update.  The default implementation checks if the
        /// name or itemtype have changed - if so, this is equivalent to creating 
        /// a new item.  
        /// </summary>
        /// <param name="oldItem">Old item</param>
        /// <param name="newItem">New item</param>
        /// <returns>true if already processed, false if subclass needs to process</returns>
        public virtual bool ProcessUpdate(Item oldItem, Item newItem)
        {
            if (newItem.Name != oldItem.Name ||
                newItem.ItemTypeID != oldItem.ItemTypeID)
            {
                ProcessCreate(newItem);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Create and attach an Intent fieldvalue to the item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="intent"></param>
        protected void CreateIntentFieldValue(Item item, string intent)
        {
            item.GetFieldValue(FieldNames.Intent, true).Value = intent;
            userContext.SaveChanges();
        }

        /// <summary>
        /// The intent of an item is a normalized string that is a key into an itemtype-specific meaning for the item
        /// For example, the intent of a Task is the Workflow that it triggers; 
        /// The intent of a Shopping Item is the lowercased naem.
        /// Default implementation for the intent of an item is the item's name, lowercased
        /// </summary>
        /// <param name="item">Item to extract an intent from</param>
        /// <returns>string representing intent</returns>
        protected virtual string ExtractIntent(Item item)
        {
            return item.Name.ToLower();
        }
    }

    public class TaskProcessor : ItemProcessor
    {
        public TaskProcessor(UserStorageContext userContext, User currentUser)
        {
            this.userContext = userContext;
            this.currentUser = currentUser;
        }

        public override bool ProcessCreate(Item item)
        {
            return base.ProcessCreate(item);
            // kick off workflow?
        }

        public override bool ProcessUpdate(Item oldItem, Item newItem)
        {
            return base.ProcessUpdate(oldItem, newItem);
            // kick off workflow?
        }
        
        protected override string ExtractIntent(Item item)
        {
            // TODO: do NLP
            return base.ExtractIntent(item);
        }
    }

    public class ShoppingItemProcessor : ItemProcessor
    {
        public ShoppingItemProcessor(UserStorageContext userContext, User currentUser)
        {
            this.userContext = userContext;
            this.currentUser = currentUser;
        }

        public override bool ProcessCreate(Item item)
        {
            // base method extracts intent
            if (base.ProcessCreate(item))
                return true;

            // assign category to the shopping item

            // check if the user already set a category in the incoming item
            // in this case, simply overwrite the current category that's saved in $User/GroceryCategories
            FieldValue groceryCategoryFV = null;
            var categoryFV = item.GetFieldValue(FieldNames.Category);
            if (categoryFV != null)
            {
                groceryCategoryFV = GetGroceryCategoryItemFieldValue(item, true);
                if (groceryCategoryFV == null)
                    return false;

                groceryCategoryFV.Value = categoryFV.Value;
                userContext.SaveChanges();
                return true;
            }

            // create a new category fieldvalue in the item to process
            categoryFV = item.GetFieldValue(FieldNames.Category, true);

            // check if the grocery category has already been saved in $User/GroceryCategories
            // in this case, store the category in the incoming item
            groceryCategoryFV = GetGroceryCategoryItemFieldValue(item);
            if (groceryCategoryFV != null && groceryCategoryFV.Value != null)
            {
                categoryFV.Value = groceryCategoryFV.Value;
                userContext.SaveChanges();
                return true;
            }

            // set up the grocery API endpoint
            GroceryAPI gApi = new GroceryAPI();
            gApi.EndpointBaseUri = string.Format("{0}{1}/", HostEnvironment.DataServicesEndpoint, "Grocery");
            TraceLog.TraceDetail("ProcessCreate: endpointURI: " + gApi.EndpointBaseUri);

            // try to find the category from the local Grocery Controller
            try
            {
                var results = gApi.Query(GroceryQueries.GroceryCategory, item.Name).ToList();

                // this should only return one result
                if (results.Count > 0)
                {
                    // get the category
                    foreach (var entry in results)
                    {
                        categoryFV.Value = entry[GroceryQueryResult.Category];
                        // only grab the first category
                        break;
                    }
                    userContext.SaveChanges();
                    TraceLog.TraceInfo(String.Format("ProcessCreate: Grocery API assigned {0} category to item {1}", categoryFV.Value, item.Name));
                    return true;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("ProcessCreate: Grocery API or database commit failed", ex);
            }

            // last resort...
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
                    var shoppingItem = context.Items.Single(i => i.ID == item.ID);
                    FieldValue categoryFV = shoppingItem.GetFieldValue(FieldNames.Category, true);

                    // get the category
                    foreach (var entry in results)
                    {
                        categoryFV.Value = entry[SupermarketQueryResult.Category];
                        // only grab the first category
                        userContext.SaveChanges();
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
                var results = smApi.Query(SupermarketQueries.SearchByProductName, item.Name);

                // get the category and image
                foreach (var entry in results)
                {
                    categoryFV.Value = entry[SupermarketQueryResult.Category];
                    if (!String.IsNullOrEmpty(entry[SupermarketQueryResult.Image]))
                        item.GetFieldValue(FieldNames.Picture, true).Value = entry[SupermarketQueryResult.Image];

                    // write the data to the blob store
                    BlobStore.WriteBlobData(BlobStore.GroceryContainerName, entry[SupermarketQueryResult.Name], entry.ToString());

                    // only grab the first category
                    userContext.SaveChanges();
                    TraceLog.TraceInfo(String.Format("ProcessCreate: Supermarket API assigned {0} category to item {1}", categoryFV.Value, item.Name));
                    return true;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("ProcessCreate: Supermarket API or database commit failed", ex);
            }
#endif
            return false;
        }

        public override bool ProcessUpdate(Item oldItem, Item newItem)
        {
            // base method checks to see if name or itemtype have changed and if so, treats as a create
            if (base.ProcessUpdate(oldItem, newItem))
                return true;

            // if the user stored a grocery category, overwrite the current category that's saved in $User/GroceryCategories
            var categoryFV = newItem.GetFieldValue(FieldNames.Category);
            if (categoryFV != null && categoryFV.Value != null)
            {
                // the old category must not exist or the value must have changed
                var oldCategoryFV = oldItem.GetFieldValue(FieldNames.Category);
                if (oldCategoryFV == null || oldCategoryFV.Value != categoryFV.Value)
                {
                    // get the grocery category fieldvalue for the corresponding Item in $User/GroceryCategories
                    var groceryCategoryFV = GetGroceryCategoryItemFieldValue(newItem, true);
                    if (groceryCategoryFV == null)
                        return false;

                    groceryCategoryFV.Value = categoryFV.Value;
                    userContext.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        private FieldValue GetGroceryCategoryItemFieldValue(Item item, bool create = false)
        {
            // get the grocery categories list under the $User folder
            var groceryCategories = userContext.GetOrCreateGroceryCategoriesList(currentUser);
            if (groceryCategories == null)
                return null;

            Item groceryCategory = null;
            FieldValue groceryCategoryFV = null;

            // get the normalized name for the grocery (stored in FieldNames.Intent) or resort to lowercased item name
            var intentFV = item.GetFieldValue(FieldNames.Intent);
            var itemName = intentFV != null && intentFV.Value != null ? intentFV.Value : item.Name.ToLower();
            
            if (userContext.Items.Any(i => i.Name == itemName && i.ParentID == groceryCategories.ID))
            {
                groceryCategory = userContext.Items.Include("FieldValues").Single(i => i.Name == itemName && i.ParentID == groceryCategories.ID);
                groceryCategoryFV = groceryCategory.GetFieldValue(FieldNames.Value, true);
            }
            else if (create)
            {
                // create grocery category item 
                DateTime now = DateTime.UtcNow;
                var groceryCategoryItemID = Guid.NewGuid();
                groceryCategoryFV = new FieldValue()
                {
                    ItemID = groceryCategoryItemID,
                    FieldName = FieldNames.Value,
                    Value = null,
                };
                groceryCategory = new Item()
                {
                    ID = groceryCategoryItemID,
                    Name = itemName,
                    FolderID = groceryCategories.FolderID,
                    UserID = currentUser.ID,
                    ItemTypeID = SystemItemTypes.NameValue,
                    ParentID = groceryCategories.ID,
                    Created = now,
                    LastModified = now,
                    FieldValues = new List<FieldValue>() { groceryCategoryFV }
                };
                userContext.Items.Add(groceryCategory);
            }

            return groceryCategoryFV;
        }
    }
}
