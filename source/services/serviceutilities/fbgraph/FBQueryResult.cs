using Newtonsoft.Json.Linq;

namespace BuiltSteady.Zaplify.ServiceUtilities.FBGraph
{
    public class FBQueryResult
    {
        JObject jobject = null;

        public FBQueryResult()
        {
            jobject = new JObject();
        }

        public FBQueryResult(JObject obj)
        {
            if (obj != null)
                jobject = obj;
            else
                jobject = new JObject();
        }

        public string Name
        {
            get
            {
                return (string)jobject["name"];
            }
        }

        public string ID
        {
            get
            {
                return (string)jobject["id"];
            }
        }

        public static FBQueryResult Parse(string str)
        {
            return new FBQueryResult(JObject.Parse(str));
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
