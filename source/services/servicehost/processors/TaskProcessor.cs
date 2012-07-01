using System;
using System.Linq;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.ServiceHost.Nlp;

namespace BuiltSteady.Zaplify.ServiceHost
{
    public class TaskProcessor : ItemProcessor
    {
        public TaskProcessor(User user, UserStorageContext storage)
        {
            this.user = user;
            this.storage = storage;
        }

        public override bool ProcessCreate(Item item)
        {
            return base.ProcessCreate(item);
            // kick off workflow?
        }

        public override bool ProcessUpdate(Item oldItem, Item newItem)
        {
            // base handles ItemType changing
            if (base.ProcessUpdate(oldItem, newItem))
                return true;

            if (newItem.Name != oldItem.Name)
            {   // name changed, process like new item
                ProcessCreate(newItem);
                return true;
            }
            // kick off workflow?
            return false;
        }

        protected override string ExtractIntent(Item item)
        {
            try
            {
                Phrase phrase = new Phrase(item.Name);
                if (phrase.Task != null)
                {
                    string verb = phrase.Task.Verb.ToLower();
                    string noun = phrase.Task.Article.ToLower();
                    Intent intent = Storage.NewSuggestionsContext.Intents.FirstOrDefault(i => i.Verb == verb && i.Noun == noun);
                    if (intent != null)
                        return intent.WorkflowType;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Could not initialize NLP engine", ex);
            }
            return base.ExtractIntent(item);
        }
    }

}
