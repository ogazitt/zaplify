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
        public override Func<WorkflowInstance, ServerEntity, object, List<Guid>, bool> Function
        {
            get
            {
                return ((workflowInstance, entity, data, list) =>
                {
                    // check for user data
                    if (data != null)
                    {
                        workflowInstance.InstanceData = (string)data;
                        return true;
                    }

                    List<string> possibleIntents = new List<string>();
                    bool completed = GetIntents(entity.Name, possibleIntents);

                    try
                    {
                        // add suggestions received in possibleIntents
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
                            list.Add(sugg.ID);
                        }

                        // if an exact match, set the TimeChosen to indicate the match
                        if (completed && possibleIntents.Count == 1)
                        {
                            sugg.TimeSelected = DateTime.UtcNow;
                            workflowInstance.InstanceData = sugg.Value;
                        }
                        WorkflowWorker.SuggestionsContext.SaveChanges();
                        return completed;
                    }
                    catch (Exception ex)
                    {
                        TraceLog.TraceError("GetSuggestions Activity failed; ex: " + ex.Message);
                    }
                    return false;
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
