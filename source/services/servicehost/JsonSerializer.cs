using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

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

        public string this[string key]
        {
            get
            {
                return (string) jobject[key];
            }
            set
            {
                jobject.Add(key, new JProperty(key, value));
            }
        }

        public override string ToString()
        {
            return jobject.ToString();
        }
    }
}
