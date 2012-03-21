using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.WorkflowWorker.Workflows;

namespace BuiltSteady.Zaplify.WorkflowWorker
{
    public class IntentNames
    {
        public const string BuyGift = "buy gift";
        public const string ChangeOil = "change oil";
        public const string CleanGutters = "clean gutters";
        public const string FakeBuyGift = "fake buy gift";
    }

    public class IntentList
    {
        public static Dictionary<string, string> Intents = new Dictionary<string, string>()
        {
            { IntentNames.BuyGift, WorkflowNames.BuyGift },
            { IntentNames.ChangeOil, WorkflowNames.BuyGift },
            { IntentNames.CleanGutters, WorkflowNames.BuyGift },
            { IntentNames.FakeBuyGift, WorkflowNames.FakeBuyGift },
        };
    }
}
