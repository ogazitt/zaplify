using System.Reflection;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class ActionType
    {
        public int ActionTypeID { get; set; }
        public string ActionName { get; set; }
        public string DisplayName { get; set; }
        public string FieldName { get; set; }
        public int SortOrder { get; set; }

        public ActionType() { }

        public ActionType(ActionType obj)
        {
            Copy(obj);
        }

        public void Copy(ActionType obj)
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
            return this.ActionName;
        }
    }
}