using System;
using Newtonsoft.Json.Linq;

namespace BuiltSteady.Zaplify.ServiceUtilities.ADGraph
{
    public class ADQueryResult
    {
        JObject jobject = null;

        public ADQueryResult()
        {
            jobject = new JObject();
        }

        public ADQueryResult(JObject obj)
        {
            if (obj != null)
                jobject = obj;
            else
                jobject = new JObject();
        }

        public DateTime Birthday { get { return (DateTime)jobject["Birthday"]; } }
        public string FirstName { get { return (string)jobject["FirstName"]; } }
        public string LastName { get { return (string)jobject["LastName"]; } }
        public string Name { get { return string.Format("{0} {1}", FirstName, LastName); } }
        public string Title { get { return (string)jobject["Title"]; } }

        public static ADQueryResult Parse(string str)
        {
            return new ADQueryResult(JObject.Parse(str));
        }

        public string this[string key]
        {
            get
            {
                return (string) jobject[key];
            }
        }

        public override string ToString()
        {
            return jobject.ToString();
        }
    }
}
