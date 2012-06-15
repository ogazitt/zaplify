using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;
using BuiltSteady.Zaplify.WorkflowHost.Activities;

namespace BuiltSteady.Zaplify.WorkflowHost.Activities
{
    /// <summary>
    /// These variables flow between activities
    /// </summary>
    public class ActivityVariables
    {
        public const string Contact = "Contact";
        public const string Intent = "Intent";
        public const string LastStateData = "LastStateData";
        public const string Like = "Like";
        public const string Likes = "Likes";
        public const string LikeSuggestionList = "LikeSuggestionList";
        public const string ParentID = "ParentID";
        public const string SubjectHint = "SubjectHint";
    }

    public abstract class WorkflowActivity
    {
        public virtual string Name { get { return this.GetType().Name; } }
        public virtual string GroupDisplayName { get { return null; } }
        public virtual string OutputParameterName { get { return TargetFieldName; } }
        public virtual string SuggestionType { get { return null; } }
        public virtual string TargetFieldName { get { return null; } }
        public abstract Func<
            WorkflowInstance, 
            ServerEntity, // item to operate over
            object,       // extra state to send to the execution Function
            Status        // activity completion status
            > Function { get; }

        public enum Status
        {
            Error = -1,       // unrecoverable error encountered (terminate workflow)
            Pending = 0,      // the activity hasn't completed - e.g. awaiting user input (quiesce the workflow)
            Complete = 1,     // the activity completed (move to the next state)
            WorkflowDone = 2  // the workflow is complete (terminate workflow)
        }

        public UserStorageContext UserContext { get; set; }
        public SuggestionsStorageContext SuggestionsContext { get; set; }

        public Dictionary<string, string> InputParameters = new Dictionary<string, string>();

        /// <summary>
        /// Factory method to construct a typed activity based on its typename
        /// </summary>
        /// <param name="typeName">Type name to instantiate</param>
        /// <returns>typed activity</returns>
        public static WorkflowActivity CreateActivity(string typeName)
        {
            if (typeName == null)
                return null;

            // ensure that the activity is in the fully-qualified form
            // this assumes the WorkflowActivity class is in the same namespace as the one we are about to load
            string fullName = typeof(WorkflowActivity).FullName;
            if (typeName.StartsWith(fullName) == false)
            {
                fullName = fullName.Replace(typeof(WorkflowActivity).Name, "");
                typeName = fullName + typeName;
            }

            Type activityType = Type.GetType(typeName);
            if (activityType == null)
            {
                TraceLog.TraceError("Could not find type name " + typeName);
                return null;
            }

            WorkflowActivity activity = Activator.CreateInstance(activityType) as WorkflowActivity;
            return activity;
        }

        /// <summary>
        /// Factory method to construct a typed activity based on its definition
        /// </summary>
        /// <param name="definition">Json value corresponding to activity definition</param>
        /// <param name="instance">Workflow instance</param>
        /// <returns>typed activity</returns>
        public static WorkflowActivity CreateActivity(JObject definition, WorkflowInstance instance)
        {
            string typeName = (string) definition["Name"];
            WorkflowActivity activity = CreateActivity(typeName);
            if (activity == null)
                return null;

            foreach (var prop in definition)
            {
                switch (prop.Value.Type)
                {
                    case JTokenType.Object: // a sub-object (typically a nested activity) - store as a string
                        activity.InputParameters[prop.Key] = prop.Value.ToString();
                        break;
                    case JTokenType.Array: // treat as a string template and format appropriately
                        activity.InputParameters[prop.Key] = FormatStringTemplate(instance, prop.Value.ToString());
                        break;
                    default: // treat as a string and expand any variables
                        activity.InputParameters[prop.Key] = ExpandVariables(instance, (string)prop.Value.ToString());
                        break;
                }
            }
            return activity;
        }

