using System;
using System.Linq;
using System.Collections.Generic;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.Shared.Entities;
using System.Text;
using BuiltSteady.Zaplify.WorkflowWorker.Workflows;

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
            string sentence = name.ToLower();

            // remove extra whitespace and filler words
            StringBuilder sb = new StringBuilder();
            foreach (var word in sentence.Split(new char[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                bool add = true;
                foreach (var filler in "a;an;the".Split(';'))
                    if (word == filler)
                    {
                        add = false;
                        break;
                    }
                if (add)
                    sb.AppendFormat("{0} ", word);
            }
            sentence = sb.ToString().Trim();

            // poor man's NLP - assume first word is verb, second word is noun
            string[] parts = sentence.Split(' ');
            string verb = null;
            string noun = null;
            string subject = null;
            if (parts.Length >= 2)
            {
                verb = parts[0];
                noun = parts[1];
            }
            if (parts.Length >= 4)
            {
                if (parts[2] == "for")
                {
                    subject = parts[3];
                    StoreInstanceData(workflowInstance, FieldNames.SubjectHint, subject);
                }
            }

            try
            {
                Intent intent = WorkflowWorker.SuggestionsContext.Intents.Single(i => i.Verb == verb && i.Noun == noun);
                try
                {
                    var wt = WorkflowWorker.SuggestionsContext.WorkflowTypes.Single(t => t.Type == intent.Name);
                    suggestionList[intent.Name] = wt.Type;
                    return true;  // exact match
                }
                catch (Exception ex)
                {
                    TraceLog.TraceException("GenerateSuggestions: could not find or deserialize workflow definition", ex);
                    // try to recover by falling through the block below and generating intent suggestions for the user
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
                    suggestionList[intent.Name] = intent.Name;
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
