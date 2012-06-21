using System;
using System.Collections.Generic;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost.Helpers;
using BuiltSteady.Zaplify.ServiceHost.Nlp;
using BuiltSteady.Zaplify.ServiceUtilities.FBGraph;
using BuiltSteady.Zaplify.ServiceUtilities.Grocery;
using BuiltSteady.Zaplify.ServiceUtilities.Supermarket;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.ServiceHost
{
    public abstract class ItemProcessor
    {
        protected UserStorageContext userContext;
        protected User currentUser;

        // Factory method to create a new item processor based on the item type
        public static ItemProcessor Create(UserStorageContext userContext, User currentUser, Guid itemTypeID)
        {
            if (itemTypeID == SystemItemTypes.Contact)
                return new ContactProcessor(userContext, currentUser);
            if (itemTypeID == SystemItemTypes.Grocery)
                return new GroceryProcessor(userContext, currentUser);
            if (itemTypeID == SystemItemTypes.Task)
                return new TaskProcessor(userContext, currentUser);
            return null;
        }


        // Process a new item that is being created  
        // Extracts the intent based on ItemType and extends as FieldValue on Item
        // return true to indicate to sub-classes that processing is complete
        public virtual bool ProcessCreate(Item item)
        {
            var intent = ExtractIntent(item);
            if (intent != null)
                CreateIntentFieldValue(item, intent);
            return false;
        }

        // Process an item that is being deleted.  
        // Default implementation does nothing.
        // return true to indicate to sub-classes that processing is complete
        public virtual bool ProcessDelete(Item item)
        {
            return false;
        }

        // Process an item being updated.  
        // Default implementation checks to see if Name or ItemType have changed
        // which is equivalent to a new Item being created 
        public virtual bool ProcessUpdate(Item oldItem, Item newItem)
        {
            if (newItem.Name != oldItem.Name || newItem.ItemTypeID != oldItem.ItemTypeID)
            {
                ProcessCreate(newItem);
                return true;
            }

#if false            
            // OBSOLETE! Stamping of CompletedOn field is done on each client to get proper timezone and immediate response
            // process CompletedOn field if newItem has a Complete field set true
            FieldValue complete = newItem.GetFieldValue(FieldNames.Complete);
            if (complete != null && complete.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                FieldProcessor.ProcessUpdateCompletedOn(userContext, oldItem, newItem);
            }
#endif
            return false;
        }


        // Create and add an Intent FieldValue to the item
        protected void CreateIntentFieldValue(Item item, string intent)
        {
            item.GetFieldValue(ExtendedFieldNames.Intent, true).Value = intent;
            TraceLog.TraceDetail(String.Format("Assigned {0} intent to item {1}", intent, item.Name));
        }


        // The Intent of an Item is a normalized string that used to infer meaning for the item
        // Intent is may be determined differently based on ItemType
        // For example, simple NLP is used to determine Intent for a Task to help select a Workflow 
        // Default implementation for the Intent of an item is to lowercase the item name.
        protected virtual string ExtractIntent(Item item)
        {
            return item.Name.ToLower();
        }
    }

    public class ContactProcessor : ItemProcessor
    {
        public ContactProcessor(UserStorageContext userContext, User currentUser)
        {
            this.userContext = userContext;
            this.currentUser = currentUser;
        }

        public override bool ProcessCreate(Item item)
        {
            if (base.ProcessCreate(item))
                return true;

            var fbret = FacebookHelper.AddContactInfo(userContext, item);
            var pcret = PossibleContactHelper.AddContact(userContext, item);
            return fbret && pcret;
        }

        public override bool ProcessDelete(Item item)
        {
            if (base.ProcessDelete(item))
                return true;
            return PossibleContactHelper.RemoveContact(userContext, item);
        }

        public override bool ProcessUpdate(Item oldItem, Item newItem)
        {
            // base method checks to see if name or itemtype have changed and if so, treats as a create
            if (base.ProcessUpdate(oldItem, newItem))
                return true;

            // if the facebook ID is set or changed, retrieve FB info
            var fbfv = newItem.GetFieldValue(FieldNames.FacebookID);
            if (fbfv != null && fbfv.Value != null)
            {
                // the old category must not exist or the value must have changed
                var oldfbfv = oldItem.GetFieldValue(FieldNames.FacebookID);
                if (oldfbfv == null || oldfbfv.Value != fbfv.Value)
                {
                    FBGraphAPI fbApi = new FBGraphAPI();
                    User user = userContext.GetUser(currentUser.ID, true);
                    UserCredential cred = user.GetCredential(UserCredential.FacebookConsent);
                    if (cred != null && cred.AccessToken != null)
                    {
                        fbApi.AccessToken = cred.AccessToken;
                    }
                    else
                    {
                        TraceLog.TraceError(FacebookHelper.TRACE_NO_FB_TOKEN);
                        return false;
                    }

                    // get or create an EntityRef in the UserFolder EntityRefs list
                    var entityRefItem = userContext.UserFolder.GetEntityRef(currentUser, newItem);
                    if (entityRefItem == null)
                    {
                        TraceLog.TraceError(FacebookHelper.TRACE_NO_CONTACT_ENTITYREF);
                        return false;
                    }

                    // get the contact's profile information from facebook
                    try
                    {
                        // this is written as a foreach because the Query API returns an IEnumerable, but there is only one result
                        foreach (var contact in fbApi.Query(fbfv.Value, FBQueries.BasicInformation))
                        {
                            newItem.GetFieldValue(FieldNames.Picture, true).Value = String.Format("https://graph.facebook.com/{0}/picture", fbfv.Value);
                            var birthday = (string)contact[FBQueryResult.Birthday];
                            if (birthday != null)
                                newItem.GetFieldValue(FieldNames.Birthday, true).Value = birthday;
                            var gender = (string)contact[FBQueryResult.Gender];
                            if (gender != null)
                                entityRefItem.GetFieldValue(FieldNames.Gender, true).Value = gender;
                        }
                        //userContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceException(FacebookHelper.TRACE_NO_SAVE_FBCONTACTINFO, ex);
                    }

                    return true;
                }
            }
            return false;
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
            try
            {
                Phrase phrase = new Phrase(item.Name);
                if (phrase.Task != null)
                {
                    string verb = phrase.Task.Verb.ToLower();
                    string noun = phrase.Task.Article.ToLower();
                    Intent intent = Storage.NewSuggestionsContext.Intents.FirstOrDefault(i => i.Verb == verb && i.Noun == noun);
                    if (intent != null)
                        return intent.WorkflowType;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Could not initialize NLP engine", ex);
            }
            return base.ExtractIntent(item);
        }
    }

    public class GroceryProcessor : ItemProcessor
    {
        public GroceryProcessor(UserStorageContext userContext, User currentUser)
        {
            this.userContext = userContext;
            this.currentUser = currentUser;
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
            // base method checks to see if name or itemtype have changed and if so, treats as a create
            if (base.ProcessUpdate(oldItem, newItem))
                return true;

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
            var knownGroceryItems = userContext.UserFolder.GetListForItemType(currentUser, SystemItemTypes.Grocery);
            if (knownGroceryItems == null)
                return null;

            Item groceryItem = null;
            FieldValue groceryCategoryFV = null;

            // get the normalized name for the grocery (stored in Intent) or resort to lowercased item name
            var intentFV = item.GetFieldValue(ExtendedFieldNames.Intent);
            var itemName = intentFV != null && intentFV.Value != null ? intentFV.Value : item.Name.ToLower();
            
            if (userContext.Items.Any(i => i.Name == itemName && i.ParentID == knownGroceryItems.ID))
            {
                groceryItem = userContext.Items.Include("FieldValues").Single(i => i.Name == itemName && i.ParentID == knownGroceryItems.ID);
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
                    UserID = currentUser.ID,
                    ItemTypeID = SystemItemTypes.NameValue,
                    ParentID = knownGroceryItems.ID,
                    Created = now,
                    LastModified = now,
                    FieldValues = new List<FieldValue>() { groceryCategoryFV }
                };
                userContext.Items.Add(groceryItem);
            }

            return groceryCategoryFV;
        }
    }
}
