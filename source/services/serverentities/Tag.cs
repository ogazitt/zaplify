using System;
using System.Collections.Generic;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class Tag : UserOwnedEntity
    {
        // UserOwnedEntity properties
        public override Guid ID { get; set; }
        public override string Name { get; set; }
        public override Guid UserID { get; set; }

        public int ColorID { get; set; }

        public List<ItemTag> ItemTags { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}