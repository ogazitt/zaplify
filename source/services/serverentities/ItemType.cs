using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class ItemType
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public Guid? UserID { get; set; }
        public string Icon { get; set; }

        public List<Field> Fields { get; set; }
    }
}