using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BuiltSteady.Zaplify.ServiceHost.Nlp
{
    public sealed class Tokenizer
    {
        #region Tokenization

        public List<string> Tokenize(string str)
        {
            List<string> tokens = new List<string>();

            Regex reg = new Regex(@"(\S+)\s*");
            MatchCollection matches = reg.Matches(str);

            foreach (Match match in matches)
            {
                if (match.Length > 0)
                {
                    string s = match.Value.Trim();

                    if (s.EndsWith(";") || s.EndsWith(",") ||
                        s.EndsWith("?") || s.EndsWith(")") ||
                        s.EndsWith(":") || s.EndsWith("."))
                    {
                        s = s.Substring(0, s.Length - 1);
                    }

                    tokens.Add(s);
                }
            }

            return tokens;
        }

        #endregion Tokenization
    }
}
