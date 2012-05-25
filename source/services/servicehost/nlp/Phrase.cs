using System.Collections.Generic;

namespace BuiltSteady.Zaplify.ServiceHost.Nlp
{
    public sealed class Phrase
    {
        #region Private data

        private static readonly Tokenizer tokenizer = new Tokenizer();
        private static readonly Tagger tagger = new Tagger();

        private static readonly Dictionary<string, int> Tasks = new Dictionary<string, int>();

        #endregion Private data

        #region Properties

        private readonly string _phraseText;
        public string PhraseText { get { return _phraseText; } }

        public Task Task { get; private set; }

        #endregion Properties

        #region Constructor

        public Phrase(string phrase)
        {
            _phraseText = phrase;

            Process();
        }

        #endregion Constructor

        #region Phrase processing

        public void Process()
        {
            List<string> words = tokenizer.Tokenize(PhraseText);

            // HACKHACK:  Given the way this system will work, I am assuming that the first
            //            word of the phrase will always be a verb of some form.  Therefore,
            //            it is not a proper noun so will not be hurt by getting lowercased.
            //    The reason for this hack is to workaround a lexical issue in the tagger.
            //if (words.Count > 0)
            //    words[0] = words[0].ToLower();

            List<string> tags = tagger.Tag(words);

            //for (int i = 0; i < words.Count; ++i)
            //    Console.Write("{0}/{1} ", words[i], tags[i]);
            //Console.WriteLine("\r\n========");

            if (tags[0] == Tagger.Tags.Verb)
            {
                Task = new Task(words[0]);
                Task.Process(words, tags);
            }
            else
            {
                // TODO:  Probably should do something here.
                //        Right now, code assumes first tag is a verb.
            }
        }

        #endregion Phrase processing
    }
}
