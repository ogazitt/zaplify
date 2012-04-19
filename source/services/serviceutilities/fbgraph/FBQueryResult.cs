using Newtonsoft.Json.Linq;

namespace BuiltSteady.Zaplify.ServiceUtilities.FBGraph
{
    public class FBQueryResult
    {
        public const string Birthday = "birthday";
        public const string Gender = "gender";
        public const string ID = "id";
        public const string Link = "link";
        public const string Name = "name";
        public const string Website = "website";

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