        /// <summary>
        /// Check and process the target field - if it is on the item, store the value in the 
        /// state bag and return true
        /// </summary>
        /// <param name="workflowInstance">WorkflowInstance to process</param>
        /// <param name="item">Item to check</param>
        /// <returns>true for success, false if target field was not found</returns>
        protected bool CheckTargetField(WorkflowInstance workflowInstance, Item item)
        {
            if (TargetFieldName == null)
                return false;
            
            // if the target field has been set, this state can terminate
            try
            {
                FieldValue targetField = item.GetFieldValue(TargetFieldName);
                if (targetField != null && targetField.Value != null)
                {
                    StoreInstanceData(workflowInstance, OutputParameterName, targetField.Value);
                    StoreInstanceData(workflowInstance, ActivityVariables.LastStateData, targetField.Value);
                    TraceLog.TraceDetail(String.Format("Target field {0} was set to {1} for activity {2}", TargetFieldName, targetField.Value, Name));
                    return true;
                }
            }
            catch (Exception)
            {
                // not an error condition if the target field wasn't found or the value is empty
            }
            return false;
        }

        /// <summary>
        /// Create a set of suggestions from the results of calling the suggestion function
        /// </summary>
        /// <param name="workflowInstance">Workflow instance to operate over</param>
        /// <param name="item">Item to process</param>
        /// <param name="suggestionFunction">Suggestion generation function</param>
        /// <returns>Complete if an exact match was found, Pending if multiple suggestions created</returns>
        protected Status CreateSuggestions(
            WorkflowInstance workflowInstance, 
            ServerEntity entity, 
            Func<WorkflowInstance, ServerEntity, Dictionary<string,string>, Status> suggestionFunction)
        {
            // analyze the item for possible suggestions
            var suggestions = new Dictionary<string, string>();
            Status status = suggestionFunction.Invoke(workflowInstance, entity, suggestions);
            TraceLog.TraceDetail(String.Format("Retrieved {0} suggestions from activity {1}", suggestions.Count, Name));

            // if the function completed with an error, or without generating any data, return (this is typically a fail-fast state)
            if (status == Status.Error || suggestions.Count == 0)
                return status;

            // if an "exact match" was discovered without user input, store it now and return
            if (status == Status.Complete && suggestions.Count == 1)
            {
                string s = null;
                foreach (var value in suggestions.Values)
                    s = value;
                StoreInstanceData(workflowInstance, ActivityVariables.LastStateData, s);
                StoreInstanceData(workflowInstance, OutputParameterName, s);
                TraceLog.TraceDetail(String.Format("Exact match {0} was found for activity {1}", s, Name));
                return status;
            }

            // construct the group display name
            string groupDisplayName = GroupDisplayName;
            if (groupDisplayName == null)
                groupDisplayName = workflowInstance.State;
            else
                groupDisplayName = FormatStringTemplate(workflowInstance, groupDisplayName);

            // get the suggestion parent ID if available
            var parentID = GetInstanceData(workflowInstance, ActivityVariables.ParentID);

            // add suggestions received from the suggestion function
            try
            {
                int num = 0;
                foreach (var s in suggestions.Keys)
                {
                    // limit to four suggestions
                    if (num++ == 4)
                        break;

                    var sugg = new Suggestion()
                    {
                        ID = Guid.NewGuid(),
                        ParentID = parentID == null ? (Guid?) null : new Guid(parentID),
                        EntityID = entity.ID,
                        EntityType = entity.GetType().Name,
                        WorkflowType = workflowInstance.WorkflowType,
                        WorkflowInstanceID = workflowInstance.ID,
                        State = workflowInstance.State,
                        SuggestionType = SuggestionType,
                        DisplayName = s,
                        GroupDisplayName = groupDisplayName,
                        SortOrder = num,
                        Value = suggestions[s],
                        TimeSelected = null
                    };
                    SuggestionsContext.Suggestions.Add(sugg);

                    TraceLog.TraceDetail(String.Format("Created suggestion {0} in group {1} for activity {2}", s, groupDisplayName, Name));
                }

                SuggestionsContext.SaveChanges();
                return status;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Activity execution failed", ex);
                return Status.Error;
            }
        }

