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

        /// <summary>
        /// Factory method to create a new item processor based on the item type
        /// </summary>
        /// <param name="userContext">User storage context</param>
        /// <param name="currentUser">Current user</param>
        /// <param name="itemTypeID">Guid representing the item type ID</param>
        /// <returns>A new ItemProcessor subtype</returns>
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
            if (newItem.Name != oldItem.Name || newItem.ItemTypeID != oldItem.ItemTypeID)
            {
                ProcessCreate(newItem);
                return true;
            }
            
            // process CompletedOn field if newItem has a Complete field set true
            FieldValue complete = newItem.GetFieldValue(FieldNames.Complete);
            if (complete != null && complete.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                FieldProcessor.ProcessUpdateCompletedOn(userContext, oldItem, newItem);
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
            TraceLog.TraceDetail(String.Format("CreateIntentFieldValue: assigned {0} intent to item {1}", intent, item.Name));
        }

        /// <summary>
        /// The intent of an item is a normalized string that is a key into an itemtype-specific meaning for the item
        /// For example, the intent of a Task is the Workflow that it triggers; 
        /// The intent of a ListItem, ShoppingItem, or Grocery is the lowercased name.
        /// Default implementation for the intent of an item is the item's name, lowercased
        /// </summary>
        /// <param name="item">Item to extract an intent from</param>
        /// <returns>string representing intent</returns>
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
                    try
                    {
                        User user = userContext.Users.Include("UserCredentials").Single(u => u.ID == currentUser.ID);
                        UserCredential cred = user.UserCredentials.Single(uc => uc.FBConsentToken != null);
                        fbApi.AccessToken = cred.FBConsentToken;
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceException("ProcessUpdate: could not find Facebook credential or consent token", ex);
                        return false;
                    }

                    // get or create an entityref in the entity ref list in the $User folder
                    var entityRefItem = userContext.GetOrCreateEntityRef(currentUser, newItem);
                    if (entityRefItem == null)
                    {
                        TraceLog.TraceError("ProcessUpdate: could not retrieve or create an entity ref for this contact");
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
                        TraceLog.TraceException("ProcessUpdate: could not save Facebook information to Contact", ex);
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
                TraceLog.TraceException("TaskProcessor.ExtractIntent: could not initialize NLP engine", ex);
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
            var intentFV = item.GetFieldValue(FieldNames.Intent);
            if (intentFV != null && intentFV.Value != null)
                intentName = intentFV.Value;

            // set up the grocery API endpoint
            GroceryAPI gApi = new GroceryAPI();
            gApi.EndpointBaseUri = string.Format("{0}{1}/", HostEnvironment.DataServicesEndpoint, "Grocery");
            TraceLog.TraceDetail("ProcessCreate: endpointURI: " + gApi.EndpointBaseUri);

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
            // get the known grocery item list under the $User folder
            var knownGroceryItems = userContext.GetOrCreateUserItemTypeList(currentUser, SystemItemTypes.Grocery);
            if (knownGroceryItems == null)
                return null;

            Item groceryItem = null;
            FieldValue groceryCategoryFV = null;

            // get the normalized name for the grocery (stored in FieldNames.Intent) or resort to lowercased item name
            var intentFV = item.GetFieldValue(FieldNames.Intent);
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
