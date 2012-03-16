using System;
using System.Linq;
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
                    return Execute(
                        workflowInstance,
                        entity,
                        data,
                        SystemItemTypes.Task,
                        (instance, e, dict) => { return GenerateSuggestions(instance, e, dict); });
                });
            }
        }

        private bool GenerateSuggestions(WorkflowInstance workflowInstance, ServerEntity entity, Dictionary<string, string> suggestionList)
        {
            Item item = entity as Item;
            if (item == null)
            {
                TraceLog.TraceError("GenerateSuggestions: non-Item passed in");
                return true;  // this will terminate the state
            }

            string name = item.Name;
            // lowercase, remove filler words
            string sentence = name.ToLower();
            foreach (var word in "a;the".Split(';'))
                sentence = sentence.Replace(word, "");
            // remove extra whitespace
            StringBuilder sb = new StringBuilder();
            foreach (var word in sentence.Split(new char[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                sb.AppendFormat("{0} ", word);
            sentence = sb.ToString().Trim();

            // poor man's NLP - assume first word is verb, second word is noun
            string[] parts = sentence.Split(' ');
            string verb = parts[0];
            string noun = parts[1];

            try
            {
                Intent intent = WorkflowWorker.SuggestionsContext.Intents.Single(i => i.Verb == verb && i.Noun == noun);
                string workflow = null;
                bool exists = IntentList.Intents.TryGetValue(intent.Name, out workflow);
                if (exists)
                {
                    suggestionList[intent.Name] = workflow;
                    return true;  // exact match
                }
            }
            catch (Exception)
            {
                // this is not an error - the intent wasn't matched precisely
            }

            try
            {
                // get a list of all approximate matches
                var intentList = WorkflowWorker.SuggestionsContext.Intents.Where(i => i.Verb == verb || i.Noun == noun);
                foreach (var intent in intentList)
                    suggestionList[intent.Name] = IntentList.Intents[intent.Name];
            }
            catch (Exception ex)
            {
                TraceLog.TraceError("GenerateSuggestions: could not find database intent in IntentList dictionary; ex: " + ex.Message);
            }

            // inexact match
            return false;
        }
    }
}
