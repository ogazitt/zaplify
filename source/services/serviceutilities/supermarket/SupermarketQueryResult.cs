using System.Collections.Generic;

namespace BuiltSteady.Zaplify.ServiceUtilities.Supermarket
{
    public class SupermarketQueryResult
    {
        public const string Name = "Name";
        public const string Category = "Category";
        public const string Description = "Description";
        public const string ID = "ID";
        public const string Image = "Image";
        public const string Aisle = "Aisle";

        Dictionary<string, string> properties = new Dictionary<string, string>();

        public string this[string key]
        {
            get
            {
                string retval = null;
                if (properties.TryGetValue(key, out retval) == false)
                    return null;
                return retval;
            }
            set
            {
                properties[key] = value;
            }
        }

        public override string ToString()
        {
            return properties.ToString();
        }
    }
}
