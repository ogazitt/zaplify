using System;
using System.Collections.Generic;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class GetPossibleSubjects : WorkflowActivity
    {
        public override string Name { get { return ActivityNames.GetPossibleSubjects; } }
        public override string TargetFieldName { get { return FieldNames.Contacts; } }
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
                                WorkflowName = workflowInstance.Name,
                                WorkflowInstanceID = workflowInstance.ID,
                                State = workflowInstance.State,
                                FieldName = TargetFieldName, 
                                DisplayName = s,
                                Value = s,
                                TimeChosen = DateTime.Now
                            };
                            WorkflowWorker.SuggestionsContext.Suggestions.Add(sugg);
                            list.Add(sugg.ID);
                        }
                        WorkflowWorker.SuggestionsContext.SaveChanges();
                        return false;
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceError("GetSuggestions Activity failed; ex: " + ex.Message);
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
