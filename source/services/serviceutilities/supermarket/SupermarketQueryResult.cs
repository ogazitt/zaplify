using System.Collections.Generic;
using System.Text;

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
            bool comma = false;
            var sb = new StringBuilder("{\n");
            foreach (var key in properties.Keys)
            {
                if (comma)
                    sb.AppendLine(",");
                else
                    comma = true;
                sb.AppendFormat("   \"{0}\": \"{1}\"", key, properties[key]);
            }
            sb.AppendLine("\n}");
            return sb.ToString();
        }
    }
}
