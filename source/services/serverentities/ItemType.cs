using System;
using System.Collections.Generic;
using System.Reflection;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class ItemType
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public Guid? UserID { get; set; }
        public string Icon { get; set; }

        public List<Field> Fields { get; set; }

        public ItemType() { }
        public ItemType(ItemType itemType)
        {
            Copy(itemType);
        }

        public void Copy(ItemType obj)
        {
            if (obj == null)
                return;

            // copy all of the properties
            foreach (PropertyInfo pi in this.GetType().GetProperties())
            {
                if (pi.Name == "Fields")
                    continue;
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