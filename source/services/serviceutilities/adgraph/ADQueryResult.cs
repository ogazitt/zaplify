using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

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

        public DateTime? Birthday { get { return (DateTime?)jobject["Birthday"]; } }
        public string FirstName { get { return (string)jobject["FirstName"]; } }
        public string LastName { get { return (string)jobject["LastName"]; } }
        public string Name { get { return string.Format("{0} {1}", FirstName, LastName); } }
        public string Title { get { return (string)jobject["Title"]; } }

        public List<ADQueryResultValue> IDs
        {
            get
            {
                JArray list = jobject["Sources"]["results"] as JArray;
                if (list != null)
                    return list.Select(val => new ADQueryResultValue() { Value = (string)val["SourceId"], Source = (string)val["SourceService"] }).ToList();
                else
                    return null;
            }
        }

        public List<ADQueryResultValue> Relationships
        {
            get
            {
                JArray list = jobject["Sources"]["results"] as JArray;
                if (list != null)
                    return list.Select(val => new ADQueryResultValue() { Value = (string)val["Relationship"], Source = (string)val["SourceService"] }).ToList();
                else
                    return null;
            }
        }

        public static ADQueryResult Parse(string str)
        {
            return new ADQueryResult(JObject.Parse(str));
        }

        public string this[string key]
        {
            get
            {
                return (string)jobject[key];
            }
        }

        public override string ToString()
        {
            return jobject.ToString();
        }
    }

    public class ADQueryResultValue
    {
        public const string FacebookSource = "Facebook";
        public const string DirectorySource = "Directory";

        public string Source { get; set; }
        public string Value { get; set; }
    }
}
