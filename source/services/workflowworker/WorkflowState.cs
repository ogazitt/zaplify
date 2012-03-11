using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;

namespace BuiltSteady.Zaplify.WorkflowWorker
{
    public class WorkflowState
    {
        public string Name { get; set; }
        public string Activity { get; set; }
        public string NextState { get; set; }
    }
}
