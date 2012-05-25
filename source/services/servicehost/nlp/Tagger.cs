using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace BuiltSteady.Zaplify.ServiceHost.Nlp
{
    public sealed class Tagger
    {
        #region Mapping table

        private static Dictionary<string, string> _mapping = null;
        private static Dictionary<string, string> Mapping
        {
            get
            {
                if (_mapping == null)
                    LoadMappingTable();

                return _mapping;
            }
        }

        private static void LoadMappingTable()
        {
            try
            {
                using (Stream stream = File.Open(HostEnvironment.LexiconFileName, FileMode.Open, FileAccess.Read))
                {
                    IFormatter formatter = new BinaryFormatter();
                    _mapping = formatter.Deserialize(stream) as Dictionary<string, string>;
                }
            }
            catch (Exception ex)
            {
                TraceLog.TraceException(String.Format("LoadMappingTable: could not open or deserialize lexicon file {0}", HostEnvironment.LexiconFileName), ex);
                throw;
            }
        }

        #endregion Mapping table

        #region Tagging

        public List<string> Tag(List<string> words)
        {
            List<string> tags = new List<string>();

            foreach (string word in words)
            {
                string tag;

                // 1/22/2002 mod (from Lisp code): if not in hash, try lower case:
                if (Mapping.TryGetValue(word, out tag) || Mapping.TryGetValue(word.ToLower(), out tag))
                {
                    int index = tag.IndexOf(" ");
                    if (index > -1)
                        tag = tag.Substring(0, index).Trim();
                }
                else
                    tag = Tags.Noun;  // default

                tags.Add(tag);
            }

            /**
             * Apply transformational rules
             **/
            for (int i = 0; i < words.Count; ++i)
            {
                //  rule: DT, {VBD | VBP} --> DT, NN
                if ((i > 0) && tags[i - 1].Equals(Tags.Determiner))
                {
                    if (tags[i].Equals(Tags.VerbPastTense)
                        || tags[i].Equals(Tags.VerbSingularPresent)
                        || tags[i].Equals(Tags.Verb))
                    {
                        tags[i] = Tags.Noun;
                    }
                }

                // rule: convert a noun to a number (CD) if "." appears in the word
                if (tags[i].StartsWith("N"))
                {
                    if (words[i].IndexOf(".") > -1)
                        tags[i] = Tags.CardinalNumber;
                }

                // rule(RA): if an adjective is preceded by a determiner and not succeeded by a
                //       noun or another adjective, it is likely a noun
                if (tags[i].Equals(Tags.Adjective) && (i > 0) && tags[i - 1].Equals(Tags.Determiner) &&
                    (i < tags.Count) && !tags[i + 1].StartsWith("NN") && !tags[i + 1].Equals(Tags.Adjective))
                    tags[i] = Tags.Noun;

                // rule(RA): if an adjective is the first word, it is likely a verb.
                if ((i == 0) && (words.Count > 1))
                    tags[i] = Tags.Verb;

                // rule: convert a noun to a past participle if words[i] ends with "ed"
                if (tags[i].StartsWith("N") && words[i].EndsWith("ed"))
                    tags[i] = Tags.VerbPastParticiple;

                // rule: convert any type to adverb if it ends in "ly";
                if (words[i].EndsWith("ly"))
                    tags[i] = Tags.Adverb;

                // rule: convert a common noun (NN or NNS) to a adjective if it ends with "al"
                if (tags[i].StartsWith("NN") && words[i].EndsWith("al"))
                    tags[i] = Tags.Adjective;

                // rule: convert a noun to a verb if the preceding work is "would"
                if ((i > 0) && tags[i].StartsWith("NN") && words[i - 1].Equals("would", StringComparison.OrdinalIgnoreCase))
                    tags[i] = Tags.Verb;

                // rule: if a word has been categorized as a common noun and it ends with "s",
                //       then set its type to plural common noun (NNS)
                if (tags[i].Equals(Tags.Noun) && words[i].EndsWith("s"))
                    tags[i] = Tags.NounPlural;

                // rule: convert a common noun to a present participle verb (i.e., a gerund)
                if (tags[i].StartsWith("NN") && words[i].EndsWith("ing"))
                    tags[i] = Tags.VerbPresentParticiple;
            }

            return tags;
        }

        #endregion Tagging

        #region Tags

        public static class Tags
        {
            public const string CoordinatingConjunction = "CC";     // Coordinating conjunction
            public const string CardinalNumber = "CD";              // Cardinal number
            public const string Determiner = "DT";                  // Determiner
            public const string Existential = "EX";                 // Existential there
            public const string ForeignWork = "FW";                 // Foreign word
            public const string Preposition = "IN";                 // Preposition or subordinating conjunction
            public const string Adjective = "JJ";                   // Adjective
            public const string AdjectiveComparitive = "JJR";       // Adjective, comparative
            public const string AdjectiveSuperlative = "JJS";       // Adjective, superlative
            public const string ListItemMarker = "LS";              // List item marker
            public const string Modal = "MD";                       // Modal
            public const string Noun = "NN";                        // Noun, singular or mass
            public const string NounPlural = "NNS";                 // Noun, plural
            public const string NounProper = "NNP";                 // Proper noun, singular
            public const string NounProperPlural = "NNPS";          // Proper noun, plural
            public const string Predeterminer = "PDT";              // Predeterminer
            public const string PossessiveEnding = "POS";           // Possessive ending
            public const string Pronoun = "PRP";                    // Personal pronoun
            public const string PronounPossessive = "PRP$";         // Possessive pronoun
            public const string Adverb = "RB";                      // Adverb
            public const string AdverbComparative = "RBR";          // Adverb, comparative
            public const string AdverbSuperlative = "RBS";          // Adverb, superlative
            public const string Particle = "RP";                    // Particle
            public const string Symbol = "SYM";                     // Symbol
            public const string To = "TO";                          // to
            public const string Interjection = "UH";                // Interjection
            public const string Verb = "VB";                        // Verb, base form
            public const string VerbPastTense = "VBD";              // Verb, past tense
            public const string VerbPresentParticiple = "VBG";      // Verb, gerund or present participle
            public const string VerbPastParticiple = "VBN";         // Verb, past participle
            public const string VerbSingularPresent = "VBP";        // Verb, non-3rd person singular present
            public const string Verb3rdPersonPresent = "VBZ";       // Verb, 3rd person singular present
            public const string WhDeterminer = "WDT";               // Wh-determiner
            public const string WhPronoun = "WP";                   // Wh-pronoun
            public const string WhPronounPossessice = "WP$";        // Possessive wh-pronoun
            public const string WhAdverb = "WRB";                   // Wh-adverb
        }

        #endregion Tags
    }
}