using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuiltSteady.Zaplify.WorkflowWorker.Workflows
{
    public class WorkflowNames
    {
        public const string NewContact = "New Contact";
        public const string NewFolder = "New Folder";
        public const string NewTask = "New Task";
        public const string NewUser = "New User";
    }

    public class WorkflowList
    {
        public static Dictionary<string, Workflow> Workflows = new Dictionary<string, Workflow>()
        {
        };
    }
}
