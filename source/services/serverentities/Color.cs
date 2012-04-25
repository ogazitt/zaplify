using System.Reflection;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class Color
    {
        public int ColorID { get; set; }
        public string Name { get; set; }

        public Color() { }

        public Color(Color obj)
        {
            Copy(obj);
        }

        public void Copy(Color obj)
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