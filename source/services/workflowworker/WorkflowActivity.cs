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
            Item, 
            object,    // extra state to send to the execution Function
            List<Guid> // return value: a list of suggestion ID's
            > Function { get; }
    }
}
