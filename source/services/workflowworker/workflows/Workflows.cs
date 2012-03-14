using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuiltSteady.Zaplify.WorkflowWorker.Workflows
{
    public class WorkflowNames
    {
        public const string ContactsChanged = "Contacts Changed";
        public const string BuyGift = "Buy Gift";
        public const string FindIntent = "Find Intent";
        public const string NameChanged = "Name Changed";
        public const string NewItem = "New Item";
        public const string NewUser = "New User";
    }

    public class WorkflowList
    {
        public static Dictionary<string, Workflow> Workflows = new Dictionary<string, Workflow>()
        {
            { WorkflowNames.BuyGift, new BuyGift() },
            { WorkflowNames.ContactsChanged, new ContactsChanged() },
            { WorkflowNames.FindIntent, new FindIntent() },
            { WorkflowNames.NameChanged, new NameChanged() },
            { WorkflowNames.NewItem, new NewItem() },
            { WorkflowNames.NewUser, new NewUser() },
        };
    }
}
