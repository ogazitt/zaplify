using System;
using System.Collections.Generic;
using System.Reflection;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class Item : ServerEntity
    {
        // ServerEntity properties
        public override Guid ID { get; set; }
        public override string Name { get; set; }

        public bool IsList { get; set; }
        public Guid FolderID { get; set; }
        public Guid? ParentID { get; set; }
        public Guid ItemTypeID { get; set; }
        public Guid UserID { get; set; }
        public float SortOrder { get; set; }

        public List<ItemTag> ItemTags { get; set; }
        public List<FieldValue> FieldValues { get; set; }

        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; } // this has to be the last property

        public Item() { }

        public Item(Item item)
        {
            Copy(item, true);
        }

        public Item(Item item, bool deepCopy)
        {
            Copy(item, deepCopy);
        }

        public void Copy(Item obj, bool deepCopy)
        {
            if (obj == null)
                return;

            // copy all of the properties
            foreach (PropertyInfo pi in this.GetType().GetProperties())
            {
                if (pi.CanWrite)
                {
                    var val = pi.GetValue(obj, null);
                    pi.SetValue(this, val, null);
                }
            }

            if (deepCopy)
            {
                // reinitialize the FieldValues collection
                this.FieldValues = new List<FieldValue>();
                if (obj.FieldValues != null)
                    foreach (FieldValue fv in obj.FieldValues)
                        this.FieldValues.Add(new FieldValue(fv));
            }
        }

        public FieldValue GetFieldValue(string fieldName, bool create = false)
        {
            if (this.FieldValues != null)
            {   
                foreach (var fv in this.FieldValues)
                {
                    if (fv.FieldName.Equals(fieldName)) { return fv; }
                }
            }
            if (create == true)
            {
                FieldValue fv = new FieldValue() { FieldName = fieldName, ItemID = this.ID };
                if (this.FieldValues == null) { this.FieldValues = new List<FieldValue>(); }
                this.FieldValues.Add(fv);
                return fv;
            }
            return null;
        }

        public static FieldValue CreateFieldValue(Guid itemID, string fieldName, string value)
        {
            return new FieldValue() { /*ID = Guid.NewGuid(),*/ ItemID = itemID, FieldName = fieldName, Value = value };
        }

    }
}