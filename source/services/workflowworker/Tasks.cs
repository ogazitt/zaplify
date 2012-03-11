using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.WorkflowWorker.Workflows;

namespace BuiltSteady.Zaplify.WorkflowWorker
{
    public class TaskNames
    {
        public const string BuyGift = "Buy a Gift";
        public const string ChangeOil = "Change the Oil";
        public const string CleanGutters = "Clean the Gutters";
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
