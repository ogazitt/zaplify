using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class Folder
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public Guid UserID { get; set; }
        public Guid ItemTypeID { get; set; }
        public float SortOrder { get; set; }

        public List<FolderUser> FolderUsers { get; set; }
        public List<Item> Items { get; set; }
    }
}