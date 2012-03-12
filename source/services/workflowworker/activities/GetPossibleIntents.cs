using System;
using System.Collections.Generic;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class GetPossibleIntents : WorkflowActivity
    {
        public override string Name { get { return ActivityNames.GetPossibleIntents; } }
        public override string TargetFieldName { get { return FieldNames.Intent; } }
        public override Func<WorkflowInstance, Item, object, List<Guid>, bool> Function
        {
            get
            {
                return ((workflowInstance, item, state, list) =>
                {
                    List<string> possibleIntents = new List<string>();
                    if (GetIntents(item.Name, possibleIntents))
                    {
                        // exact match

                        // set the Intent type on the Item model
                        return true;
                    }

                    // received a list of suggestions in possibleIntents
                    try
                    {
                        foreach (var s in possibleIntents)
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

        private bool GetIntents(string name, List<string> possibleIntents)
        {
            string sentence = name.ToLower();
            // remove filler words
            foreach (var word in "a;the".Split(';'))
            {
                sentence = sentence.Replace(" " + word + " ", "");
            }

            string workflow = null;
            bool exists = IntentList.Intents.TryGetValue(sentence, out workflow);
            if (exists)
            {
                possibleIntents.Add(sentence);
                return true;  // exact match
            }

            // populate suggestions by looping over the list of Intents
            // and picking ones that have at least one word in common with the sentence
            foreach (var task in IntentList.Intents.Keys)
            {
                foreach (var word in sentence.Split(' '))
                {
                    if (task.Contains(word))
                    {
                        possibleIntents.Add(task);
                        break;
                    }
                }
            }

            // inexact match
            return false;
        }
    }
}
