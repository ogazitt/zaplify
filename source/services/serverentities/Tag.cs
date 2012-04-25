using System;
using System.Collections.Generic;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class Tag
    {
        public Guid ID { get; set; }
        public Guid UserID { get; set; }
        public string Name { get; set; }
        public int ColorID { get; set; }

        public List<ItemTag> ItemTags { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}