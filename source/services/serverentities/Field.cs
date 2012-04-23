using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class Field
    {
        public Guid ID { get; set; }
        public Guid ItemTypeID { get; set; }
        public string Name { get; set; }
        public string FieldType { get; set; }
        public string DisplayName { get; set; }
        public string DisplayType { get; set; }
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }

        public Field() { }

        public Field(Field field)
        {
            Copy(field);
        }

        public void Copy(Field obj)
        {
            if (obj == null)
                return;

            // copy all of the properties
            foreach (PropertyInfo pi in this.GetType().GetProperties())
            {
                var val = pi.GetValue(obj, null);
                pi.SetValue(this, val, null);
            }
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}