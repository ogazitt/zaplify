using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class Item : ServerEntity
    {
        // ServerEntity properties
        public override Guid ID { get; set; }
        public override string Name { get; set; }

        public bool IsList { get; set; }
        public Guid FolderID { get; set; }
        public Guid? ParentID { get; set; }
        public Guid ItemTypeID { get; set; }
        public Guid UserID { get; set; }
        public float SortOrder { get; set; }

        public List<ItemTag> ItemTags { get; set; }
        public List<FieldValue> FieldValues { get; set; }

        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; } // this has to be the last field
    }
}