using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.WorkflowWorker.Workflows;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class GetPossibleSubjects : WorkflowActivity
    {
        public override string Name { get { return ActivityNames.GetPossibleTasks; } }
        //public override string TargetFieldName { get { return "Task"; } }
        public override Func<WorkflowInstance, Item, object, List<Guid>, bool> Function
        {
            get
            {
                return ((workflowInstance, item, state, list) =>
                {
                    List<string> possibleSubjects = new List<string>();
                    if (GetSubjects(item.Name, possibleSubjects))
                    {
                        // exact match

                        // set the Contacts on the Item model
                        return true;
                    }

                    // received a list of suggestions in possibleSubjects
                    try
                    {
                        foreach (var s in possibleSubjects)
                        {
                            var sugg = new Suggestion()
                            {
                                ID = Guid.NewGuid(),
                                ItemID = item.ID,
                                Type = "Text",
                                Name = s,
                                Value = null,
                                Retrieved = false,
                                Created = DateTime.Now
                            };
                            WorkflowWorker.StorageContext.Suggestions.Add(sugg);
                            list.Add(sugg.ID);
                        }
                        WorkflowWorker.StorageContext.SaveChanges();
                        return false;
                    }
                    catch (Exception ex)
                    {
                        LoggingHelper.TraceError("GetSuggestions Activity failed; ex: " + ex.Message);
                    }
                    return false;
                });
            }
        }

        private bool GetSubjects(string name, List<string> possibleSubjects)
        {
            foreach (var subject in "Mike Maples;Mike Smith;Mike Abbott".Split(';'))
            {
                possibleSubjects.Add(subject);
            }

            // inexact match
            return false;
        }
    }
}