        /// <summary>
        /// Canned Execution method for an Activity for processing Items.  This method will:
        ///   1. validate the entity as an Item
        ///   2. verify the item type
        ///   3. check whether the target field is set on the Item and set state appropriately
        ///   4. check whether the user made a selection (via the data parameter) and set state appropriately
        ///   5. if none of this is true, add a set of suggestions from a suggestion function passed in
        /// </summary>
        /// <param name="workflowInstance">Workflow instance to process</param>
        /// <param name="entity">Entity to process</param>
        /// <param name="data">User selection data passed in</param>
        /// <param name="suggestionFunction">Suggestion function to invoke</param>
        /// <returns>return value for the Function</returns>
        protected Status Execute(
            WorkflowInstance workflowInstance, 
            ServerEntity entity, 
            object data,
            Guid expectedItemType,
            Func<WorkflowInstance, ServerEntity, Dictionary<string, string>, Status> suggestionFunction)
        {
            Item item = entity as Item;
            if (item == null)
            {
                TraceLog.TraceError("Entity is not an Item");
                return Status.Error;
            }

            if (VerifyItemType(item, expectedItemType) == false)
                return Status.Error; 

            // if the target field has been set, this state can terminate
            if (CheckTargetField(workflowInstance, item))
                return Status.Complete;

            // check for user selection
            if (data != null)
                return ProcessActivityData(workflowInstance, data);

            return CreateSuggestions(workflowInstance, entity, suggestionFunction);
        }

        /// <summary>
        /// Expand each of the variables in a format string based on the InstanceData
        /// Variable syntax is $(varname)
        /// </summary>
        /// <param name="workflowInstance">Workflow instance to operate over</param>
        /// <param name="formatExpr">Format expression to expand</param>
        /// <returns></returns>
        protected static string ExpandVariables(WorkflowInstance workflowInstance, string formatExpr)
        {
            if (formatExpr == null)
                return "";

            StringBuilder returnString = new StringBuilder();

            // successively substitute all variables until none are left, or an
            // unbound variable is found.  If the latter, ignore the whole term
            string newExpr = SubstituteNextVariable(workflowInstance, formatExpr);
            while (newExpr != null && newExpr != formatExpr)
            {
                formatExpr = newExpr;
                newExpr = SubstituteNextVariable(workflowInstance, formatExpr);
            }
            if (newExpr != null)
                returnString.Append(newExpr);

            return returnString.ToString();
        }

        /// <summary>
        /// This function takes a string containing a JSON-serialized array of terms
        /// Each term may have zero or more variables defined (bracketed in "$()") 
        /// At the end of execution, all variables will be bound from the workflow's InstanceData and
        /// the resultant (space-concatenated) string returned.  If an unbound variable is found, the term is discarded.
        /// Ex: ["Choose from", "$(Subject)'s", "likes"] will return "Choose from Mike's likes" if Subject is
        /// bound to "Mike", otherwise will return "Choose from likes".
        /// </summary>
        /// <param name="workflowInstance"></param>
        /// <param name="stringTemplate"></param>
        /// <returns></returns>
        protected static string FormatStringTemplate(WorkflowInstance workflowInstance, string stringTemplate)
        {
            if (stringTemplate == null)
                return "";

            try
            {
                JArray array = JArray.Parse(stringTemplate);
                StringBuilder returnString = new StringBuilder();

                foreach (var str in array)
                {
                    returnString.Append(ExpandVariables(workflowInstance, str.ToString()));
                    returnString.Append(" ");
                }
                return returnString.ToString().Trim();
            }
            catch (Exception)
            {
                // if the string template is not a JSON-serialized array, then just return the string as-is
                return stringTemplate;
            }
        }

