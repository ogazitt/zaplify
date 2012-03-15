using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;

namespace BuiltSteady.Zaplify.WorkflowWorker
{
    public abstract class WorkflowActivity
    {
        public abstract string Name { get; }
        public abstract string TargetFieldName { get; }
        public abstract Func<
            WorkflowInstance, 
            ServerEntity, // item to operate over
            object,       // extra state to send to the execution Function
            bool          // true for "completed", false for "not completed" (needs user input)
            > Function { get; }

        /// <summary>
        /// Get the value from the instance data bag, stored by key
        /// </summary>
        /// <param name="workflowInstance">Instance to retrieve the data from</param>
        /// <param name="key">Key of the value to return</param>
        /// <returns>Value for the key</returns>
        public string GetInstanceData(WorkflowInstance workflowInstance, string key)
        {
            JsonValue dict = JsonValue.Parse(workflowInstance.InstanceData);
            try
            {
                return dict[key];
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        /// <summary>
        /// Store a value for a key on the instance data bag
        /// </summary>
        /// <param name="workflowInstance">Instance to retrieve the data from</param>
        /// <param name="key">Key to store under</param>
        /// <param name="data">Data to store under the key</param>
        public void StoreInstanceData(WorkflowInstance workflowInstance, string key, string data)
        {
            JsonValue dict = JsonValue.Parse(workflowInstance.InstanceData);
            dict[key] = data;
            workflowInstance.InstanceData = dict.ToString();
            WorkflowWorker.SuggestionsContext.SaveChanges();
        }
    }
}
