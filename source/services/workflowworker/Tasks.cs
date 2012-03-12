using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.WorkflowWorker.Workflows;

namespace BuiltSteady.Zaplify.WorkflowWorker
{
    public class TaskNames
    {
        public const string BuyGift = "buy gift";
        public const string ChangeOil = "change oil";
        public const string CleanGutters = "clean gutters";
    }

    public class TaskList
    {
        public static Dictionary<string, string> Tasks = new Dictionary<string, string>()
        {
            { TaskNames.BuyGift, WorkflowNames.BuyGift },
            { TaskNames.ChangeOil, WorkflowNames.BuyGift },
            { TaskNames.CleanGutters, WorkflowNames.BuyGift },
        };
    }
}
