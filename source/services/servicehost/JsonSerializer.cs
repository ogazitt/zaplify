using Newtonsoft.Json;

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
}
