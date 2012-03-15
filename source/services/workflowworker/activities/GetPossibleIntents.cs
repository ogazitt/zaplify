using System;
using System.Collections.Generic;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;
using System.Text;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class GetPossibleIntents : WorkflowActivity
    {
        public override string Name { get { return ActivityNames.GetPossibleIntents; } }
        public override string TargetFieldName { get { return FieldNames.Intent; } }
        public override Func<WorkflowInstance, ServerEntity, object, bool> Function
        {
            get
            {
                return ((workflowInstance, entity, data) =>
                {
                    // if the workflow has already computed an intent, store it now and return 
                    string intent = GetInstanceData(workflowInstance, FieldNames.Intent);
                    if (intent != null)
                    {
                        StoreInstanceData(workflowInstance, Workflow.LastStateData, intent);
                        return true;
                    }

                    // check for user input
                    if (data != null)
                    {
                        var suggList = data as List<Suggestion>;
                        if (suggList != null)
                        {
                            // return true if a user has selected an action
                            foreach (var sugg in suggList)
                            {
                                if (sugg.TimeSelected != null)
                                {
                                    StoreInstanceData(workflowInstance, FieldNames.Intent, sugg.Value);
                                    StoreInstanceData(workflowInstance, Workflow.LastStateData, sugg.Value);
                                    return true;
                                }
                            }

                            // return false if the user hasn't yet selected an action but suggestions were already generated
                            // for the current state (we don't want a duplicate set of suggestions)
                            return false;
                        }
                    }

                    // analyze the entity name for possible intents
                    List<string> possibleIntents = new List<string>();
                    bool completed = GetIntents(entity.Name, possibleIntents);

                    // if an intent was deciphered without user input, store it now and return
                    if (completed && possibleIntents.Count == 1)
                    {
                        StoreInstanceData(workflowInstance, Workflow.LastStateData, possibleIntents[0]);
                        StoreInstanceData(workflowInstance, FieldNames.Intent, possibleIntents[0]);
                        return true;
                    }

                    // add suggestions received in possibleIntents
                    try
                    {
                        Suggestion sugg = null;
                        foreach (var s in possibleIntents)
                        {
                            sugg = new Suggestion()
                            {
                                ID = Guid.NewGuid(),
                                EntityID = entity.ID,
                                EntityType = entity.GetType().Name,
                                WorkflowName = workflowInstance.Name,
                                WorkflowInstanceID = workflowInstance.ID,
                                State = workflowInstance.State,
                                FieldName = TargetFieldName, 
                                DisplayName = s,
                                Value = s,
                                TimeSelected = null
                            };
                            WorkflowWorker.SuggestionsContext.Suggestions.Add(sugg);
                        }

                        WorkflowWorker.SuggestionsContext.SaveChanges();
                        return completed;
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceError("GetPossibleIntents Activity failed; ex: " + ex.Message);
                        return false;
                    }
                });
            }
        }

        private bool GetIntents(string name, List<string> possibleIntents)
        {
            // lowercase, remove filler words
            string sentence = name.ToLower();
            foreach (var word in "a;the".Split(';'))
                sentence = sentence.Replace(word, "");
            // remove extra whitespace
            StringBuilder sb = new StringBuilder();
            foreach (var word in sentence.Split(new char[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                sb.AppendFormat("{0} ", word);
            sentence = sb.ToString().Trim();

            string workflow = null;
            bool exists = IntentList.Intents.TryGetValue(sentence, out workflow);
            if (exists)
            {
                possibleIntents.Add(workflow);
                return true;  // exact match
            }

            // populate suggestions by looping over the list of Intents
            // and picking ones that have at least one word in common with the sentence
            foreach (var intent in IntentList.Intents.Keys)
            {
                foreach (var word in sentence.Split(' '))
                {
                    if (intent.Contains(word))
                    {
                        possibleIntents.Add(IntentList.Intents[intent]);
                        break;
                    }
                }
            }

            // inexact match
            return false;
        }
    }
}
