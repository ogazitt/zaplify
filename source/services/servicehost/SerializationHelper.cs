using Newtonsoft.Json;

namespace BuiltSteady.Zaplify.ServiceHost
{
    public class SerializationHelper
    {
        public static string JsonSerialize(object body)
        {
            return JsonConvert.SerializeObject(body);
        }
    }
}
