using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuiltSteady.Zaplify.WorkflowWorker.Workflows
{
    public class WorkflowNames
    {
        public const string FindTask = "Find Task";
        public const string BuyGift = "Buy Gift";
        public const string NewItem = "New Item";
    }

    public class WorkflowList
    {
        public static Dictionary<string, Workflow> Workflows = new Dictionary<string, Workflow>()
        {
            { WorkflowNames.FindTask, new FindTask() },
            { WorkflowNames.BuyGift, new BuyGift() },
            { WorkflowNames.NewItem, new NewItem() },
        };
    }
}
