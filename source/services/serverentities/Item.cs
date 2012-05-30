using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class Item : UserOwnedEntity
    {
        // UserOwnedEntity properties
        public override Guid ID { get; set; }
        public override string Name { get; set; }
        public override Guid UserID { get; set; }

        public bool IsList { get; set; }
        public Guid FolderID { get; set; }
        public Guid? ParentID { get; set; }
        public Guid ItemTypeID { get; set; }
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

        public object GetFieldValue(Field field)
        {
            PropertyInfo pi = null;
            object currentValue = null;

            // get the current field value.
            // the value can either be in a strongly-typed property on the item (e.g. Name),
            // or in one of the FieldValues 
            try
            {
                // get the strongly typed property
                pi = this.GetType().GetProperty(field.Name);
                if (pi != null)
                    currentValue = pi.GetValue(this, null);
            }
            catch (Exception)
            {
                // an exception indicates this isn't a strongly typed property on the Item
                // this is NOT an error condition
            }

            // if couldn't find a strongly typed property, this property could be stored as a 
            // FieldValue on the item
            if (pi == null)
            {
                // get current item's value for this field
                FieldValue fieldValue = this.FieldValues.FirstOrDefault(fv => fv.FieldName == field.Name);
                if (fieldValue != null)
                    currentValue = fieldValue.Value;
            }

            return currentValue;
        }

        public static FieldValue CreateFieldValue(Guid itemID, string fieldName, string value)
        {
            return new FieldValue() { /*ID = Guid.NewGuid(),*/ ItemID = itemID, FieldName = fieldName, Value = value };
        }
    }
}