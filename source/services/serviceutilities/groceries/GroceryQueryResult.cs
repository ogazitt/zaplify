using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace BuiltSteady.Zaplify.ServiceUtilities.Grocery
{
    public class GroceryQueryResult
    {
        public const string Name = "Name";
        public const string Category = "Category";
        public const string ImageUrl = "ImageUrl";

        JObject jobject = null;

        public GroceryQueryResult()
        {
            jobject = new JObject();
        }

        public GroceryQueryResult(JObject obj)
        {
            if (obj != null)
                jobject = obj;
            else
                jobject = new JObject();
        }

        public static GroceryQueryResult Parse(string str)
        {
            return new GroceryQueryResult(JObject.Parse(str));
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
