using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class ItemType
    {
        // default item types
        public static Guid Task = new Guid("00000000-0000-0000-0000-000000000001");
        public static Guid ListItem = new Guid("00000000-0000-0000-0000-000000000002");
        public static Guid Location = new Guid("00000000-0000-0000-0000-000000000003");
        public static Guid Contact = new Guid("00000000-0000-0000-0000-000000000004");
        public static List<Guid> Default = new List<Guid>() { Task, ListItem, Location, Contact };

        public Guid ID { get; set; }
        public string Name { get; set; }
        public Guid? UserID { get; set; }
        public string Icon { get; set; }
        public List<Field> Fields { get; set; }
    }
}