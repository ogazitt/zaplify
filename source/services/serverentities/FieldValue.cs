using System;
using System.Reflection;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class FieldValue
    {
        public long ID { get; set; }
        public string FieldName { get; set; }
        public Guid ItemID { get; set; }
        public string Value { get; set; }

        public FieldValue() { }

        public FieldValue(FieldValue fieldValue)
        {
            Copy(fieldValue);
        }

        public void Copy(FieldValue obj)
        {
            if (obj == null)
                return;

            // copy all of the properties
            foreach (PropertyInfo pi in obj.GetType().GetProperties())
            {
                // get the value of the property
                var val = pi.GetValue(obj, null);
                pi.SetValue(this, val, null);
            }
        }

        public override string ToString()
        {
            return this.FieldName;
        }
    }
}