using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;

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
            List<Guid>,   // an empty list of suggestion ID's that the function can populate
            bool          // true for "completed", false for "not completed" (needs user input)
            > Function { get; }
    }
}
