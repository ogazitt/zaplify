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
        public override string GroupDisplayName { get { return "Are you trying to"; } }
        public override string OutputParameterName { get { return ActivityParameters.Intent; } }
        public override string SuggestionType { get { return SuggestionTypes.ChooseOne; } }
        public override Func<WorkflowInstance, ServerEntity, object, Status> Function
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

        private Status GenerateSuggestions(WorkflowInstance workflowInstance, ServerEntity entity, Dictionary<string, string> suggestionList)
        {
            Item item = entity as Item;
            if (item == null)
            {
                TraceLog.TraceError("GenerateSuggestions: non-Item passed in");
                return Status.Error;
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
                if (parts[2] == "for" || parts[2] == "with")
                {
                    // capitalize and store the word following "for" in the SubjectHint workflow parameter
                    subject = parts[3];
                    subject = subject.Substring(0, 1).ToUpper() + subject.Substring(1);
                    StoreInstanceData(workflowInstance, FieldNames.SubjectHint, subject);
                }
            }

            try
            {
                Intent intent = SuggestionsContext.Intents.Single(i => i.Verb == verb && i.Noun == noun);
                try
                {
                    var wt = SuggestionsContext.WorkflowTypes.Single(t => t.Type == intent.WorkflowType);
                    suggestionList[intent.WorkflowType] = wt.Type;
                    return Status.Complete;
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
                var intentList = SuggestionsContext.Intents.Where(i => i.Verb == verb || i.Noun == noun);
                foreach (var intent in intentList)
                    suggestionList[intent.WorkflowType] = intent.WorkflowType;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("GenerateSuggestions: could not find matching intents in Intents table", ex);
                return Status.Error;
            }

            return Status.Pending;
        }
    }
}