        /// <summary>
        /// Get the value from the instance data bag, stored by key
        /// </summary>
        /// <param name="workflowInstance">Instance to retrieve the data from</param>
        /// <param name="key">Key of the value to return</param>
        /// <returns>Value for the key</returns>
        protected static string GetInstanceData(WorkflowInstance workflowInstance, string key)
        {
            if (key == null)
                return null;
            JsonValue dict = JsonValue.Parse(workflowInstance.InstanceData);
            try
            {
                return (string) dict[key];
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Process activity data (typically created through a user selection)
        /// </summary>
        /// <param name="workflowInstance">Instance to store data into</param>
        /// <param name="data">Data to process</param>
        protected Status ProcessActivityData(WorkflowInstance workflowInstance, object data)
        {
            var suggList = data as List<Suggestion>;
            if (suggList != null)
            {
                // return true if a user has selected an action
                foreach (var sugg in suggList)
                {
                    if (sugg.ReasonSelected == Reasons.Chosen || sugg.ReasonSelected == Reasons.Like)
                    {
                        StoreInstanceData(workflowInstance, OutputParameterName, sugg.Value);
                        StoreInstanceData(workflowInstance, ActivityVariables.LastStateData, sugg.Value);
                        TraceLog.TraceInfo(String.Format("User selected suggestion {0} in group {1} for activity {2}",
                            sugg.DisplayName, sugg.GroupDisplayName, Name));
                        return Status.Complete;
                    }
                }

                // return false if the user hasn't yet selected an action but suggestions were already generated
                // for the current state (we don't want a duplicate set of suggestions)
                return Status.Pending;
            }

            // if the data can't be cast into a suggestion list, there is a serious error - move the workflow forward
            // (otherwise it will be stuck forever)
            TraceLog.TraceError("Data passed in is not a list of suggestions");
            return Status.Error;
        }

        /// <summary>
        /// This method will add a Suggestion with a RefreshEntity FieldName and State to the Suggestions
        /// for this ServerEntity.  By convention, this will tell the UI to refresh the Entity.  This method
        /// is called when an Activity changes the Item (e.g. a DueDate is parsed out of the Name, a Contacts 
        /// FieldName is created, etc).
        /// </summary>
        /// <param name="workflowInstance"></param>
        /// <param name="entity"></param>
        protected void SignalEntityRefresh(WorkflowInstance workflowInstance, ServerEntity entity)
        {
            var sugg = new Suggestion()
            {
                ID = Guid.NewGuid(),
                EntityID = entity.ID,
                EntityType = entity.GetType().Name,
                WorkflowType = workflowInstance.WorkflowType,
                WorkflowInstanceID = workflowInstance.ID,
                State = SuggestionTypes.RefreshEntity,
                SuggestionType = SuggestionTypes.RefreshEntity,
                DisplayName = SuggestionTypes.RefreshEntity,
                GroupDisplayName = SuggestionTypes.RefreshEntity,
                Value = null,
                TimeSelected = null
            };
            SuggestionsContext.Suggestions.Add(sugg);
            SuggestionsContext.SaveChanges();
        }

        /// <summary>
        /// Store a value for a key on the instance data bag
        /// </summary>
        /// <param name="workflowInstance">Instance to retrieve the data from</param>
        /// <param name="key">Key to store under</param>
        /// <param name="data">Data to store under the key</param>
        protected void StoreInstanceData(WorkflowInstance workflowInstance, string key, string data)
        {
            if (key == null)
                return;
            JsonValue dict = JsonValue.Parse(workflowInstance.InstanceData);
            dict[key] = data;
            workflowInstance.InstanceData = dict.ToString();
            SuggestionsContext.SaveChanges();
        }

        /// <summary>
        /// Verify that an item is of a certain ItemType
        /// </summary>
        /// <param name="item">Item to verify</param>
        /// <param name="desiredItemType">Desired item type</param>
        /// <returns>true if check succeeds, false if not</returns>
        protected bool VerifyItemType(Item item, Guid desiredItemType)
        {
            if (item.ItemTypeID != desiredItemType)
            {
                TraceLog.TraceError("Wrong item type");
                return false;  
            }

            return true;
        }

        #region Helpers

        /// <summary>
        /// This function finds the next occurrence of a variable in the format $(varname)
        /// It will attempt to substitute a value from the workflow's instance data, using
        /// varname as the key
        /// </summary>
        /// <param name="workflowInstance">Workflow instance to operate over</param>
        /// <param name="formatString">Format string that contains variables to substitute</param>
        /// <returns>null if the variable wasn't bound, the new string if it was (or if no variables were found)</returns>
        private static string SubstituteNextVariable(WorkflowInstance workflowInstance, string formatString)
        {
            if (formatString == null)
                return null;

            StringBuilder returnString = new StringBuilder();
            int pos = formatString.IndexOf("$(");
            if (pos < 0)
                return formatString;
            int end = formatString.IndexOf(')', pos);
            if (end < 0)
                return formatString;
            
            string variableName = formatString.Substring(pos + 2, Math.Max(0, end - pos - 2));
            returnString.Append(formatString.Substring(0, pos));

            string value = GetInstanceData(workflowInstance, variableName);
            if (value == null)
                return null;  // the variable wasn't bound

            returnString.Append(value);
            returnString.Append(formatString.Substring(end + 1));
            return returnString.ToString();
        }

        #endregion Helpers
    }
}
