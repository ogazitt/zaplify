using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BuiltSteady.Zaplify.ServiceHost
{
    public class JsonSerializer
    {
        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static string Serialize(object body)
        {
            return JsonConvert.SerializeObject(body);
        }
    }

    public class JsonValue
    {
        JObject jobject = null;

        public JsonValue()
        {
            jobject = new JObject();
        }

        public JsonValue(JObject obj)
        {
            jobject = obj;
        }

        public static JsonValue Parse(string str)
        {
            return new JsonValue(JObject.Parse(str));
        }

        public object this[string key]
        {
            get
            {
                object obj = jobject[key];
                JArray list = obj as JArray;
                if (list != null)
                {
                    var query = from jobj in list select new JsonValue(new JObject(jobj));
                    var jlist = new JsonList();
                    jlist.AddRange(query);
                    return jlist;
                }

                return (string)jobject[key];
            }
            set
            {
                if (jobject[key] != null)
                    jobject.Remove(key);
                jobject.Add(new JProperty(key, value));
            }
        }

        public override string ToString()
        {
            return jobject.ToString();
        }
    }

    public class JsonList : List<JsonValue>
    {
    }
}
