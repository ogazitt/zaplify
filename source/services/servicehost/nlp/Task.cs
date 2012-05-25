using System.Collections.Generic;

namespace BuiltSteady.Zaplify.ServiceHost.Nlp
{
    public class Task
    {
        #region Properties

        private readonly string _verb;
        public string Verb { get { return _verb; } }

        public string Article { get; private set; }
        public string Subject { get; private set; }

        #endregion Properties

        #region Constructor

        public Task(string verb)
        {
            _verb = verb;
        }

        #endregion Constructor

        #region Processing

        internal virtual void Process(List<string> words, List<string> tags)
        {
            // Copy lists so we can modify them without breaking caller.
            List<string> w = new List<string>(words);
            List<string> t = new List<string>(tags);

            // Strip the task verb
            t.RemoveAt(0);
            w.RemoveAt(0);

            // NOTE:  The ordering here matters.
            Subject = FindSubject(w, t);
            Article = FindArticle(w, t);
        }

        protected string FindArticle(List<string> w, List<string> t)
        {
            for (int i = 0; i < t.Count; ++i)
            {
                if ((t[i] == Tagger.Tags.Noun) || (t[i] == Tagger.Tags.NounPlural))
                {
                    if ((i > 0) && (t[i - 1] == Tagger.Tags.Determiner))
                    {
                        t.RemoveAt(i - 1);
                        w.RemoveAt(i - 1);
                        --i;
                    }

                    string s = w[i];
                    t.RemoveAt(i);
                    w.RemoveAt(i);

                    while ((i < t.Count) && ((t[i] == Tagger.Tags.Noun) || (t[i] == Tagger.Tags.NounPlural)))
                    {
                        s += " " + w[i];
                        t.RemoveAt(i);
                        w.RemoveAt(i);
                    }

                    return s;
                }
            }

            return string.Empty;
        }

        protected string FindSubject(List<string> w, List<string> t)
        {
            for (int i = 0; i < t.Count; ++i)
            {
                if (t[i] == Tagger.Tags.NounProper)
                {
                    if ((i > 0) && (t[i - 1] == Tagger.Tags.Preposition))
                    {
                        t.RemoveAt(i - 1);
                        w.RemoveAt(i - 1);
                        --i;
                    }

                    string s = w[i];
                    t.RemoveAt(i);
                    w.RemoveAt(i);

                    while ((i < t.Count) && (t[i] == Tagger.Tags.NounProper))
                    {
                        s += " " + w[i];
                        t.RemoveAt(i);
                        w.RemoveAt(i);
                    }

                    return s;
                }
            }

            return string.Empty;
        }

        #endregion Processing
    }
}
