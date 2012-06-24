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
            return JsonConvert.SerializeObject(body, new JsonSerializerSettings() { DateFormatHandling = DateFormatHandling.MicrosoftDateFormat });
        }
    }

    public class JsonValue 
    {
        JObject jobject = null;

        public Dictionary<string, object> Properties = new Dictionary<string, object>();

        public JsonValue()
        {
            jobject = new JObject();
        }

        public JsonValue(JObject obj)
        {
            jobject = obj;
            foreach (var token in obj)
            {
                switch (token.Value.Type)
                {
                    case JTokenType.Object:
                        Properties[token.Key] = new JsonValue(new JObject(token.Value));
                        break;
                    case JTokenType.Array:
                        JArray list = token.Value as JArray;
                        var query = from jobj in list select new JsonValue(new JObject(jobj));
                        var jlist = new JsonList();
                        jlist.AddRange(query);
                        break;
                    case JTokenType.Null:
                        Properties[token.Key] = null;
                        break;
                    default:
                        Properties[token.Key] = token.Value.ToString();
                        break;
                }
            }
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
                {
                    jobject.Remove(key);
                    Properties.Remove(key);
                }
                jobject.Add(new JProperty(key, value));
            }
        }

        public override string ToString()
        {
            return jobject.ToString();
        }

        public bool IsString
        {
            get
            {
                return jobject.Type == JTokenType.String;
            }
        }
    }

    public class JsonList : List<JsonValue>
    {
    }
}
