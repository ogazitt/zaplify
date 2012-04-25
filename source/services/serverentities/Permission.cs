using System.Reflection;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class Permission
    {
        public int PermissionID { get; set; }
        public string Name { get; set; }

        public Permission() { }

        public Permission(Permission obj)
        {
            Copy(obj);
        }

        public void Copy(Permission obj)
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