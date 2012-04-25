using System.Reflection;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class Priority
    {
        public int PriorityID { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }

        public Priority() { }

        public Priority(Priority obj)
        {
            Copy(obj);
        }

        public void Copy(Priority obj)
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