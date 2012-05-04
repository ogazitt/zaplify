using System;
using System.Collections.Generic;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class Folder : UserOwnedEntity
    {
        // UserOwnedEntity properties
        public override Guid ID { get; set; }
        public override string Name { get; set; }
        public override Guid UserID { get; set; }

        public Guid ItemTypeID { get; set; }
        public float SortOrder { get; set; }

        public List<FolderUser> FolderUsers { get; set; }
        public List<Item> Items { get; set; }
    }
}