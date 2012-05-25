using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost;
using BuiltSteady.Zaplify.ServiceHost.Nlp;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.WorkflowWorker.Activities
{
    public class GetPossibleIntents : WorkflowActivity
    {
        public override string GroupDisplayName { get { return "Are you trying to"; } }
        public override string OutputParameterName { get { return ActivityVariables.Intent; } }
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

            // if an intent was already matched, return it now
            var intentFV = item.GetFieldValue(FieldNames.Intent);
            if (intentFV != null && !String.IsNullOrWhiteSpace(intentFV.Value))
            {
                var wt = SuggestionsContext.WorkflowTypes.FirstOrDefault(t => t.Type == intentFV.Value);
                if (wt != null)
                {
                    suggestionList[wt.Type] = wt.Type;
                    return Status.Complete;
                }
            }

            // run NLP engine over the task name to extract intent (verb / noun) as well as a subject hint
            string name = item.Name;
            string verb = null;
            string noun = null;
            string subject = null;
            try
            {
                Phrase phrase = new Phrase(name);
                if (phrase.Task != null)
                {
                    verb = phrase.Task.Verb;
                    noun = phrase.Task.Article;
                    subject = phrase.Task.Subject;
                    if (!String.IsNullOrWhiteSpace(subject))
                        StoreInstanceData(workflowInstance, ActivityVariables.SubjectHint, subject);
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("GenerateSuggestions: could not initialize NLP engine", ex);
            }

            // if NLP failed (e.g. data file not found), do "poor man's NLP" - assume a structure like <verb> <noun> [{for,with} <subject>]
            if (verb == null)
            {
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
                        StoreInstanceData(workflowInstance, ActivityVariables.SubjectHint, subject);
                    }
                }
            }

            // try to find an intent that exactly matches the verb/noun extracted by the NLP step
            Intent intent = SuggestionsContext.Intents.FirstOrDefault(i => i.Verb == verb && i.Noun == noun);
            if (intent != null)
            {
                var wt = SuggestionsContext.WorkflowTypes.FirstOrDefault(t => t.Type == intent.WorkflowType);
                if (wt != null)
                {
                    suggestionList[intent.WorkflowType] = wt.Type;
                    return Status.Complete;
                }
            }

            // get a list of all approximate matches and surface as suggestions to the user
            var intentList = SuggestionsContext.Intents.Where(i => i.Verb == verb || i.Noun == noun);
            foreach (var i in intentList)
                suggestionList[i.WorkflowType] = intent.WorkflowType;

            // if there are no suggestions, we can terminate this workflow
            if (suggestionList.Count == 0)
            {
                TraceLog.TraceError("GenerateSuggestions: no possible intents were found; terminating workflow");
                return Status.Error;
            }

            return Status.Pending;
        }
    }
}
