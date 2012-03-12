using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.WorkflowWorker.Workflows;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class GetPossibleTasks : WorkflowActivity
    {
        public override string Name { get { return ActivityNames.GetPossibleTasks; } }
        //public override string TargetFieldName { get { return "Task"; } }
        public override Func<WorkflowInstance, Item, object, List<Guid>, bool> Function
        {
            get
            {
                return ((workflowInstance, item, state, list) =>
                {
                    List<string> possibleTasks = new List<string>();
                    if (GetTasks(item.Name, possibleTasks))
                    {
                        // exact match

                        // set the Task type on the Item model
                        return true;
                    }

                    // received a list of suggestions in possibleTasks
                    try
                    {
                        foreach (var s in possibleTasks)
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

        private bool GetTasks(string name, List<string> possibleTasks)
        {
            string sentence = name.ToLower();
            // remove filler words
            foreach (var word in "a;the".Split(';'))
            {
                sentence = sentence.Replace(" " + word + " ", "");
            }

            string workflow = null;
            bool exists = TaskList.Tasks.TryGetValue(sentence, out workflow);
            if (exists)
            {
                possibleTasks.Add(sentence);
                return true;  // exact match
            }

            // populate suggestions by looping over the list of Tasks
            // and picking ones that have at least one word in common with the sentence
            foreach (var task in TaskList.Tasks.Keys)
            {
                foreach (var word in sentence.Split(' '))
                {
                    if (task.Contains(word))
                    {
                        possibleTasks.Add(task);
                        break;
                    }
                }
            }

            // inexact match
            return false;
        }
    }
}
