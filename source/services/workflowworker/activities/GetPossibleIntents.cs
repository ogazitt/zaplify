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
                    bool completed = GetIntents(item.Name, possibleIntents);

                    try
                    {
                        // add suggestions received in possibleIntents
                        Suggestion sugg = null;
                        foreach (var s in possibleIntents)
                        {
                            sugg = new Suggestion()
                            {
                                ID = Guid.NewGuid(),
                                ItemID = item.ID,
                                WorkflowName = workflowInstance.Name,
                                WorkflowInstanceID = workflowInstance.ID,
                                State = workflowInstance.State,
                                FieldName = TargetFieldName, 
                                DisplayName = s,
                                Value = s,
                                TimeChosen = null
                            };
                            WorkflowWorker.StorageContext.Suggestions.Add(sugg);
                            list.Add(sugg.ID);
                        }

                        // if an exact match, set the TimeChosen to indicate the match
                        if (completed && possibleIntents.Count == 1)
                            sugg.TimeChosen = DateTime.Now;
                        WorkflowWorker.StorageContext.SaveChanges();
                        return completed;
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
